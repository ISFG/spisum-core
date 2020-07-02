using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Document;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Interfaces;
using ISFG.SpisUm.Models.V1;
using ISFG.Translations.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/document")]
    public class DocumentController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAlfrescoModelComparer _alfrescoModelComparer;
        private readonly IAuditLogService _auditLogService;
        private readonly IComponentService _componentService;
        private readonly IDocumentService _documentService;
        private readonly IIdentityUser _identityUser;
        private readonly INodesService _nodesService;
        private readonly IShipmentsService _shipmentsService;
        private readonly ISpisUmConfiguration _spisUmConfiguration;
        private readonly ITransactionHistoryService _transactionHistory;
        private readonly ITranslateService _translateService;
        private readonly IValidationService _validationService;

        #endregion

        #region Constructors

        public DocumentController(
            IAlfrescoHttpClient alfrescoHttpClient,
            IAuditLogService auditLogService,
            IComponentService componentService,
            IDocumentService documentService,
            IIdentityUser identityUser,
            INodesService nodesService,
            ISpisUmConfiguration spisUmConfiguration,
            ITransactionHistoryService transactionHistory,
            IShipmentsService shipmentsService,
            IValidationService validationService,
            IAlfrescoModelComparer alfrescoModelComparer,
            ITranslateService translateService
        )
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _auditLogService = auditLogService;
            _componentService = componentService;
            _documentService = documentService;
            _identityUser = identityUser;
            _nodesService = nodesService;
            _shipmentsService = shipmentsService;
            _spisUmConfiguration = spisUmConfiguration;
            _transactionHistory = transactionHistory;
            _validationService = validationService;
            _alfrescoModelComparer = alfrescoModelComparer;
            _translateService = translateService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Borrow a document
        /// </summary>
        [HttpPost("{nodeId}/borrow")]
        public async Task<NodeEntry> Borrow([FromRoute] DocumentBorrow input)
        {
            return await _documentService.Borrow(input.NodeId, input.Body.Group, input.Body.User);
        }

        /// <summary>
        /// Cancel document / file
        /// </summary>
        /// <param name="nodeBody"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/cancel")]
        public async Task Cancel([FromRoute] DocumentCancel nodeBody)
        {
            await _nodesService.CancelNode(nodeBody.NodeId, nodeBody.Body.Reason);
        }

        /// <summary>
        /// Cancel document Settle action
        /// </summary>
        [HttpPost("{nodeId}/settle/cancel")]
        public async Task<NodeEntry> CancelSettle([FromRoute] DocumentSettleCancel documentSettleCancel)
        {
            return await _documentService.SettleCancel(documentSettleCancel.NodeId, documentSettleCancel.Body.Reason);
        }

        [HttpPost("{nodeId}/component/{componentId}/convert")]
        public async Task<NodeEntry> ConvertComponent([FromQuery] DocumentComponentOutputFormat outputFormat)
        {
            return await _validationService.ConvertToOutputFormat(outputFormat.NodeId, outputFormat.ComponentId, outputFormat.Body.Reason, _spisUmConfiguration.Originator);
        }

        /// <summary>
        /// Create a commment for document
        /// </summary>
        [HttpPost("{nodeId}/comment/create")]
        public async Task<CommentEntryFixed> CreateComment([FromRoute] string nodeId, [FromBody] CommentBody body)
        {
            return await _alfrescoHttpClient.CreateComment(nodeId, body);
        }

        /// <summary>
        /// Creates component (secondary children) for provided node
        /// </summary>
        /// <param name="componentCreate"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/component/create")]
        public async Task<NodeEntry> CreateComponent([FromRoute] DocumentComponentCreate componentCreate)
        {
            var componentEntry = await _componentService.CreateVersionedComponent(componentCreate.NodeId, componentCreate.FileData);
            var nodeFinal = await _validationService.CheckOutputFormat(componentEntry?.Entry?.Id, componentCreate.FileData);

            try
            {
                var documentEntry = await _alfrescoHttpClient.GetNodeInfo(componentCreate.NodeId);

                var componentPid = componentEntry?.GetPid();

                await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.VlozeniKDokumentu, string.Format(TransactinoHistoryMessages.DocumentComponentCreateDocument, documentEntry?.GetPid()));

                var fileId = await _documentService.GetDocumentFileId(documentEntry?.Entry?.Id);

                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.VlozeniKDokumentu, string.Format(TransactinoHistoryMessages.DocumentComponentCreateFile, documentEntry?.GetPid()));

            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return nodeFinal;
        }

        /// <summary>
        /// Creates a new node with provided nodeType
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [HttpPost("create")]
        public async Task<NodeEntry> CreateNode(DocumentCreate parameters)
        {
            return await _documentService.Create(SpisumNames.NodeTypes.Document, SpisumNames.Paths.MailRoomUnfinished, parameters.DocumentType, parameters.NodeId);
        }

        /// <summary>
        /// Delete a component
        /// </summary>
        [HttpPost("{nodeId}/component/delete")]
        public async Task<List<string>> DeleteComponents([FromQuery] DocumentComponentDelete input)
        {
            var componentId = await _componentService.CancelComponent(input.NodeId, input.ComponentsId);
            return componentId;
        }

        /// <summary>
        /// Download a component
        /// </summary>
        [HttpPost("{nodeId}/component/download")]
        public async Task<FileContentResult> Download(List<string> nodesId)
        {
            var file =  await _nodesService.Download(nodesId);

            try
            {
                await nodesId.ForEachAsync(async componentId =>
                {
                    var componentEntry = await _alfrescoHttpClient.GetNodeInfo(componentId);

                    var documentParent = await _nodesService.GetParentsByAssociation(componentId, new List<string> { SpisumNames.Associations.Components });
                    var documentEntry = await _alfrescoHttpClient.GetNodeInfo(documentParent?.FirstOrDefault()?.Entry?.Id);

                    var componentPid = componentEntry?.GetPid();

                    await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Export, TransactinoHistoryMessages.DocumentComponentPostContentDocument);

                    var fileId = await _documentService.GetDocumentFileId(documentEntry?.Entry?.Id);

                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Export, TransactinoHistoryMessages.DocumentComponentPostContentFile);

                });
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return file;
        }

        /// <summary>
        /// Marks document as favourite
        /// </summary>
        /// <param name="favouriteAdd"></param>
        /// <returns></returns>
        [HttpPost("favorite/add")]
        public async Task<List<string>> FavoriteAdd([FromQuery] DocumentFavouriteAdd favouriteAdd)
        {
            return await _documentService.FavoriteAdd(favouriteAdd.NodeId);
        }

        /// <summary>
        /// Unmarks document as favourite   
        /// </summary>
        /// <param name="favouriteRemove"></param>
        /// <returns></returns>
        [HttpPost("favorite/remove")]
        public async Task<List<string>> FavoriteRemove([FromQuery] DocumentFavouriteRemove favouriteRemove)
        {
            return await _documentService.FavoriteRemove(favouriteRemove.NodeId);
        }

        /// <summary>
        /// Handover a document for signature
        /// </summary>
        [HttpPost("{nodeId}/for-signature")]
        public async Task ForSigniture([FromQuery] DocumentForSignature documentForSignature)
        {
            await _nodesService.UpdateForSignaturePermisionsAll(documentForSignature.NodeId, documentForSignature.Body.User, documentForSignature.Body.Group);
            await _nodesService.MoveForSignature(documentForSignature.NodeId, _identityUser.RequestGroup);

            try
            {
                await _transactionHistory.LogForSignature(documentForSignature?.NodeId, await _documentService.GetDocumentFileId(documentForSignature?.NodeId),
                                documentForSignature.Body.Group, documentForSignature.Body.User);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            await _nodesService.LockAll(documentForSignature.NodeId);
        }

        /// <summary>
        /// Found a document that has been lost
        /// </summary>
        [HttpPost("found")]
        public async Task<List<string>> Found([FromRoute] DocumentFound documentFound)
        {
            return await _nodesService.FoundNode(documentFound.NodeId, SpisumNames.NodeTypes.Document);
        }

        /// <summary>
        /// Return a document from signature
        /// </summary>
        [HttpPost("{nodeId}/from-signature")]
        public async Task<NodeEntry> FromSignature([FromRoute] DocumentFromSignature input)
        {
            return await _nodesService.MoveFromSignature(input.NodeId);
        }

        /// <summary>
        /// Get document comments
        /// </summary>
        [HttpGet("{nodeId}/comments")]
        public async Task<CommentPagingFixed> GetComments([FromRoute] string nodeId, [FromQuery] SimpleQueryParams queryParams)
        {
            return await _alfrescoHttpClient.GetComments(nodeId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));
        }

        /// <summary>
        /// Get content
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

                    await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Zobrazeni, TransactinoHistoryMessages.DocumentComponentGetContentDocument);

                    var fileId = await _documentService.GetDocumentFileId(documentEntry?.Entry?.Id);

                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Zobrazeni, TransactinoHistoryMessages.DocumentComponentGetContentFile);

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
        /// Get a document NodeEntr
        /// </summary>
        [HttpGet("{nodeId}")]
        public async Task<NodeEntry> GetDocument([FromRoute] string nodeId, [FromQuery] BasicNodeQueryParamsWithRelativePath queryParams)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));

            try
            {
                var componentPid = nodeInfo?.GetPid();

                await _auditLogService.Record(nodeInfo?.Entry?.Id, SpisumNames.NodeTypes.Document, componentPid, NodeTypeCodes.Dokument, EventCodes.Zobrazeni,
                    TransactinoHistoryMessages.Document);

                var fileId = await _documentService.GetDocumentFileId(nodeInfo?.Entry?.Id);

                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Document, componentPid, NodeTypeCodes.Dokument, EventCodes.Zobrazeni,
                        TransactinoHistoryMessages.Document);

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

            var output = new HistoryResponseModel
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
            return output;
        }

        /// <summary>
        /// Get a component thumbnail in PDF
        /// </summary>
        [HttpGet("{nodeId}/component/{componentId}/thumbnail/pdf")]
        public async Task<FileContentResult> GetThumbnailPdf([FromRoute] string nodeId, [FromRoute] string componentId)
        {
            var fileContent = await _alfrescoHttpClient.GetThumbnailPdf(componentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter("c", "force", ParameterType.QueryString))
                .Add(new Parameter("noCache", DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds, ParameterType.QueryString)));

            if (fileContent != null)
            {
                try
                {
                    var componentEntry = await _alfrescoHttpClient.GetNodeInfo(componentId);

                    var documentParent = await _nodesService.GetParentsByAssociation(componentId, new List<string> { SpisumNames.Associations.Components });
                    var documentEntry = await _alfrescoHttpClient.GetNodeInfo(documentParent?.FirstOrDefault()?.Entry?.Id);

                    var componentPid = componentEntry?.GetPid();

                    await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Zobrazeni, TransactinoHistoryMessages.DocumentComponentGetContentDocument);

                    var fileId = await _documentService.GetDocumentFileId(documentEntry?.Entry?.Id);

                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.Zobrazeni, TransactinoHistoryMessages.DocumentComponentGetContentFile);

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
        /// Changes location of the document
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/change-file-mark")]
        public async Task<NodeEntry> ChangeFileMark([FromRoute] DocumentChangeFileMark input)
        {
            return await _documentService.ChangeFileMark(input.NodeId, input.Body.FileMark);
        }

        /// <summary>
        /// Changes location of the document
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/change-location")]
        public async Task<NodeEntry> ChangeLocation([FromRoute] DocumentChangeLocation input)
        {
            return await _documentService.ChangeLocation(input.NodeId, input.Body.Location);
        }

        /// <summary>
        /// Change retention mark to A
        /// </summary>
        [HttpPost("{nodeId}/change-to-a")]
        public async Task ChangeToA([FromRoute] DocumentShreddingA documentShreddingA)
        {
            await _documentService.ShreddingChange(documentShreddingA.NodeId, "A");
        }

        /// <summary>
        /// Change retention mark to S
        /// </summary>
        [HttpPost("{nodeId}/change-to-s")]
        public async Task ChangeToS([FromRoute] DocumentShreddingS documentShreddingS)
        {
            await _documentService.ShreddingChange(documentShreddingS.NodeId, "S");
        }

        /// <summary>
        /// Mark document as lost or destroyed
        /// </summary>
        [HttpPost("{nodeId}/lost-destroyed")]
        public async Task LostDestroyed([FromRoute] DocumentLostDestroyed nodeBody)
        {
            await _nodesService.LostDestroyedNode(nodeBody.NodeId, nodeBody.Body.Reason);
        }

        /// <summary>
        /// Accept a document that has been handovered
        /// </summary>
        [HttpPost("{nodeId}/owner/accept")]
        public async Task OwnerAccept([FromRoute] DocumentOwnerAccept ownerAccept)
        {
            await _nodesService.AcceptOwner(ownerAccept.NodeId);
        }

        /// <summary>
        /// Cancel handover action
        /// </summary>
        [HttpPost("{nodeId}/owner/cancel")]
        public async Task OwnerCancel([FromRoute] DocumentOwnerCancel ownerCancel)
        {
            await _nodesService.DeclineOwner(ownerCancel.NodeId, true);
        }

        /// <summary>
        /// Decline handovered document
        /// </summary>
        [HttpPost("{nodeId}/owner/decline")]
        public async Task OwnerDecline([FromRoute] DocumentOwnerDecline ownerDecline)
        {
            await _nodesService.DeclineOwner(ownerDecline.NodeId, false);
        }

        /// <summary>
        /// Handover a document
        /// </summary>
        [HttpPost("{nodeId}/owner/handover")]
        public async Task OwnerHandover([FromRoute] DocumentOwnerHandOver nodeBody)
        {
            if (await _nodesService.IsNodeLocked(nodeBody.NodeId))
                await _nodesService.UnlockAll(nodeBody.NodeId);

            await _nodesService.UpdateHandOverPermissionsAll(nodeBody.NodeId, nodeBody.Body.NextGroup, nodeBody.Body.NextOwner);

            await _nodesService.MoveHandOverPath(nodeBody.NodeId, _identityUser.RequestGroup, nodeBody.Body.NextGroup, nodeBody.Body.NextOwner);

            await _nodesService.LockAll(nodeBody.NodeId);

            await _transactionHistory.LogHandover(nodeBody.NodeId, nodeBody.Body.NextGroup, nodeBody.Body.NextOwner);
        }

        /// <summary>
        /// Recover provided document
        /// </summary>
        /// <param name="favouriteRemove"></param>
        /// <returns>List of Ids that cannot or failed to be proceed.</returns>
        [HttpPost("recover")]
        public async Task<List<string>> Recover(DocumentRecover favouriteRemove)
        {
            return await _documentService.Recover(favouriteRemove.Ids, favouriteRemove.Reason);
        }

        /// <summary>
        /// Register document
        /// </summary>
        /// <param name="nodeBody"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/register")]
        public async Task<NodeEntry> Register([FromRoute] NodeUpdate nodeBody)
        {
            var documentEntry = await _documentService.Register(nodeBody, new GenerateSsid
            {
                Pattern = _spisUmConfiguration.Ssid.Pattern,
                Shortcut = _spisUmConfiguration.Ssid.Shortcut,
                SsidNumberPlaces = _spisUmConfiguration.Ssid.SsidNumberPlaces
            });

            try
            {
                await _auditLogService.Record(documentEntry?.Entry?.Id, SpisumNames.NodeTypes.Document, documentEntry?.GetPid(), NodeTypeCodes.Dokument, EventCodes.PripojeniCJ, TransactinoHistoryMessages.DocumentRegister);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return documentEntry;
        }

        /// <summary>
        /// Return a borrowed document
        /// </summary>
        [HttpPost("{nodeId}/return")]
        public async Task<NodeEntry> Return([FromRoute] DocumentReturn input)
        {
            return await _documentService.Return(input.NodeId);
        }

        /// <summary>
        /// Return a document for a rework that has been handovered
        /// </summary>
        [HttpPost("{nodeId}/return-for-rework")]
        public async Task<NodeEntry> ReturnForRework([FromQuery] DocumentReturnForRework input)
        {
            return await _documentService.DocumentReturnForRework(input.NodeId, input.Body.Reason);
        }

        /// <summary>
        /// Revert document version
        /// </summary>
        [HttpPost("{nodeId}/revert/{versionId}")]
        public async Task<NodeEntry> RevertConcept([FromRoute] DocumentRevert input)
        {
            return await _documentService.Revert(input.NodeId, input.VersionId);
        }

        /// <summary>
        /// Settle a document
        /// </summary>
        [HttpPost("{nodeId}/settle")]
        public async Task<NodeEntry> Settle([FromRoute] DocumentSettle documentSettle)
        {
             var documentEntry = await _documentService.Settle(documentSettle.NodeId, documentSettle.Body.SettleMethod,
                documentSettle.Body.SettleDate.Value, SpisumNames.Paths.EvidenceDocumentsProcessed(_identityUser.RequestGroup), documentSettle.Body.CustomSettleMethod, documentSettle.Body.SettleReason);

             return documentEntry;
        }

        /// <summary>
        /// Create a new shipment type of email
        /// </summary>
        [HttpPost("{nodeId}/shipment/create/email")]
        public async Task<NodeEntry> ShipmentCreate([FromRoute] ShipmentCreateEmail shipmentCreateEmail)
        {
            return await _shipmentsService.CreateShipmentEmail(
                shipmentCreateEmail.NodeId,
                shipmentCreateEmail.Body.Recipient,
                shipmentCreateEmail.Body.Sender,
                shipmentCreateEmail.Body.Subject,
                shipmentCreateEmail.Body.Components,
                Path.Combine(_spisUmConfiguration.Shipments.ConfigurationFilesFolder, _spisUmConfiguration.Shipments.FolderName, _spisUmConfiguration.Shipments.ShipmentCreateTextFile));
        }

        /// <summary>
        /// Create a new shipment type of databox
        /// </summary>
        [HttpPost("{nodeId}/shipment/create/databox")]
        public async Task<NodeEntry> ShipmentCreateDataBox([FromRoute] ShipmentCreateDataBox shipmentCreateDataBox)
        {
            return await _shipmentsService.CreateShipmentDataBox(
                shipmentCreateDataBox.NodeId,
                shipmentCreateDataBox.Body.AllowSubstDelivery,
                shipmentCreateDataBox.Body.LegalTitleLaw,
                shipmentCreateDataBox.Body.LegalTitleYear,
                shipmentCreateDataBox.Body.LegalTitleSect,
                shipmentCreateDataBox.Body.LegalTitlePar,
                shipmentCreateDataBox.Body.LegalTitlePoint,
                shipmentCreateDataBox.Body.PersonalDelivery,
                shipmentCreateDataBox.Body.Recipient,
                shipmentCreateDataBox.Body.Sender,
                shipmentCreateDataBox.Body.Subject,
                shipmentCreateDataBox.Body.ToHands,
                shipmentCreateDataBox.Body.Components);
        }

        /// <summary>
        /// Create a new shipment type of personally
        /// </summary>
        [HttpPost("{nodeId}/shipment/create/personally")]
        public async Task<NodeEntry> ShipmentCreatePersonally([FromRoute] ShipmentCreatePersonally shipmentCreatePersonally)
        {
            return await _shipmentsService.CreateShipmentPersonally(
                shipmentCreatePersonally.NodeId,
                shipmentCreatePersonally.Body.Address1,
                shipmentCreatePersonally.Body.Address2,
                shipmentCreatePersonally.Body.Address3,
                shipmentCreatePersonally.Body.Address4,
                shipmentCreatePersonally.Body.AddressStreet,
                shipmentCreatePersonally.Body.AddressCity,
                shipmentCreatePersonally.Body.AddressZip,
                shipmentCreatePersonally.Body.AddressState);
        }

        /// <summary>
        /// Create a new shipment type of post
        /// </summary>
        [HttpPost("{nodeId}/shipment/create/post")]
        public async Task<NodeEntry> ShipmentCreatePost([FromRoute] ShipmentCreatePost shipmentCreatePost)
        {
            return await _shipmentsService.CreateShipmentPost(
                shipmentCreatePost.NodeId,
                shipmentCreatePost.Body.Address1,
                shipmentCreatePost.Body.Address2,
                shipmentCreatePost.Body.Address3,
                shipmentCreatePost.Body.Address4,
                shipmentCreatePost.Body.AddressStreet,
                shipmentCreatePost.Body.AddressCity,
                shipmentCreatePost.Body.AddressZip,
                shipmentCreatePost.Body.AddressState,
                shipmentCreatePost.Body.PostType,
                shipmentCreatePost.Body.PostTypeOther,
                shipmentCreatePost.Body.PostItemType,
                shipmentCreatePost.Body.PostItemTypeOther,
                shipmentCreatePost.Body.PostItemCashOnDelivery,
                shipmentCreatePost.Body.PostItemStatedPrice);
        }

        /// <summary>
        /// Create a new shipment type of publish
        /// </summary>
        [HttpPost("{nodeId}/shipment/create/publish")]
        public async Task<NodeEntry> ShipmentCreatePublish([FromRoute] ShipmentCreatePublish shipmentCreatePublish)
        {
            return await _shipmentsService.CreateShipmentPublish(
                shipmentCreatePublish.NodeId,
                shipmentCreatePublish.Body.Components,
                shipmentCreatePublish.Body.DateFrom.Value,
                shipmentCreatePublish.Body.Days,
                shipmentCreatePublish.Body.Note);
        }

        /// <summary>
        /// Send shipment (document)
        /// </summary>
        /// <param name="documentShipmentSend"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/shipment/send")]
        public async Task<List<string>> ShipmentSend([FromRoute] DocumentShipmentSend documentShipmentSend)
        {
            return await _shipmentsService.ShipmentsSend(documentShipmentSend.NodeId, documentShipmentSend.ShipmentsId);
        }

        /// <summary>
        /// Cancel document shredding discard action
        /// </summary>
        [HttpPost("{nodeId}/shredding/cancel-discard")]
        public async Task<NodeEntry> ShreddingCancelDiscard([FromRoute] DocumentShreddingCancelDiscard input)
        {
            return await _documentService.ShreddingCancelDiscard(input.NodeId, SpisumNames.Associations.DocumentInRepository);
        }

        /// <summary>
        /// Document shredding discard action
        /// </summary>
        [HttpPost("{nodeId}/shredding/discard")]
        public async Task<NodeEntry> ShreddingDiscard([FromRoute] DocumentShreddingDiscard input)
        {
            return await _documentService.ShreddingDiscard(input.NodeId, input.Body.Date.Value, input.Body.Reason, DateTime.UtcNow, SpisumNames.Associations.DocumentInRepository);
        }

        /// <summary>
        /// Handover a document to repository
        /// </summary>
        [HttpPost("to-repository")]
        public async Task<List<string>> ToRepository(DocumentToRepository documentToRepository)
        {
            return await _documentService.ToRepository(documentToRepository.Group, documentToRepository.Ids);
        }

        /// <summary>
        /// Update component
        /// </summary>
        /// <param name="nodeBody"></param>
        /// <param name="queryParams"></param>
        /// <returns></returns>
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

                try
                {
                    var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                    if (componentsJson != null)
                        difference.Remove(componentsJson);
                }
                catch { }

                string messageDocument = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.DocumentComponentUpdateDocument, difference);
                string messageFile = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.DocumentComponentUpdateFile, difference);

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
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return nodeEntryAfterUpdate;
        }

        /// <summary>
        /// Creates a new version of component
        /// </summary>
        [HttpPost("{nodeId}/component/{componentId}/content")]
        public async Task<NodeEntry> UpdateContentComponent([FromRoute] DocumentComponentUpdateContent input)
        {
            var componentEntryBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(input.NodeId);
            var componentEntryAfterUpdate = await _componentService.UploadNewVersionComponent(input.NodeId, input.ComponentId, input.FileData);

            componentEntryAfterUpdate = await _validationService.CheckOutputFormat(componentEntryAfterUpdate?.Entry?.Id, input.FileData);

            try
            {
                var componentPid = componentEntryAfterUpdate?.GetPid();
                
                await _auditLogService.Record(input.NodeId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.NovaVerze,
                    componentEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                    componentEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                    TransactinoHistoryMessages.DocumentComponentDownloadDocument);

                var fileId = await _documentService.GetDocumentFileId(input.NodeId);

                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.NovaVerze, TransactinoHistoryMessages.DocumentComponentDownloadFile);

            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return componentEntryAfterUpdate;
        }

        /// <summary>
        /// Update document
        /// </summary>
        /// <param name="nodeBody"></param>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/update")]
        public async Task<NodeEntry> UpdateDocument([FromRoute] NodeUpdate nodeBody, [FromQuery] IncludeFieldsQueryParams queryParams)
        {
            var documentEntryBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(nodeBody?.NodeId);
            var documentEntryAfterUpdate = await _nodesService.Update(nodeBody, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));

            try
            {
                var documentId = documentEntryBeforeUpdate?.Entry?.Id;
                var documentPid = documentEntryBeforeUpdate?.GetPid();

                var difference = _alfrescoModelComparer.CompareProperties(
                    documentEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                    documentEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary());

                try
                {
                    var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                    if (componentsJson != null)
                        difference.Remove(componentsJson);
                }
                catch { }

                string messageDocument = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.DocumentUpdateDocument, difference);
                string messageFile = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.DocumentUpdateFile, difference);

                await _auditLogService.Record(
                    documentId,
                    SpisumNames.NodeTypes.Document,
                    documentPid,
                    NodeTypeCodes.Dokument,
                    EventCodes.Uprava,
                    documentEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                    documentEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                    messageDocument);

                var fileId = await _documentService.GetDocumentFileId(documentId);
                if (fileId != null)
                    await _auditLogService.Record(
                        fileId,
                        SpisumNames.NodeTypes.Document,
                        documentPid,
                        NodeTypeCodes.Dokument,
                        EventCodes.Uprava,
                        documentEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        documentEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        messageFile);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return documentEntryAfterUpdate;
        }

        #endregion
    }
}