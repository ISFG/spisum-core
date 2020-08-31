using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Data.Models;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Enums;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using ISFG.SpisUm.ClientSide.Validators;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class FileService : IFileService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAlfrescoModelComparer _alfrescoModelComparer;
        private readonly IAuditLogService _auditLogService;
        private readonly IDocumentService _documentService;
        private readonly IIdentityUser _identityUser;
        private readonly INodesService _nodesService;
        private readonly IValidationService _validationService;

        #endregion

        #region Constructors

        public FileService(
            IAlfrescoHttpClient alfrescoHttpClient, 
            INodesService nodesService, 
            IIdentityUser identityUser, 
            IDocumentService documentService,
            IAuditLogService auditLogService,
            IAlfrescoModelComparer alfrescoModelComparer, 
            IValidationService validationService)
        {
            _nodesService = nodesService;
            _identityUser = identityUser;
            _alfrescoHttpClient = alfrescoHttpClient;
            _documentService = documentService;
            _auditLogService = auditLogService;
            _alfrescoModelComparer = alfrescoModelComparer;
            _validationService = validationService;
        }

        #endregion

        #region Implementation of IFileService

        public async Task<List<string>> AddDocumentsToFile(string fileNodeId, string[] nodeIds)
        {
            var lockedDocuments = new List<string>();
            var finnalyLock = false;

            try
            {
                var fileInfo = await _alfrescoHttpClient.GetNodeInfo(fileNodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (fileInfo?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(fileNodeId);
                    finnalyLock = true;
                }

                var unprocessedIds = new List<string>();
                bool markFileAsFavourite = false;
                int succefullyAttachedDocuments = 0;

                var fileNodeInfo = await _alfrescoHttpClient.GetNodeInfo(fileNodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString)));

                var properties = fileNodeInfo.Entry.Properties.As<JObject>().ToDictionary();
                var fileIdentificator = properties.GetNestedValueOrDefault(SpisumNames.Properties.FileIdentificator);

                FileForm? fileForm = await GetFileForm(fileNodeInfo);

                foreach (var documentNodeId in nodeIds)
                {
                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(documentNodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.IsFavorite},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

                    if (nodeInfo?.Entry?.IsLocked == true)
                    {
                        await _alfrescoHttpClient.NodeUnlock(documentNodeId);
                        lockedDocuments.Add(documentNodeId);
                    }

                    var existingFileParent = await _alfrescoHttpClient.GetNodeParents(documentNodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                    if (existingFileParent.List.Entries.Count != 0 ||
                        nodeInfo?.Entry?.Path?.Name.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        unprocessedIds.Add(documentNodeId);
                        continue;
                    }

                    if (!markFileAsFavourite)
                        markFileAsFavourite = nodeInfo.Entry.IsFavorite ?? false;

                    if (nodeInfo.Entry.IsFavorite.HasValue && nodeInfo.Entry.IsFavorite.Value)
                        await _documentService.FavoriteRemove(documentNodeId);

                    if (nodeInfo.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsForProcessing(_identityUser.RequestGroup), StringComparison.OrdinalIgnoreCase) ||
                    nodeInfo.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsProcessed(_identityUser.RequestGroup), StringComparison.OrdinalIgnoreCase))
                    {

                        try
                        {
                            await _alfrescoHttpClient.CreateNodeSecondaryChildren(fileNodeId, new ChildAssociationBody
                            {
                                AssocType = SpisumNames.Associations.Documents,
                                ChildId = documentNodeId
                            });

                            await _alfrescoHttpClient.UpdateNode(documentNodeId, new NodeBodyUpdate()
                                .AddProperty(SpisumNames.Properties.FileIdentificator, fileIdentificator)
                                .AddProperty(SpisumNames.Properties.Ref, fileNodeId)
                                .AddProperty(SpisumNames.Properties.IsInFile, true));

                            await _nodesService.MoveByPath(
                                documentNodeId,
                                nodeInfo.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsForProcessing(_identityUser.RequestGroup), StringComparison.OrdinalIgnoreCase) ?
                                    SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(_identityUser.RequestGroup) : SpisumNames.Paths.EvidenceFilesDocumentsProcessed(_identityUser.RequestGroup));

                            await _validationService.UpdateFileOutputFormat(fileNodeId);
                            await _validationService.UpdateFileSecurityFeatures(fileNodeId);
                            
                            try
                            {
                                var filePid = fileNodeInfo?.GetPid();
                                var documentPid = nodeInfo?.GetPid();

                                // Audit log for a document
                                await _auditLogService.Record(nodeInfo?.Entry?.Id, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.VlozeniDoSpisu,
                                    string.Format(TransactinoHistoryMessages.FileDocumentAddDocumentDocument, filePid));

                                // Audit log for a file
                                await _auditLogService.Record(fileNodeInfo?.Entry?.Id, SpisumNames.NodeTypes.File, filePid, NodeTypeCodes.Spis, EventCodes.VlozeniDoSpisu,
                                    string.Format(TransactinoHistoryMessages.FileDocumentAddDocumentFile, documentPid));
                            }
                            catch (Exception ex)
                            {
                                Log.Logger?.Error(ex, "Audit log failed");
                            }

                            succefullyAttachedDocuments++;
                        }
                        catch { unprocessedIds.Add(documentNodeId); }
                    }
                    else
                    {
                        unprocessedIds.Add(documentNodeId);
                    }
                }

                // Do not update file if document zero documents were associated with file
                if (succefullyAttachedDocuments != 0)
                {
                    var secondaryChildrens = await _alfrescoHttpClient.GetNodeSecondaryChildren(fileNodeId, ImmutableList<Parameter>.Empty
                                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString)));

                    fileForm = await GetFileFormFromDocument(fileNodeId);

                    await _alfrescoHttpClient.UpdateNode(fileNodeId, new NodeBodyUpdate()
                            .AddProperty(SpisumNames.Properties.AssociationCount, secondaryChildrens?.List?.Entries?.Count)
                            .AddProperty(SpisumNames.Properties.Form, fileForm.ToString().ToLower()));

                    if (markFileAsFavourite)
                        await FavoriteAdd(fileNodeId);
                }

                return unprocessedIds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (lockedDocuments.Count > 0)
                    await lockedDocuments.ForEachAsync(async locNodeId => await _alfrescoHttpClient.NodeLock(locNodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL)));
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(fileNodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<NodeEntry> Borrow(string fileId, string group, string user)
        {
            await _nodesService.TryUnlockNode(fileId);

            var groupRepository = await _alfrescoHttpClient.GetGroup(_identityUser.RequestGroup);

            var documents = await GetDocuments(fileId);

            await documents.ForEachAsync(async document =>
            {
                await _documentService.Borrow(document?.Entry?.Id, group, user, false);
            });

            var updateBody = await _nodesService.GetNodeBodyUpdateWithPermissions(fileId);

            var node = await _alfrescoHttpClient.UpdateNode(fileId, updateBody
                .AddProperty(SpisumNames.Properties.BorrowDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                .AddProperty(SpisumNames.Properties.Borrower, user)
                .AddProperty(SpisumNames.Properties.BorrowGroup, group)
                .AddProperty(SpisumNames.Properties.RepositoryName, groupRepository?.Entry?.DisplayName)
                .AddProperty(SpisumNames.Properties.ReturnedDate, null)
                .AddPermission(group, $"{GroupPermissionTypes.Consumer}")
                .AddPermission($"{SpisumNames.Prefixes.UserGroup}{user}", $"{GroupPermissionTypes.Consumer}")
            );

            await _nodesService.MoveByPath(fileId, SpisumNames.Paths.RepositoryRented);

            await _alfrescoHttpClient.NodeLock(fileId, new NodeBodyLock {Type = NodeBodyLockType.FULL});

            try
            {
                var personEntry = await _alfrescoHttpClient.GetPerson(user);
                var groupEntry = await _alfrescoHttpClient.GetGroup(group);

                var filePid = node?.GetPid();

                // Audit log for a file
                await _auditLogService.Record(fileId, SpisumNames.NodeTypes.File, filePid, NodeTypeCodes.Spis, EventCodes.VyjmutiZUkladaciJednotky,
                    string.Format(TransactinoHistoryMessages.FileBorrow, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return node;
        }

        public async Task<NodeEntry> Close(string nodeId, string settleMethod, DateTime settleDate, string customSettleMethod = null, string settleReason = null)
        {
            var documents = await GetDocuments(nodeId, true, true);
            var validator = new DocumentPropertiesValidator(_alfrescoHttpClient, _identityUser, _nodesService);

            await documents.ForEachAsync(async document => await validator.ValidateAsync(new DocumentProperties(document?.Entry?.Id)));
            await documents.ForEachAsync(async document =>
            {
                var properties = document.Entry.Properties.As<JObject>().ToDictionary();
                var documentState = properties.GetNestedValueOrDefault(SpisumNames.Properties.State)?.ToString();

                if (documentState != SpisumNames.State.Settled)
                    await _documentService.Settle(document?.Entry?.Id, settleMethod, settleDate, SpisumNames.Paths.EvidenceFilesDocumentsProcessed(_identityUser.RequestGroup), customSettleMethod, settleReason);
            });

            var fileInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            var fileProperties = fileInfo?.Entry?.Properties?.As<JObject>().ToDictionary();
            int.TryParse(fileProperties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionPeriod)?.ToString(),
                out int retentionPeriod);

            var nodeBody = new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.ClosureDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                .AddProperty(SpisumNames.Properties.SettleMethod, settleMethod)
                .AddProperty(SpisumNames.Properties.SettleDate, settleDate.ToUniversalTime())
                .AddProperty(SpisumNames.Properties.CustomSettleMethod, customSettleMethod)
                .AddProperty(SpisumNames.Properties.SettleReason, settleReason)
                .AddProperty(SpisumNames.Properties.State, SpisumNames.State.Closed)
                .AddProperty(SpisumNames.Properties.Processor, _identityUser.Id)
                .AddProperty(SpisumNames.Properties.ProcessorId, _identityUser.OrganizationUserId)
                .AddProperty(SpisumNames.Properties.ProcessorOrgId, _identityUser.OrganizationId)
                .AddProperty(SpisumNames.Properties.ProcessorOrgName, _identityUser.OrganizationName)
                .AddProperty(SpisumNames.Properties.ProcessorOrgUnit, _identityUser.OrganizationUnit)
                .AddProperty(SpisumNames.Properties.ProcessorOrgAddress, _identityUser.OrganizationAddress)
                .AddProperty(SpisumNames.Properties.ProcessorJob, _identityUser.Job)
                .AddProperty(SpisumNames.Properties.ShreddingYear, new DateTime(settleDate.Year + 1 + retentionPeriod, 1, 1).ToAlfrescoDateTimeString()
                );

            await _alfrescoHttpClient.UpdateNode(nodeId, nodeBody);
            await _nodesService.MoveByPath(nodeId, SpisumNames.Paths.EvidenceFilesClosed(_identityUser.RequestGroup));

            var fileEntry = await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock {Type = NodeBodyLockType.FULL});

            try
            {
                if (settleMethod == SpisumNames.SettleMethod.Other)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, fileEntry?.GetPid(), NodeTypeCodes.Spis, EventCodes.Vyrizeni,
                        string.Format(TransactinoHistoryMessages.FileCloseSettleOther, settleMethod, customSettleMethod, settleReason));
                else
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, fileEntry?.GetPid(), NodeTypeCodes.Spis, EventCodes.Vyrizeni,
                        string.Format(TransactinoHistoryMessages.FileCloseSettle, settleMethod));

                await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, fileEntry?.GetPid(), NodeTypeCodes.Spis, EventCodes.Uzavreni,
                    TransactinoHistoryMessages.FileClose);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return fileEntry;
        }

        public async Task<NodeEntry> Create(string documentId)
        {
            var finnalyLock = false;

            try
            {
                var documentNodeEntry = await _alfrescoHttpClient.GetNodeInfo(documentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.IsFavorite},{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.IsLocked},{AlfrescoNames.Includes.Permissions}", ParameterType.QueryString)));

                if (documentNodeEntry?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(documentId);
                    finnalyLock = true;
                }

                if (!IsDocumentPropertiesFilled(documentNodeEntry))
                    throw new BadRequestException("", "Document does not have filled mandatory properties");

                var evidenceFilesNode = await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.RelativePath, SpisumNames.Paths.EvidenceFilesOpen(_identityUser.RequestGroup), ParameterType.QueryString)));

                if (!documentNodeEntry.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsForProcessing(_identityUser.RequestGroup), StringComparison.OrdinalIgnoreCase) &&
                    !documentNodeEntry.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsProcessed(_identityUser.RequestGroup), StringComparison.OrdinalIgnoreCase)
                    )
                    throw new BadRequestException("", "Document path is forbidden.");

                // Update all properties to the document
                var properties = documentNodeEntry.Entry.Properties.As<JObject>().ToDictionary();

                var fileIdentificator = SpisumNames.Prefixes.FileSsidPrefix + properties.GetNestedValueOrDefault(SpisumNames.Properties.Ssid);
                var createdDate = properties.GetNestedValueOrDefault(SpisumNames.Properties.CreatedDate)?.ToString();

                if (DateTime.TryParse(properties.GetNestedValueOrDefault(SpisumNames.Properties.CreatedDate)?.ToString(), out DateTime dateCreated)) createdDate = dateCreated.ToAlfrescoDateTimeString();

                createdDate ??= documentNodeEntry.Entry.CreatedAt.UtcDateTime.ToAlfrescoDateTimeString();

                string settleDate = null;
                if (DateTime.TryParse(properties.GetNestedValueOrDefault(SpisumNames.Properties.SettleToDate)?.ToString(), out DateTime date)) settleDate = date.ToAlfrescoDateTimeString();
                var owner = properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString();
                var group = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

                // Create a file
                var nodeFile = await _alfrescoHttpClient.CreateNode(evidenceFilesNode.Entry.Id, new FormDataParam(new byte[] { 01 }), ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.NodeType, SpisumNames.NodeTypes.File, ParameterType.GetOrPost))
                    .Add(new Parameter(AlfrescoNames.ContentModel.Owner, _identityUser.Id, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.AssociationCount, 1, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.FileIdentificator, fileIdentificator, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.DirectionMethod, SpisumNames.Other.Priorace, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Subject, properties.GetNestedValueOrDefault(SpisumNames.Properties.Subject)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Sender, properties.GetNestedValueOrDefault(SpisumNames.Properties.Sender)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.SenderSSID, properties.GetNestedValueOrDefault(SpisumNames.Properties.SenderSSID)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.CreatedDate, createdDate, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.SettleToDate, settleDate, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Group, group, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Form, properties.GetNestedValueOrDefault(SpisumNames.Properties.Form)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Sender_Address, properties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_Address)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Sender_Contact, properties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_Contact)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Sender_Name, properties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_Name)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Sender_OrgName, properties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_OrgName)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Sender_Job, properties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_Job)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Sender_OrgUnit, properties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_OrgUnit)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.SenderType, properties.GetNestedValueOrDefault(SpisumNames.Properties.SenderType)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.SenderIdent, properties.GetNestedValueOrDefault(SpisumNames.Properties.SenderIdent)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.State, SpisumNames.State.Unprocessed, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.RetentionMark, properties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionMark)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.RetentionMode, properties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionMode)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.RetentionPeriod, properties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionPeriod)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.FileMark, properties.GetNestedValueOrDefault(SpisumNames.Properties.FileMark)?.ToString(), ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.FilePlan, properties.GetNestedValueOrDefault(SpisumNames.Properties.FilePlan)?.ToString(), ParameterType.GetOrPost)));
                
                try
                {
                    var fileInfo = await _alfrescoHttpClient.GetNodeInfo(nodeFile?.Entry?.Id);

                    await _auditLogService.Record(nodeFile.Entry.Id, SpisumNames.NodeTypes.File, fileInfo?.GetPid(), NodeTypeCodes.Spis, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.FileCreate);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                // Associate document to the file
                await _alfrescoHttpClient.CreateNodeSecondaryChildren(nodeFile.Entry.Id, new ChildAssociationBody
                {
                    AssocType = SpisumNames.Associations.Documents,
                    ChildId = documentId
                });

                if (documentNodeEntry.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsForProcessing(_identityUser.RequestGroup), StringComparison.OrdinalIgnoreCase))
                    await _nodesService.MoveByPath(documentNodeEntry.Entry.Id, SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(_identityUser.RequestGroup));
                else if (documentNodeEntry.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsProcessed(_identityUser.RequestGroup), StringComparison.OrdinalIgnoreCase))
                    await _nodesService.MoveByPath(documentNodeEntry.Entry.Id, SpisumNames.Paths.EvidenceFilesDocumentsProcessed(_identityUser.RequestGroup));

                if (documentNodeEntry.Entry.IsFavorite.HasValue && documentNodeEntry.Entry.IsFavorite.Value)
                {
                    await _documentService.FavoriteRemove(documentId);
                    await FavoriteAdd(nodeFile.Entry.Id);
                }

                await _nodesService.UpdateNodeAsAdmin(nodeFile.Entry.Id, new NodeBodyUpdate
                { 
                    Permissions = new PermissionsBody
                    {
                        IsInheritanceEnabled = false,
                        LocallySet = documentNodeEntry.Entry.Permissions.LocallySet 
                    }
                }.AddProperty(AlfrescoNames.ContentModel.Owner, owner));

                var documentEntry = await _alfrescoHttpClient.UpdateNode(documentId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileIdentificator, fileIdentificator)
                    .AddProperty(SpisumNames.Properties.Ref, nodeFile.Entry.Id)
                    .AddProperty(SpisumNames.Properties.IsInFile, true));

                await _validationService.UpdateFileOutputFormat(nodeFile?.Entry?.Id);
                await _validationService.UpdateFileSecurityFeatures(nodeFile?.Entry?.Id);
                
                try
                {
                    var fileInfo = await _alfrescoHttpClient.GetNodeInfo(nodeFile?.Entry?.Id);

                    var filePid = fileInfo?.GetPid();
                    var documentPid = documentEntry?.GetPid();

                    // Audit log for a document
                    await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Spis, EventCodes.VlozeniDoSpisu,
                        string.Format(TransactinoHistoryMessages.FileCreateAddDocument, filePid));

                    // Audit log for a file
                    await _auditLogService.Record(nodeFile.Entry.Id, SpisumNames.NodeTypes.File, filePid, NodeTypeCodes.Spis, EventCodes.VlozeniDoSpisu,
                        string.Format(TransactinoHistoryMessages.FileCreateAddFile, documentPid));
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return nodeFile;

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(documentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<List<string>> FavoriteAdd(List<string> fileIds)
        {
            List<string> unprocessedFileIds = new List<string>();

            foreach (var fileId in fileIds)
                try
                {
                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(fileId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                    if (!await IsFile(fileId) ||
                        nodeInfo?.Entry?.Path?.Name.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        unprocessedFileIds.Add(fileId);
                        continue;
                    }

                    await FavoriteAdd(fileId);
                }
                catch
                {
                    unprocessedFileIds.Add(fileId);
                }

            return unprocessedFileIds;
        }

        public async Task<List<string>> FavoriteRemove(List<string> favouriteIds)
        {
            List<string> unprocessedFileIds = new List<string>();

            foreach (var fileId in favouriteIds)
                try
                {
                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(fileId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                    if (!await IsFile(fileId) || 
                        nodeInfo?.Entry?.Path?.Name.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        unprocessedFileIds.Add(fileId);
                        continue;
                    }

                    await _alfrescoHttpClient.FavoriteRemove(_identityUser.Id ?? AlfrescoNames.Aliases.Me, fileId);

                    try
                    {
                        var fileEntry = await _alfrescoHttpClient.GetNodeInfo(fileId);

                        // Audit log for a file
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.File, fileEntry?.GetPid(), NodeTypeCodes.Spis, EventCodes.Uprava,
                            TransactinoHistoryMessages.FileFavoriteRemove);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    unprocessedFileIds.Add(fileId);
                }

            return unprocessedFileIds;
        }

        public async Task<List<NodeChildAssociationEntry>> GetDocuments(string fileId, bool includePath = false, bool includeProperties = false)
        {
            return await _nodesService.GetSecondaryChildren(fileId, SpisumNames.Associations.Documents, includePath, includeProperties);
        }

        public async Task<NodeEntry> ChangeFileMark(string nodeId, string fileMark)
        {
            if (await _nodesService.IsNodeLocked(nodeId))
                await _nodesService.UnlockAll(nodeId);

            var fileInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);
            var documentProperties = fileInfo?.Entry?.Properties?.As<JObject>().ToDictionary();

            var documentFilePlanId = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.FilePlan)?.ToString();

            // Check if filemark exists
            var plan = _nodesService.GetShreddingPlans().FirstOrDefault(x => x.Id == documentFilePlanId)?.Items
                .FirstOrDefault(x => (x.IsCaption == false || x.IsCaption == null) && x.FileMark == fileMark);

            var retentionMode = $"{plan.RetentionMark}/{plan.Period}";

            if (plan == null)
                throw new BadRequestException("", "Provided FileMark doesn't exists or is not in FilePlan");

            var documents = await GetDocuments(nodeId);

            await documents.ForEachAsync(async x =>
            {
                await _documentService.ChangeFileMark(x?.Entry?.Id, fileMark, plan);
            });

            var node = await _documentService.ChangeFileMark(nodeId, fileMark, plan);

            // changefilemark will lock file again
            if (await _nodesService.IsNodeLocked(nodeId))
                await _nodesService.UnlockAll(nodeId);

            await _nodesService.LockAll(nodeId);

            try
            {
                var fileMarkOld = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.FileMark)?.ToString();
                var retentionModeOld = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionMode)?.ToString();

                var filePid = fileInfo?.GetPid();

                // Audit log for a document
                await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, filePid, NodeTypeCodes.Spis, EventCodes.Uprava,
                    string.Format(TransactinoHistoryMessages.DocumentChangeFileMark, fileMarkOld, fileMark, retentionModeOld, retentionMode));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return node;
        }

        public async Task<NodeEntry> ChangeLocation(string nodeId, string location)
        {
            var isMemberOfRepository = await _nodesService.IsMemberOfRepository(_identityUser.RequestGroup);

            var nodeBeforeupdate = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            if (isMemberOfRepository)
                await _nodesService.TryUnlockNode(nodeId);

            var fileDocuments = await GetDocuments(nodeId);

            await fileDocuments.ForEachAsync(async x =>
            {
                await _documentService.ChangeLocation(x?.Entry?.Id, location, isMemberOfRepository);
            });

            var node = await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.Location, location));

            var nodeEntry = await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock
            {
                Type = NodeBodyLockType.FULL
            });

            try
            {                
                var properties = nodeBeforeupdate?.Entry?.Properties?.As<JObject>().ToDictionary();
                var oldLocation = properties.GetNestedValueOrDefault(SpisumNames.Properties.Location)?.ToString();

                var filePid = node?.GetPid();

                // Audit log for a file
                await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, filePid, NodeTypeCodes.Spis, EventCodes.VlozeniDoUkladaciJednotky,
                    string.Format(TransactinoHistoryMessages.FileChangeLocation, oldLocation, location));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return nodeEntry;
        }

        public async Task<List<string>> Recover(List<string> nodeIds, string reason)
        {
            return await _nodesService.Recover(nodeIds, reason, SpisumNames.NodeTypes.File, SpisumNames.Paths.EvidenceCancelled(_identityUser.RequestGroup));
        }

        public async Task<List<string>> RemoveDocumentsFromFile(string fileNodeId, string[] nodeIds)
        {
            List<string> unprocessedIds = new List<string>();

            var fileNodeInfo = await _alfrescoHttpClient.GetNodeInfo(fileNodeId, ImmutableList<Parameter>.Empty
                               .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString)));

            FileForm? fileForm = await GetFileForm(fileNodeInfo);

            var filePid = fileNodeInfo?.GetPid();

            // Process documents
            foreach (var documentNodeId in nodeIds)
                try
                {
                    string pathToMove = null;
                    var documentInfo = await _alfrescoHttpClient.GetNodeInfo(documentNodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.IsFavorite},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

                    if (documentInfo?.Entry?.Path?.Name == AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(_identityUser.RequestGroup))
                        pathToMove = SpisumNames.Paths.EvidenceDocumentsForProcessing(_identityUser.RequestGroup);
                    else if (documentInfo?.Entry?.Path?.Name == AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceFilesDocumentsForProcessingForSignature(_identityUser.RequestGroup))
                        pathToMove = SpisumNames.Paths.EvidenceDocumentsForProcessingForSignature(_identityUser.RequestGroup);
                    else if (documentInfo?.Entry?.Path?.Name == AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceFilesDocumentsProcessed(_identityUser.RequestGroup))
                        pathToMove = SpisumNames.Paths.EvidenceDocumentsProcessed(_identityUser.RequestGroup);

                    if (pathToMove == null)
                    {
                        unprocessedIds.Add(documentNodeId);
                        continue;
                    }

                    if (documentInfo?.Entry?.IsLocked == true)
                        await _alfrescoHttpClient.NodeUnlock(documentNodeId);

                    // Remove association
                    await _alfrescoHttpClient.DeleteSecondaryChildren(fileNodeId, documentNodeId);
                    
                    await _alfrescoHttpClient.UpdateNode(documentNodeId, new NodeBodyUpdate()
                        .AddProperty(SpisumNames.Properties.FileIdentificator, null)
                        .AddProperty(SpisumNames.Properties.Ref, null)
                        .AddProperty(SpisumNames.Properties.IsInFile, false));

                    await _nodesService.MoveByPath(documentNodeId, pathToMove);

                    if (documentInfo?.Entry?.IsLocked == true)
                        await _alfrescoHttpClient.NodeLock(documentNodeId, new NodeBodyLock() { Type = NodeBodyLockType.FULL });

                    try
                    {
                        var documentPid = documentInfo?.GetPid();

                        // Audit log for a document
                        await _auditLogService.Record(documentInfo?.Entry?.Id, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Spis, EventCodes.VyjmutiZeSpisu,
                            string.Format(TransactinoHistoryMessages.FileDocumentRemoveDocumentDocument, filePid));

                        // Audit log for a file
                        await _auditLogService.Record(fileNodeInfo?.Entry?.Id, SpisumNames.NodeTypes.File, filePid, NodeTypeCodes.Spis, EventCodes.VyjmutiZeSpisu,
                            string.Format(TransactinoHistoryMessages.FileDocumentRemoveDocumentFile, documentPid));
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    unprocessedIds.Add(documentNodeId);
                }

            // Not any has been removed - return
            if (unprocessedIds.Count == nodeIds.Count())
                return unprocessedIds;
            
            await _validationService.UpdateFileOutputFormat(fileNodeId);
            await _validationService.UpdateFileSecurityFeatures(fileNodeId);

            var childrens = await _alfrescoHttpClient.GetNodeSecondaryChildren(fileNodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString)));

            fileForm = await GetFileFormFromDocument(fileNodeId);

            await _alfrescoHttpClient.UpdateNode(fileNodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.AssociationCount, childrens?.List.Pagination?.TotalItems ?? 0)
                .AddPropertyIfNotNull(SpisumNames.Properties.Form, childrens?.List?.Pagination?.TotalItems != 0 ? fileForm.ToString().ToLower() : null));

            return unprocessedIds;
        }

        public async Task<NodeEntry> Return(string fileId)
        {
            var isMemberOfRepository = await _nodesService.IsMemberOfRepository(_identityUser.RequestGroup);

            var documentInfo = await _alfrescoHttpClient.GetNodeInfo(fileId);
            var documentProperties = documentInfo?.Entry?.Properties?.As<JObject>().ToDictionary();
            var borrowedGroup = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.BorrowGroup)?.ToString();
            var borrowedUser = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.Borrower)?.ToString();

            if (isMemberOfRepository)
            {
                await _nodesService.TryUnlockNode(fileId);

                var updateBody = await _nodesService.GetNodeBodyUpdateWithPermissions(fileId);

                // Member of repository has permission to update
                await _alfrescoHttpClient.UpdateNode(fileId, updateBody
                    .AddProperty(SpisumNames.Properties.BorrowDate, null)
                    .AddProperty(SpisumNames.Properties.Borrower, null)
                    .AddProperty(SpisumNames.Properties.BorrowGroup, null)
                    .AddProperty(SpisumNames.Properties.RepositoryName, null)
                    .AddProperty(SpisumNames.Properties.ReturnedDate, DateTime.UtcNow.ToAlfrescoDateTimeString()));

                await _nodesService.MoveByPath(fileId, SpisumNames.Paths.RepositoryStored);

                await _nodesService.UpdateNodeAsAdmin(fileId, updateBody
                    .RemovePermission(borrowedGroup)
                    .RemovePermission($"{SpisumNames.Prefixes.UserGroup}{borrowedUser}")
                );

                var node = await _alfrescoHttpClient.NodeLock(fileId, new NodeBodyLock { Type = NodeBodyLockType.FULL });

                try
                {
                    var documentPid = node?.GetPid();

                    // Audit log for a document
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.File, documentPid, NodeTypeCodes.Spis, EventCodes.VlozeniDoUkladaciJednotky,
                        TransactinoHistoryMessages.FileReturn);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return node;
            }
            else
            {
                await _nodesService.UnlockNodeAsAdmin(fileId);

                var updateBody = await _nodesService.GetNodeBodyUpdateWithPermissionsAsAdmin(fileId);

                // Don't have a permissions to update, update as admin

                await _nodesService.UpdateNodeAsAdmin(fileId, updateBody
                    .AddProperty(SpisumNames.Properties.BorrowDate, null)
                    .AddProperty(SpisumNames.Properties.Borrower, null)
                    .AddProperty(SpisumNames.Properties.BorrowGroup, null)
                    .AddProperty(SpisumNames.Properties.RepositoryName, null)
                    .AddProperty(SpisumNames.Properties.ReturnedDate, DateTime.UtcNow.ToAlfrescoDateTimeString()));

                await _nodesService.MoveByPathAsAdmin(fileId, SpisumNames.Paths.RepositoryStored);

                await _nodesService.UpdateNodeAsAdmin(fileId, updateBody
                    .RemovePermission(borrowedGroup)
                    .RemovePermission($"{SpisumNames.Prefixes.UserGroup}{borrowedUser}")
                );

                var node = await _nodesService.NodeLockAsAdmin(fileId);

                try
                {
                    var documentPid = node?.GetPid();

                    // Audit log for a document
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.File, documentPid, NodeTypeCodes.Spis, EventCodes.VlozeniDoUkladaciJednotky,
                        TransactinoHistoryMessages.FileReturn);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return node;
            }
        }

        public async Task<NodeEntry> ShreddingCancelDiscard(string nodeId)
        {
            var documents = await GetDocuments(nodeId);

            await documents.ForEachAsync(async document =>
            {
                await _documentService.ShreddingCancelDiscard(document?.Entry?.Id);
            });

            return await _documentService.ShreddingCancelDiscard(nodeId);
        }

        public async Task<NodeEntry> ShreddingDiscard(string nodeId, DateTime date, string reason)
        {
            var documents = await GetDocuments(nodeId);

            var shreddingDate = DateTime.UtcNow;

            await documents.ForEachAsync(async document =>
            {
                await _documentService.ShreddingDiscard(document?.Entry?.Id, date, reason, shreddingDate);
            });

            return await _documentService.ShreddingDiscard(nodeId, date, reason, shreddingDate);
        }

        public async Task ShreddingChange(string nodeId, string retentionMark)
        {
            var fileEntry = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            await _documentService.ShreddingChange(nodeId, retentionMark);
            
            var documents = await GetDocuments(nodeId);

            await documents.ForEachAsync(async document => await _documentService.ShreddingChange(document.Entry.Id, retentionMark));
        }

        public async Task<List<string>> ToRepository(string group, List<string> files)
        {
            List<string> errorIds = new List<string>();
            var fileValidator = new FileToRepositoryFileValidator(_alfrescoHttpClient);
  
            await files.ForEachAsync(async file =>
            {
                try
                {
                    await fileValidator.ValidateAsync(new DocumentProperties(file));
                    
                    var documents = (await GetDocuments(file, true, true)).Select(x => x.Entry.Id).ToList();
                    var docsRepository = await _documentService.ToRepository(group, documents, false);
                    if (docsRepository != null && docsRepository.Any())
                        throw new Exception();

                    if (await _nodesService.IsNodeLocked(file))
                        await _nodesService.UnlockAll(file);

                    await _nodesService.UpdateHandOverRepositoryPermissionsAll(file, group);

                    var fileEntry = await _alfrescoHttpClient.UpdateNode(file, new NodeBodyUpdate()
                        .AddProperty(SpisumNames.Properties.State, SpisumNames.State.HandoverToRepository)
                        .AddProperty(SpisumNames.Properties.ToRepositoryDate, DateTime.UtcNow.ToAlfrescoDateTimeString()));

                    // move file
                    var documentInfo = await _alfrescoHttpClient.GetNodeInfo(file, ImmutableList<Parameter>.Empty
                           .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                    var properties = documentInfo.Entry.Properties.As<JObject>().ToDictionary();
                    var groupFile = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

                    await _nodesService.MoveHandOverPath(file, groupFile, group, null);

                    await _nodesService.LockAll(file);

                    try
                    {
                        // Audit log for a file
                        await _auditLogService.Record(file, SpisumNames.NodeTypes.File, fileEntry?.GetPid(), NodeTypeCodes.Spis, EventCodes.PredaniNaSpisovnu,
                            TransactinoHistoryMessages.FileToRepository);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    errorIds.Add(file);
                }
            });

            return errorIds;
        }

        public async Task<NodeEntry> Update(NodeUpdate nodeBody, ImmutableList<Parameter> queryParams)
        {
            var fileEntryBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(nodeBody?.NodeId);

            var fileId = fileEntryBeforeUpdate?.Entry?.Id;
            var filePid = fileEntryBeforeUpdate?.GetPid();

            var fileEntryAfterUpdate = await _nodesService.Update(nodeBody, queryParams);

            var difference = _alfrescoModelComparer.CompareProperties(
                fileEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                fileEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary());

            if (difference.Count > 0)
            {
                try
                {
                    var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                    if (componentsJson != null)
                        difference.Remove(componentsJson);
                }
                catch { }

                string messageFile = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.FileUpdate, difference);

                await UpdateDocumentsFileFilePlan(fileEntryAfterUpdate);

                try
                {
                    await _auditLogService.Record(
                        fileId,
                        SpisumNames.NodeTypes.File,
                        filePid,
                        NodeTypeCodes.Spis,
                        EventCodes.Uprava,
                        fileEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        fileEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        messageFile);

                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }
            }

            return fileEntryAfterUpdate;
        }

        #endregion

        #region Public Methods

        public async Task<FavoriteEntry> FavoriteAdd(string fileId)
        {
            var favoriteEntry = await _alfrescoHttpClient.FavoriteAdd(_identityUser.Id, new FavoriteBodyCreate
            {
                Target = new FavouriteFile
                {
                    File = new FavouriteBody
                    {
                        Guid = fileId
                    }
                }
            });

            try
            {
                var fileEntry = await _alfrescoHttpClient.GetNodeInfo(fileId);

                // Audit log for a file
                await _auditLogService.Record(fileId, SpisumNames.NodeTypes.File, fileEntry?.GetPid(), NodeTypeCodes.Spis, EventCodes.Uprava,
                    TransactinoHistoryMessages.FileFavoriteAdd);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return favoriteEntry;
        }

        #endregion

        #region Private Methods

        private async Task<FileForm?> GetFileForm(string fileNodeId)
        {
            var fileNodeInfo = await _alfrescoHttpClient.GetNodeInfo(fileNodeId, ImmutableList<Parameter>.Empty
                   .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString)));

            return await GetFileForm(fileNodeInfo);
        }     
        private async Task<FileForm?> GetFileForm(NodeEntry nodeEntry)
        {
            try
            {
                var properties = nodeEntry.Entry.Properties.As<JObject>().ToDictionary();

                if (properties == null && properties.Count == 0)
                {
                    // If nodeEntry does not contains properties
                    var fileNodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeEntry.Entry.Id, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString)));

                    properties = fileNodeInfo.Entry.Properties.As<JObject>().ToDictionary();
                }

                var form = properties.GetNestedValueOrDefault(SpisumNames.Properties.Form)?.ToString();

                if (form.Equals(SpisumNames.Form.Digital))
                    return FileForm.Digital;
                if (form.Equals(SpisumNames.Form.Analog))
                    return FileForm.Analog;
                return FileForm.Hybrid;
            }
            catch { return null; }
        }

        private async Task<FileForm?> GetFileFormFromDocument(string fileId)
        {
            List<string> forms = new List<string>();

            var documents = await _nodesService.GetSecondaryChildren(fileId, SpisumNames.Associations.Documents, false, true);

            documents.ForEach(x =>
            {
                var properties = x?.Entry?.Properties?.As<JObject>().ToDictionary();
                var form = properties?.GetNestedValueOrDefault(SpisumNames.Properties.Form)?.ToString();

                if (form != null)
                    forms.Add(form);
                else
                    Log.Error($"Document {x?.Entry?.Id} in file {fileId} has null property {SpisumNames.Properties.Form}");
            });

            if (documents.Count == 0)
                return await GetFileForm(fileId);

            if (forms.All(x => x.Equals(SpisumNames.Form.Analog)))
                return FileForm.Analog;
            else if (forms.All(x => x.Equals(SpisumNames.Form.Digital)))
                return FileForm.Digital;
            else
                return FileForm.Hybrid;
        }

        private bool IsDocumentPropertiesFilled(NodeEntry documentInfo)
        {
            var documentProperties = documentInfo.Entry.Properties.As<JObject>().ToDictionary();

            var senderContact = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_Contact)?.ToString();
            var senderOrgName = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_OrgName)?.ToString();
            var senderName = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_Name)?.ToString();

            return !string.IsNullOrEmpty(senderContact) || !string.IsNullOrEmpty(senderOrgName) || !string.IsNullOrEmpty(senderName);
        }

        private async Task<bool> IsFile(string documentId)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(documentId, ImmutableList<Parameter>.Empty);

            return nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.File;
        }

        private async Task UpdateDocumentsFileFilePlan(NodeEntry fileEntry)
        {
            var properties = fileEntry?.Entry?.Properties.As<JObject>().ToDictionary();

            var documents = await GetDocuments(fileEntry?.Entry?.Id);

            await documents.ForEachAsync(async documentEntry =>
            {
                var documentEntryBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(documentEntry?.Entry?.Id, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                var documentId = documentEntryBeforeUpdate?.Entry?.Id;
                var documentPid = documentEntryBeforeUpdate?.GetPid();

                if (documentEntryBeforeUpdate?.Entry?.IsLocked == true)
                    await _alfrescoHttpClient.NodeUnlock(documentEntryBeforeUpdate?.Entry?.Id);

                var documentEntryAfterUpdate = await _alfrescoHttpClient.UpdateNode(documentEntry?.Entry?.Id, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileMark, properties.GetNestedValueOrDefault(SpisumNames.Properties.FileMark))
                    .AddProperty(SpisumNames.Properties.FilePlan, properties.GetNestedValueOrDefault(SpisumNames.Properties.FilePlan))
                    .AddProperty(SpisumNames.Properties.RetentionMode, properties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionMode))
                    .AddProperty(SpisumNames.Properties.RetentionMark, properties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionMark))
                    .AddProperty(SpisumNames.Properties.RetentionPeriod, properties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionPeriod))
                    );

                if (documentEntryBeforeUpdate?.Entry?.IsLocked == true)
                    await _alfrescoHttpClient.NodeLock(documentEntryBeforeUpdate?.Entry?.Id, new NodeBodyLock() { Type = NodeBodyLockType.FULL });

                var difference = _alfrescoModelComparer.CompareProperties(
                    documentEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                    documentEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary());

                if (difference.Count > 0)
                {
                    try
                    {
                        var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                        if (componentsJson != null)
                            difference.Remove(componentsJson);
                    }
                    catch { }

                    string messageDocument = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.DocumentUpdateDocument, difference);

                    try
                    {
                        await _auditLogService.Record(
                            documentId,
                            SpisumNames.NodeTypes.Document,
                            documentPid,
                            NodeTypeCodes.Dokument,
                            EventCodes.Uprava,
                            documentEntryBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                            documentEntryAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                            messageDocument);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
            });
        }

        #endregion
    }
}
