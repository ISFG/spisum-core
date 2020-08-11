using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Data.Models;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Concept;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using ISFG.SpisUm.ClientSide.Models.TransactionHistory;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Interfaces;
using ISFG.SpisUm.Models.V1;
using ISFG.Translations.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;
using static ISFG.SpisUm.ClientSide.Models.Nodes.NodeUpdate;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/concept")]
    public class ConceptController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAlfrescoModelComparer _alfrescoModelComparer;
        private readonly IAuditLogService _auditLogService;
        private readonly IComponentService _componentService;
        private readonly IConceptService _conceptService;
        private readonly IDocumentService _documentService;
        private readonly IIdentityUser _identityUser;
        private readonly INodesService _nodesService;
        private readonly ISpisUmConfiguration _spisUmConfiguration;
        private readonly ITransactionHistoryService _transactionHistoryService;
        private readonly ITranslateService _translateService;
        private readonly IValidationService _validationService;

        #endregion

        #region Constructors

        public ConceptController(
            IAlfrescoHttpClient alfrescoHttpClient,
            IAuditLogService auditLogService,
            INodesService nodesService, 
            IDocumentService documentService, 
            IConceptService conceptService, 
            IIdentityUser identityUser, 
            ISpisUmConfiguration spisUmConfiguration, 
            IComponentService componentService,
            IValidationService validationService,
            ITranslateService translateService,
            ITransactionHistoryService transactionHistoryService,
            IAlfrescoModelComparer alfrescoModelComparer
        )
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _auditLogService = auditLogService;
            _nodesService = nodesService;
            _documentService = documentService;
            _conceptService = conceptService;
            _identityUser = identityUser;
            _spisUmConfiguration = spisUmConfiguration;
            _componentService = componentService;
            _validationService = validationService;
            _translateService = translateService;
            _transactionHistoryService = transactionHistoryService;
            _alfrescoModelComparer = alfrescoModelComparer;

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Cancel concept action
        /// </summary>
        [HttpPost("{nodeId}/cancel")]
        public async Task Cancel([FromRoute] ConceptCancel nodeBody)
        {
            await _nodesService.CancelNode(nodeBody.NodeId, nodeBody.Body.Reason);
        }

        /// <summary>
        /// Create a new concept
        /// </summary>
        [HttpPost("create")]
        public async Task<NodeEntry> Create([FromBody] NodeUpdateBody nodeBody, [FromQuery] IncludeFieldsQueryParams queryParams)
        {
            var concept = await _documentService.Create(SpisumNames.NodeTypes.Concept, SpisumNames.Paths.EvidenceConcepts(_identityUser.RequestGroup), CodeLists.DocumentTypes.Digital);
            return await _nodesService.Update(new NodeUpdate { Body = nodeBody, NodeId = concept.Entry.Id }, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));
        }

        /// <summary>
        /// Create a comment for concept
        /// </summary>
        [HttpPost("{nodeId}/comment/create")]
        public async Task<CommentEntryFixed> CreateComment([FromRoute] string nodeId, [FromBody] CommentBody body)
        {
            var comment = await _alfrescoHttpClient.CreateComment(nodeId, body);

            try
            {
                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);
                var pid = nodeInfo?.GetPid();
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
            

            return comment;
        }

        /// <summary>
        /// Create a component for provided nodeId
        /// </summary>
        [HttpPost("{nodeId}/component/create")]
        public async Task<NodeEntry> CreateComponent([FromRoute] ConceptComponentCreate input)
        {
            var componentEntry = await _componentService.CreateVersionedComponent(input.NodeId, input.FileData);
            var nodeFinal = await _validationService.CheckOutputFormat(componentEntry?.Entry?.Id, input.FileData);
            await _validationService.UpdateDocumentOutputFormat(input.NodeId);
            try
            {
                var documentEntry = await _alfrescoHttpClient.GetNodeInfo(input.NodeId);

                var componentPid = componentEntry?.GetPid();

                await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.VlozeniKDokumentu, 
                    string.Format(TransactinoHistoryMessages.ConceptComponentCreate, documentEntry?.GetPid()));

                var fileId = await _documentService.GetDocumentFileId(documentEntry?.Entry?.Id);

                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.VlozeniKDokumentu, 
                        string.Format(TransactinoHistoryMessages.ConceptComponentCreateFile, documentEntry?.GetPid()));

            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return nodeFinal;
        }

        /// <summary>
        /// Cancel component
        /// </summary>
        [HttpPost("{nodeId}/component/delete")]
        public async Task<List<string>> DeleteComponent([FromRoute] ConceptComponentCancel input)
        {
            var deleted = await _componentService.CancelComponent(input.NodeId, input.ComponentsId);
            await _validationService.UpdateDocumentOutputFormat(input.NodeId);

            return deleted;
        }

        /// <summary>
        /// Download a component        
        /// </summary>
        [HttpPost("{nodeId}/component/download")]
        public async Task<FileContentResult> Download([FromRoute] string nodeId, [FromBody] List<string> nodesId)
        {
            var notValidIds = new List<string>();
            var childrens = await _alfrescoHttpClient.GetNodeSecondaryChildren(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Components}')", ParameterType.QueryString)));

            foreach (var node in nodesId)
                if (childrens.List.Entries.All(x => x.Entry.Id != node))
                    notValidIds.Add(node);
            
            if (notValidIds.Any())
                throw new BadRequestException($"Node id's '{string.Join(",", notValidIds.ToArray())}' are not associated with concept.");

            var nodeEntry = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            var file = await _nodesService.Download(nodesId, nodeEntry?.GetPid());

            try
            {
                await nodesId.ForEachAsync(async componentId =>
                {
                    var componentEntry = await _alfrescoHttpClient.GetNodeInfo(componentId);

                    var documentParent = await _nodesService.GetParentsByAssociation(componentId, new List<string> { SpisumNames.Associations.Components });
                    var documentEntry = await _alfrescoHttpClient.GetNodeInfo(documentParent?.FirstOrDefault()?.Entry?.Id);

                    var componentPid = componentEntry?.GetPid();

                    await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Export, 
                        TransactinoHistoryMessages.ConceptComponentDownloadDocument);

                    var fileId = await _documentService.GetDocumentFileId(documentEntry?.Entry?.Id);

                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Export, 
                            TransactinoHistoryMessages.ConceptComponentDownloadFile);

                });
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return file;
        }

        /// <summary>
        /// Get concept comments
        /// </summary>
        [HttpGet("{nodeId}/comments")]
        public async Task<CommentPagingFixed> GetComments([FromRoute] string nodeId, [FromQuery] SimpleQueryParams queryParams)
        {
            return await _alfrescoHttpClient.GetComments(nodeId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));
        }

        /// <summary>
        /// Upload a new version of component
        /// </summary>
        [HttpGet("{nodeId}/component/{componentId}/content")]
        public async Task<FileContentResult> GetContent([FromRoute] string nodeId, [FromRoute] string componentId, [FromQuery] ContentQueryParams queryParams)
        {
            var fileContent = await _alfrescoHttpClient.NodeContent(componentId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));

            if (fileContent != null)
            {
                try
                {
                    var componentEntry = await _alfrescoHttpClient.GetNodeInfo(componentId);

                    var documentParent = await _nodesService.GetParentsByAssociation(componentId, new List<string> { SpisumNames.Associations.Components });
                    var documentEntry = await _alfrescoHttpClient.GetNodeInfo(documentParent?.FirstOrDefault()?.Entry?.Id);

                    var componentPid = componentEntry?.GetPid();

                    await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Zobrazeni, 
                        TransactinoHistoryMessages.ConceptComponentGetContentDocument);

                    var fileId = await _documentService.GetDocumentFileId(documentEntry?.Entry?.Id);

                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Zobrazeni, 
                            TransactinoHistoryMessages.ConceptComponentGetContentFile);

                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return new FileContentResult(fileContent.File, fileContent.ContentType)
                {
                    FileDownloadName = $"{fileContent.FileName}"
                };
            }

            throw new Exception();
        }

        /// <summary>
        /// Get Concept NodeEntry
        /// </summary>
        [HttpGet("{nodeId}")]
        public async Task<NodeEntry> GetFile([FromRoute] string nodeId, [FromQuery] BasicNodeQueryParamsWithRelativePath queryParams)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));

            try
            {
                var componentPid = nodeInfo?.GetPid();

                await _auditLogService.Record(nodeInfo?.Entry?.Id, SpisumNames.NodeTypes.Concept, componentPid, NodeTypeCodes.Dokument, EventCodes.Zobrazeni,
                    TransactinoHistoryMessages.Concept);

                var fileId = await _documentService.GetDocumentFileId(nodeInfo?.Entry?.Id);

                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Concept, componentPid, NodeTypeCodes.Dokument, EventCodes.Zobrazeni,
                        TransactinoHistoryMessages.Concept);

            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return nodeInfo;
        }

        /// <summary>
        /// Get history for node and all associated nodes
        /// </summary>
        /// <returns>List of history records</returns>  
        [HttpGet("{nodeId}/history")]
        public async Task<HistoryResponseModel> GetHistory([FromRoute] string nodeId, [FromQuery] SimpleQueryParams queryParams)
        {
            if (queryParams.SkipCount < 0) queryParams.SkipCount = 0;
            if (queryParams.MaxItems < 0 || queryParams.MaxItems > 100) queryParams.MaxItems = 100;

            var history = await _auditLogService.GetEvents(new TransactionHistoryQuery
            {
                NodeId = nodeId,
                CurrentPage = queryParams.SkipCount + 1,
                PageSize = queryParams.MaxItems
            });

            return new HistoryResponseModel
            {
                List = new HistoryListModel
                {
                    Pagination = new Pagination
                    {
                        Count = history.PageCount,
                        HasMoreItems = history.CurrentPage < history.PageCount,
                        TotalItems = history.RecordCount,
                        SkipCount = queryParams.SkipCount,
                        MaxItems = queryParams.MaxItems
                    },
                    Entries = history.Results.ToList().Select(x => new TransactionHistoryEntry { Entry = x })
                }
            };
        }

        /// <summary>
        /// Get a components thumbnail in PDF 
        /// </summary>
        [HttpGet("{nodeId}/component/{componentId}/thumbnail/pdf")]
        public async Task<FileContentResult> GetThumbnailPdf([FromRoute] string nodeId, [FromRoute] string componentId)
        {
            var fileContent = await _alfrescoHttpClient.GetThumbnailPdf(componentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Query.C, AlfrescoNames.Query.Force, ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Query.NoCache, DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds, ParameterType.QueryString)));

            if (fileContent != null)
            {
                try
                {
                    var componentEntry = await _alfrescoHttpClient.GetNodeInfo(componentId);

                    var documentParent = await _nodesService.GetParentsByAssociation(componentId, new List<string> { SpisumNames.Associations.Components });
                    var documentEntry = await _alfrescoHttpClient.GetNodeInfo(documentParent?.FirstOrDefault()?.Entry?.Id);

                    var componentPid = componentEntry?.GetPid();

                    await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Zobrazeni, 
                        TransactinoHistoryMessages.DocumentComponentGetContentDocument);

                    var fileId = await _documentService.GetDocumentFileId(documentEntry?.Entry?.Id);

                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Zobrazeni, 
                                TransactinoHistoryMessages.DocumentComponentGetContentFile);

                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return new FileContentResult(fileContent.File, fileContent.ContentType)
                {
                    FileDownloadName = $"{fileContent.FileName}"
                };
            }

            throw new Exception();
        }

        /// <summary>
        /// Accept handover
        /// </summary>
        [HttpPost("{nodeId}/owner/accept")]
        public async Task OwnerAccept([FromRoute] DocumentOwnerAccept ownerAccept)
        {
            await _nodesService.AcceptOwner(ownerAccept.NodeId);
        }

        /// <summary>
        /// Cancel handover
        /// </summary>
        [HttpPost("{nodeId}/owner/cancel")]
        public async Task OwnerCancel([FromRoute] DocumentOwnerCancel ownerCancel)
        {
            await _nodesService.DeclineOwner(ownerCancel.NodeId, true);
        }

        /// <summary>
        /// Decline handover
        /// </summary>
        [HttpPost("{nodeId}/owner/decline")]
        public async Task OwnerDecline([FromRoute] DocumentOwnerDecline ownerDecline)
        {
            await _nodesService.DeclineOwner(ownerDecline.NodeId, false);
        }

        /// <summary>
        /// Handover a concept
        /// </summary>
        [HttpPost("{nodeId}/owner/handover")]
        public async Task OwnerHandover([FromRoute] DocumentOwnerHandOver nodeBody)
        {
            if (await _nodesService.IsNodeLocked(nodeBody.NodeId))
                await _nodesService.UnlockAll(nodeBody.NodeId);

            await _nodesService.UpdateHandOverPermissionsAll(nodeBody.NodeId, nodeBody.Body.NextGroup, nodeBody.Body.NextOwner);

            await _nodesService.MoveHandOverPath(nodeBody.NodeId, _identityUser.RequestGroup, nodeBody.Body.NextGroup, nodeBody.Body.NextOwner);
            await _nodesService.LockAll(nodeBody.NodeId);

            await _transactionHistoryService.LogHandover(nodeBody.NodeId, nodeBody.Body.NextGroup, nodeBody.Body.NextOwner);
        }

        /// <summary>
        /// Recover provided concept
        /// </summary>
        [HttpPost("recover")]
        public async Task Recover(ConceptRecover nodeBody)
        {
            await _nodesService.Recover(nodeBody.Ids, nodeBody.Reason, SpisumNames.NodeTypes.Concept, SpisumNames.Paths.EvidenceCancelled(_identityUser.RequestGroup));
        }

        /// <summary>
        /// Revert concept version
        /// </summary>
        [HttpPost("{nodeId}/revert/{versionId}")]
        public async Task<NodeEntry> RevertConcept([FromRoute] ConceptRevert input)
        {
            return await _documentService.Revert(input.NodeId, input.VersionId);
        }

        /// <summary>
        /// Converts concept into document
        /// </summary>        
        [HttpPost("{nodeId}/to-document")]
        public async Task<NodeEntry> ToDocument([FromRoute] ConceptToDocument nodeBody)
        {
            return await _conceptService.ToDocument(nodeBody.NodeId, nodeBody.Body.Author, nodeBody.Body.Subject, nodeBody.Body.AttachmentsCount.Value,
                new GenerateSsid
                {
                    Pattern = _spisUmConfiguration.Ssid.Pattern,
                    Shortcut = _spisUmConfiguration.Ssid.Shortcut,
                    SsidNumberPlaces = _spisUmConfiguration.Ssid.SsidNumberPlaces
                }
                , nodeBody.Body.SettleTo);
        }

        /// <summary>
        /// Update concept's component
        /// </summary>
        [HttpPost("{nodeId}/component/{componentId}/update")]
        public async Task<NodeEntry> UpdateComponent([FromRoute] ComponentUpdate nodeBody, [FromQuery] IncludeFieldsQueryParams queryParams)
        {
            var nodeEntryBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(nodeBody.ComponentId);
            var nodeEntryAfterUpdate = await _nodesService.ComponentUpdate(nodeBody, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));

            try
            {
                var documentEntry = await _alfrescoHttpClient.GetNodeInfo(nodeBody?.NodeId);

                var difference = _alfrescoModelComparer.CompareProperties(
                    nodeEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                    nodeEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary());

                if (difference.Count > 0)
                {
                    try
                    {
                        var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                        if (componentsJson != null)
                            difference.Remove(componentsJson);
                    }
                    catch { }

                    string messageDocument = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.ConceptComponentUpdateDocument, difference);
                    string messageFile = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.ConceptComponentUpdateFile, difference);

                    await _auditLogService.Record(
                        documentEntry?.Entry?.Id,
                        SpisumNames.NodeTypes.Component,
                        nodeEntryBeforeUpdate?.GetPid(),
                        NodeTypeCodes.Komponenta,
                        EventCodes.Uprava,
                        nodeEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        nodeEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        messageDocument);

                    var fileId = await _documentService.GetDocumentFileId(documentEntry?.Entry?.Id);
                    if (fileId != null)
                        await _auditLogService.Record(
                            fileId,
                            SpisumNames.NodeTypes.Component,
                            nodeEntryBeforeUpdate?.GetPid(),
                            NodeTypeCodes.Komponenta,
                            EventCodes.Uprava,
                            nodeEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                            nodeEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                            messageFile);
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return nodeEntryAfterUpdate;
        }

        /// <summary>
        /// Create shredding discard for document
        /// </summary>
        [HttpPost("{nodeId}/update")]
        public async Task<NodeEntry> UpdateConcept([FromRoute] NodeUpdate nodeBody, [FromQuery] IncludeFieldsQueryParams queryParams)
        {
            var conceptEntryBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(nodeBody?.NodeId);
            var conceptEntryAfterUpdate = await _nodesService.Update(nodeBody, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));

            var documentId = conceptEntryBeforeUpdate?.Entry?.Id;
            var documentPid = conceptEntryAfterUpdate?.GetPid();

            var difference = _alfrescoModelComparer.CompareProperties(
                conceptEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                conceptEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary());

            if (difference.Count > 0)
            {
                try
                {
                    var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                    if (componentsJson != null)
                        difference.Remove(componentsJson);
                }
                catch { }

                string messageDocument = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.ConceptUpdate, difference);

                try
                {
                    await _auditLogService.Record(
                        documentId,
                        SpisumNames.NodeTypes.Concept,
                        documentPid,
                        NodeTypeCodes.Dokument,
                        EventCodes.Uprava,
                        conceptEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        conceptEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        messageDocument);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

            }

            return conceptEntryAfterUpdate;
        }

        /// <summary>
        /// Creates a new version of component
        /// </summary>
        [HttpPost("{nodeId}/component/{componentId}/content")]
        public async Task<NodeEntry> UpdateContentComponent([FromRoute] ConceptComponentUpdateContent input)
        {
            var componentEntryBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(input.NodeId);
            var componentEntryAfterUpdate = await _componentService.UploadNewVersionComponent(input.NodeId, input.ComponentId, input.FileData);
            
            await _validationService.CheckOutputFormat(componentEntryAfterUpdate?.Entry?.Id, input.FileData);

            try
            {
                var component = await _alfrescoHttpClient.GetNodeInfo(input.ComponentId);

                var componentPid = component?.GetPid();

                await _auditLogService.Record(input.NodeId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.NovaVerze,
                    componentEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                    componentEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                    TransactinoHistoryMessages.ConceptComponentPostContentDocument);

                var fileId = await _documentService.GetDocumentFileId(input.NodeId);

                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.NovaVerze, 
                        TransactinoHistoryMessages.ConceptComponentPostContentFile);

            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return componentEntryAfterUpdate;
        }

        #endregion
    }
}