using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.Alfresco.Api.Services;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Services
{
    internal class DocumentService : IDocumentService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAuditLogService _auditLogService;
        private readonly IComponentService _componentService;
        private readonly IEmailDataBoxService _emailDataBoxService;
        private readonly IIdentityUser _identityUser;
        private readonly IMapper _mapper;
        private readonly INodesService _nodesService;
        private readonly IPersonService _personService;
        private readonly IRepositoryService _repositoryService;
        private readonly ITransactionHistoryService _transactionHistoryService;
        private readonly IValidationService _validationService;

        #endregion

        #region Constructors

        public DocumentService(
            IAlfrescoHttpClient alfrescoHttpClient,
            IAuditLogService auditLogService,
            IComponentService componentService,
            IEmailDataBoxService emailDataBoxService,
            IIdentityUser identityUser,
            IMapper mapper,
            INodesService nodesService,
            IPersonService personService,
            ITransactionHistoryService transactionHistoryService,
            IRepositoryService repositoryService,
            IValidationService validationService
        )
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _auditLogService = auditLogService;
            _componentService = componentService;
            _emailDataBoxService = emailDataBoxService;
            _identityUser = identityUser;
            _mapper = mapper;
            _nodesService = nodesService;
            _personService = personService;
            _transactionHistoryService = transactionHistoryService;
            _repositoryService = repositoryService;
            _validationService = validationService;
        }

        #endregion

        #region Implementation of IDocumentService

        public async Task<NodeEntry> Borrow(string documentId, string group, string user, bool moveDocument = true)
        {
            await _nodesService.TryUnlockNode(documentId);

            var groupRepository = await _alfrescoHttpClient.GetGroup(_identityUser.RequestGroup);

            var updateBody = await _nodesService.GetNodeBodyUpdateWithPermissions(documentId);

            await _alfrescoHttpClient.UpdateNode(documentId, updateBody
                .AddProperty(SpisumNames.Properties.BorrowDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                .AddProperty(SpisumNames.Properties.Borrower, user)
                .AddProperty(SpisumNames.Properties.BorrowGroup, group)
                .AddProperty(SpisumNames.Properties.ReturnedDate, null)
                .AddProperty(SpisumNames.Properties.RepositoryName, groupRepository?.Entry?.Id)
                .AddPermission(group, $"{GroupPermissionTypes.Consumer}")
                .AddPermission($"{SpisumNames.Prefixes.UserGroup}{user}", $"{GroupPermissionTypes.Consumer}")
            );

            if (moveDocument)
                await _nodesService.MoveByPath(documentId, SpisumNames.Paths.RepositoryRented);

            var node = await _alfrescoHttpClient.NodeLock(documentId, new NodeBodyLock { Type = NodeBodyLockType.FULL });

            try
            {

                var personEntry = await _alfrescoHttpClient.GetPerson(user);
                var groupEntry = await _alfrescoHttpClient.GetGroup(group);

                var documentPid = node?.GetPid();

                // Audit log for a document
                await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.VyjmutiZUkladaciJednotky,
                    string.Format(TransactinoHistoryMessages.DocumentBorrow, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));
            }
            catch { }

            return node;
        }

        public async Task<NodeEntry> Create(string nodeType, string relativePath, string documentForm = null, string nodeId = null)
        {
            EmailOrDataboxEnum? type = null;
            string nodePath = null;

            if (nodeId != null)
            {
                var originNodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString)));

                if (originNodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    return originNodeInfo;

                var parent = await _alfrescoHttpClient.GetNodeParents(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Components}')", ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                if (parent.List.Entries.Count > 0)
                    return _mapper.Map<NodeEntry>(parent.List.Entries.First());

                var parentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                   .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                nodePath = parentInfo.Entry.Path.Name;
                type = GetEmailDataboxTypeByPath(nodePath);
            }

            if (type != null && documentForm == null)
                documentForm = SpisumNames.Form.Digital;

            if (type != null && documentForm != SpisumNames.Form.Digital)
                throw new BadRequestException("Digital document type expected");

            var personGroup = await _personService.GetCreateUserGroup();

            var responseCreate = await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new FormDataParam(new byte[] { 01 }), ImmutableList<Parameter>.Empty
                .Add(new Parameter(SpisumNames.Properties.DeliveryMode, type?.ToString()?.ToLower(), ParameterType.GetOrPost))
                .Add(new Parameter(AlfrescoNames.Headers.NodeType, nodeType, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Form, documentForm ?? CodeLists.DocumentTypes.Analog, ParameterType.GetOrPost))
                .Add(new Parameter(AlfrescoNames.Headers.RelativePath, relativePath, ParameterType.GetOrPost))
                .Add(new Parameter(AlfrescoNames.ContentModel.Owner, personGroup.PersonId, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Group, _identityUser.RequestGroup, ParameterType.GetOrPost))
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                .Add(new Parameter(SpisumNames.Properties.DocumentType, GetDocumentType(type, documentForm), ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Version, 1, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.State, SpisumNames.State.Unprocessed, ParameterType.GetOrPost))
                );

            try
            {
                var nodeEntry = await _alfrescoHttpClient.GetNodeInfo(responseCreate.Entry.Id, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString)));

                if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    await _auditLogService.Record(responseCreate.Entry.Id, SpisumNames.NodeTypes.Document, nodeEntry.GetPid(), NodeTypeCodes.Dokument, EventCodes.Zalozeni, TransactinoHistoryMessages.DocumentCreate);
                if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                    await _auditLogService.Record(responseCreate.Entry.Id, SpisumNames.NodeTypes.Concept, nodeEntry.GetPid(), NodeTypeCodes.Dokument, EventCodes.Zalozeni, TransactinoHistoryMessages.ConceptCreate);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            try
            {
                await _nodesService.CreatePermissions(responseCreate.Entry.Id, personGroup.GroupPrefix, _identityUser.Id, nodeType != SpisumNames.NodeTypes.Concept);
            }
            catch
            {
                await _alfrescoHttpClient.DeleteNode(responseCreate.Entry.Id);
            }

            if (nodePath != null && type != null)
                await AssociateEmailDataBox(responseCreate.Entry.Id, nodePath, nodeId, (EmailOrDataboxEnum)type);

            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(responseCreate.Entry.Id, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString)));

            await _componentService.UpdateAssociationCount(responseCreate.Entry.Id);

            return nodeInfo;
        }

        public async Task<NodeEntry> DocumentReturnForRework(string documentId, string reason)
        {
            if (await _nodesService.IsNodeLocked(documentId))
                await _nodesService.UnlockAll(documentId);

            var documentInfo = await _alfrescoHttpClient.GetNodeInfo(documentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{ AlfrescoNames.Includes.Permissions }", ParameterType.QueryString)));

            var properties = documentInfo?.Entry.Properties.As<JObject>().ToDictionary();
            var forSignatureGroup = properties.GetNestedValueOrDefault(SpisumNames.Properties.ForSignatureGroup)?.ToString();
            var forSignatureUser = properties.GetNestedValueOrDefault(SpisumNames.Properties.ForSignatureUser)?.ToString();

            var owner = properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString();
            var ownerGroup = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

            var locallySet = documentInfo?.Entry?.Permissions?.LocallySet;

            var updateBody = new NodeBodyUpdateFixed
            {
                Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
            };

            await _alfrescoHttpClient.UpdateNode(documentId, updateBody
                .AddProperty(SpisumNames.Properties.ReasonForRework, reason)
                .AddProperty(SpisumNames.Properties.ReturnedForReworkDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                .AddProperty(SpisumNames.Properties.ForSignatureGroup, null)
                .AddProperty(SpisumNames.Properties.ForSignatureUser, null)
                .AddProperty(SpisumNames.Properties.ForSignatureDate, null)
            );

            var path = documentInfo?.Entry?.Path?.Name;

            await _nodesService.MoveByPath(documentId, path.Replace("/ForSignature", "").Replace(AlfrescoNames.Prefixes.Path, ""));

            // Remove permissions as admin
            var permissionBody = new NodeBodyUpdateFixed
            {
                Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
            };
            permissionBody
                .RemovePermission($"{forSignatureGroup}_Sign")
                .RemovePermission($"{SpisumNames.Prefixes.UserGroup}{forSignatureUser}");

            var node = await _nodesService.UpdateNodeAsAdmin(documentId, permissionBody);

            try
            {
                var personEntry = await _alfrescoHttpClient.GetPerson(owner);
                var groupEntry = await _alfrescoHttpClient.GetGroup(ownerGroup);

                var documentPid = node?.GetPid();

                // Audit log for a document
                await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.VraceniZAgendy,
                    string.Format(TransactinoHistoryMessages.DocumentReturnForRework, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));

                // Audit log for a file
                var fileId = await GetDocumentFileId(documentId);

                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.VraceniZAgendy,
                        string.Format(TransactinoHistoryMessages.DocumentReturnForRework, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            var takeRef = properties.GetNestedValueOrDefault(SpisumNames.Properties.TakeRef)?.ToString();
            var waitingRef = properties.GetNestedValueOrDefault(SpisumNames.Properties.WaitingRef)?.ToString();

            if (takeRef != null)
                await _nodesService.DeleteNodeAsAdmin(takeRef);
            if (waitingRef != null)
                await _nodesService.DeleteNodeAsAdmin(waitingRef);

            return node;
        }

        public async Task<List<string>> FavoriteAdd(List<string> documentIds)
        {
            List<string> unprocessedDocumentIds = new List<string>();

            foreach (var documentId in documentIds)
            {
                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(documentId, ImmutableList<Parameter>.Empty
                   .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                if (
                    await IsDocumentInFile(documentId) ||
                    !await IsDocument(documentId) ||
                    nodeInfo?.Entry?.Path?.Name.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == false)
                {
                    unprocessedDocumentIds.Add(documentId);
                    continue;
                }

                try
                {
                    await _alfrescoHttpClient.FavoriteAdd(_identityUser.Id, new FavoriteBodyCreate
                    {
                        Target = new FavouriteFile
                        {
                            File = new FavouriteBody
                            {
                                Guid = documentId
                            }
                        }
                    });

                    try
                    {
                        await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Document, nodeInfo?.GetPid(), NodeTypeCodes.Dokument, EventCodes.Uprava,
                            TransactinoHistoryMessages.DocumentFavoriteAdd);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    unprocessedDocumentIds.Add(documentId);
                }
            }

            return unprocessedDocumentIds;
        }

        public async Task<List<string>> FavoriteRemove(List<string> documentIds)
        {
            List<string> unprocessedDocumentIds = new List<string>();

            foreach (var documentId in documentIds)
            {
                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(documentId, ImmutableList<Parameter>.Empty
                  .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                if (await IsDocumentInFile(documentId) ||
                    !await IsDocument(documentId) ||
                    nodeInfo?.Entry?.Path?.Name.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == false)
                {
                    unprocessedDocumentIds.Add(documentId);
                    continue;
                }

                try
                {
                    await _alfrescoHttpClient.FavoriteRemove(_identityUser.Id, documentId);

                    try
                    {
                        await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Document, nodeInfo?.GetPid(), NodeTypeCodes.Dokument, EventCodes.Uprava,
                            TransactinoHistoryMessages.DocumentFavoriteRemove);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch { unprocessedDocumentIds.Add(documentId); }
            }

            return unprocessedDocumentIds;
        }

        public async Task FavoriteRemove(string favouriteId)
        {
            try
            {
                await _alfrescoHttpClient.FavoriteRemove(_identityUser.Id, favouriteId);
            }
            catch { }
        }

        public async Task<List<NodeChildAssociationEntry>> GetComponents(string documentId, bool includePath = false, bool includeProperties = false)
        {
            return await _nodesService.GetSecondaryChildren(documentId, SpisumNames.Associations.Components, includePath, includeProperties);
        }

        public async Task<string> GetDocumentFileId(string documentId)
        {
            var documentFileParent = await _alfrescoHttpClient.GetNodeParents(documentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

            return documentFileParent?.List?.Entries?.FirstOrDefault()?.Entry?.Id;
        }

        public async Task<NodeEntry> ChangeFileMark(string nodeId, string fileMark, ShreddingPlanItemModel plan = null)
        {
            var documentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);
            var documentProperties = documentInfo?.Entry?.Properties?.As<JObject>().ToDictionary();

            DateTime.TryParse(documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.SettleDate)?.ToString(),
                out DateTime settleDate);

            int.TryParse(documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionPeriod)?.ToString(),
                out int retentionPeriod);

            if (plan == null)
            {
                if (await _nodesService.IsNodeLocked(nodeId))
                    await _nodesService.UnlockAll(nodeId);

                var documentFilePlanId = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.FilePlan)?.ToString();
                var fileMarkOld = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.FileMark)?.ToString();
                var retentionModeOld = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionMode)?.ToString();

                // Check if filemark exists
                plan = _nodesService.GetShreddingPlans().FirstOrDefault(x => x.Id == documentFilePlanId)?.Items
                    .FirstOrDefault(x => (x.IsCaption == false || x.IsCaption == null) && x.FileMark == fileMark);

                if (plan == null)
                    throw new BadRequestException("", "Provided FileMark doesn't exists or is not in FilePlan");
            }

            var retentionMode = $"{plan.RetentionMark}/{plan.Period}";

            var node = await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.FileMark, fileMark)
                .AddProperty(SpisumNames.Properties.RetentionMark, plan.RetentionMark)
                .AddProperty(SpisumNames.Properties.RetentionMode, retentionMode)
                .AddProperty(SpisumNames.Properties.RetentionPeriod, plan.Period)
                .AddProperty(SpisumNames.Properties.ShreddingYear, new DateTime(settleDate.Year + 1 + retentionPeriod, 1, 1).ToAlfrescoDateTimeString())
            );

            var takeRefNode = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.TakeRef)?.ToString();
            var waitingRefNode = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.WaitingRef)?.ToString();

            if (!string.IsNullOrWhiteSpace(takeRefNode))
            {
                await _nodesService.UpdateNodeAsAdmin(takeRefNode, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileMark, fileMark)
                    .AddProperty(SpisumNames.Properties.RetentionMark, plan.RetentionMark)
                    .AddProperty(SpisumNames.Properties.RetentionMode, retentionMode)
                    .AddProperty(SpisumNames.Properties.RetentionPeriod, plan.Period)
                    .AddProperty(SpisumNames.Properties.ShreddingYear, new DateTime(settleDate.Year + 1 + retentionPeriod, 1, 1).ToAlfrescoDateTimeString())
                    );
            }
            if (!string.IsNullOrWhiteSpace(waitingRefNode))
            {
                await _nodesService.UpdateNodeAsAdmin(waitingRefNode, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileMark, fileMark)
                    .AddProperty(SpisumNames.Properties.RetentionMark, plan.RetentionMark)
                    .AddProperty(SpisumNames.Properties.RetentionMode, retentionMode)
                    .AddProperty(SpisumNames.Properties.RetentionPeriod, plan.Period)
                    .AddProperty(SpisumNames.Properties.ShreddingYear, new DateTime(settleDate.Year + 1 + retentionPeriod, 1, 1).ToAlfrescoDateTimeString())
                    );
            }

            var parentRm = await _nodesService.GetParentsByAssociation(nodeId, new List<string>
                {
                    SpisumNames.Associations.DocumentInRepository,
                    SpisumNames.Associations.FileInRepository
                });

            if (parentRm != null && parentRm?.Count > 0)
                await _repositoryService.ChangeRetention(parentRm?.FirstOrDefault()?.Entry.Id, plan.RetentionMark, Convert.ToInt32(plan.Period), settleDate, fileMark);

            await _nodesService.LockAll(nodeId);

            try
            {
                if (node?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                {
                    var fileMarkOld = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.FileMark)?.ToString();
                    var retentionModeOld = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionMode)?.ToString();

                    var documentPid = node?.GetPid();

                    // Audit log for a document
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.Uprava,
                        string.Format(TransactinoHistoryMessages.DocumentChangeFileMark, fileMarkOld, fileMark, retentionModeOld, retentionMode));
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return node;
        }

        public async Task<NodeEntry> ChangeLocation(string nodeId, string location, bool? isMemberOfRepository = null)
        {
            isMemberOfRepository ??= await _nodesService.IsMemberOfRepository(_identityUser.RequestGroup);

            if (isMemberOfRepository.Value)
                await _nodesService.TryUnlockNode(nodeId);

            var nodeBeforeupdate = await _alfrescoHttpClient.GetNodeInfo(nodeId);
            var properties = nodeBeforeupdate?.Entry?.Properties?.As<JObject>().ToDictionary();
            var oldLocation = properties.GetNestedValueOrDefault(SpisumNames.Properties.Location)?.ToString();

            var node = await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.Location, location));

            try
            {
                var documentPid = node?.GetPid();

                // Audit log for a document
                await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.VlozeniDoUkladaciJednotky,
                    string.Format(TransactinoHistoryMessages.DocumentChangeLocation, oldLocation, location));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            if (isMemberOfRepository.Value)
                return await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock
                {
                    Type = NodeBodyLockType.FULL
                });

            return node;
        }

        public async Task<bool> IsDocumentInFile(string documentId)
        {
            var documentFileParent = await _alfrescoHttpClient.GetNodeParents(documentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

            return documentFileParent.List.Entries.Count != 0;
        }

        public async Task<List<string>> Recover(List<string> nodeIds, string reason)
        {
            return await _nodesService.Recover(nodeIds, reason, SpisumNames.NodeTypes.Document, SpisumNames.Paths.EvidenceCancelled(_identityUser.RequestGroup));
        }

        public async Task<NodeEntry> Register(NodeUpdate body, GenerateSsid ssidConfiguration)
        {
            var documentInfo = await _alfrescoHttpClient.GetNodeInfo(body.NodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            if (documentInfo?.Entry.NodeType != SpisumNames.NodeTypes.Document)
                throw new BadRequestException("", "Document expected");

            if (documentInfo?.Entry?.Path?.Name?.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomUnfinished) != true)
                throw new BadRequestException("", "Document is not in expected path");

            var properties = documentInfo.Entry.Properties.As<JObject>().ToDictionary();
            var documentType = properties.GetNestedValueOrDefault(SpisumNames.Properties.DocumentType)?.ToString();

            if (documentType == "email")
            {
                await _emailDataBoxService.Register(body, EmailOrDataboxEnum.Email);
            }
            else if (documentType == "databox")
            {
                await _emailDataBoxService.Register(body, EmailOrDataboxEnum.Databox);
            }
            else
            {
                await _nodesService.Update(body);
                await _nodesService.MoveByPath(body.NodeId, SpisumNames.Paths.MailRoomNotPassed);
            }

            return await _nodesService.GenerateSsid(body.NodeId, ssidConfiguration);
        }

        public async Task<NodeEntry> Return(string documentId, bool moveDocument = true)
        {
            bool isMemberOfRepository = await _nodesService.IsMemberOfRepository(_identityUser.RequestGroup);

            var documentInfo = await _alfrescoHttpClient.GetNodeInfo(documentId);
            var documentProperties = documentInfo?.Entry?.Properties?.As<JObject>().ToDictionary();
            var borrowedGroup = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.BorrowGroup)?.ToString();
            var borrowedUser = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.Borrower)?.ToString();


            if (isMemberOfRepository)
            {
                await _nodesService.TryUnlockNode(documentId);

                var updateBody = await _nodesService.GetNodeBodyUpdateWithPermissions(documentId);

                // Member of repository has permission to update
                await _alfrescoHttpClient.UpdateNode(documentId, updateBody
                    .AddProperty(SpisumNames.Properties.BorrowDate, null)
                    .AddProperty(SpisumNames.Properties.Borrower, null)
                    .AddProperty(SpisumNames.Properties.BorrowGroup, null)
                    .AddProperty(SpisumNames.Properties.RepositoryName, null)
                    .AddProperty(SpisumNames.Properties.ReturnedDate, DateTime.UtcNow.ToAlfrescoDateTimeString()));

                await _nodesService.MoveByPath(documentId, SpisumNames.Paths.RepositoryStored);

                await _nodesService.UpdateNodeAsAdmin(documentId, updateBody
                    .RemovePermission(borrowedGroup)
                    .RemovePermission($"{SpisumNames.Prefixes.UserGroup}{borrowedUser}")
                );

                var node = await _alfrescoHttpClient.NodeLock(documentId, new NodeBodyLock { Type = NodeBodyLockType.FULL });

                try
                {
                    var documentPid = node?.GetPid();

                    // Audit log for a document
                    await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.VlozeniDoUkladaciJednotky,
                        TransactinoHistoryMessages.DocumentReturn);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return node;
            }
            else
            {
                await _nodesService.UnlockNodeAsAdmin(documentId);

                var updateBody = await _nodesService.GetNodeBodyUpdateWithPermissionsAsAdmin(documentId);
                // Don't have a permissions to update, update as admin

                await _nodesService.UpdateNodeAsAdmin(documentId, updateBody
                    .AddProperty(SpisumNames.Properties.BorrowDate, null)
                    .AddProperty(SpisumNames.Properties.Borrower, null)
                    .AddProperty(SpisumNames.Properties.BorrowGroup, null)
                    .AddProperty(SpisumNames.Properties.RepositoryName, null)
                    .AddProperty(SpisumNames.Properties.ReturnedDate, DateTime.UtcNow.ToAlfrescoDateTimeString()));

                await _nodesService.MoveByPathAsAdmin(documentId, SpisumNames.Paths.RepositoryStored);

                await _nodesService.UpdateNodeAsAdmin(documentId, updateBody
                    .RemovePermission(borrowedGroup)
                    .RemovePermission($"{SpisumNames.Prefixes.UserGroup}{borrowedUser}")
                );

                var node = await _nodesService.NodeLockAsAdmin(documentId);

                try
                {
                    var documentPid = node?.GetPid();

                    // Audit log for a document
                    await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.VlozeniDoUkladaciJednotky,
                        TransactinoHistoryMessages.DocumentReturn);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return node;
            }
        }

        public async Task<NodeEntry> Revert(string nodeId, string versionId)
        {
            VersionEntry versionEntry = null;
            List<VersionRecord> componentsVersionList = new List<VersionRecord>();
            List<VersionRecord> componentsRevertVersion = new List<VersionRecord>();

            try
            {
                versionEntry = await _alfrescoHttpClient.NodeVersion(nodeId, versionId);
            }
            catch
            {
                throw new BadRequestException("", $"Version {versionId} does not exists");
            }

            var conceptEntry = await _alfrescoHttpClient.GetNodeInfo(nodeId);
            var conceptProperties = conceptEntry?.Entry?.Properties?.As<JObject>().ToDictionary();

            var currentVersion = conceptProperties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.VersionLabel)?.ToString();
            if (currentVersion == versionId)
                throw new BadRequestException("", $"Concept/Document is currently on this version {versionId}");

            var componentsVersion = JsonConvert.DeserializeObject<List<VersionRecord>>(conceptProperties.GetNestedValueOrDefault(SpisumNames.Properties.ComponentVersionJSON)?.ToString());

            var VersionProperties = versionEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
            var componentsRevertVersionJSON = VersionProperties.GetNestedValueOrDefault(SpisumNames.Properties.ComponentVersionJSON);

            if (componentsRevertVersionJSON == null)
            {
                var componentsToDelete = await _nodesService.GetSecondaryChildren(nodeId, SpisumNames.Associations.Components);
                foreach (var component in componentsToDelete)
                    if (!componentsRevertVersion.Exists(x => x.Id == component?.Entry?.Id))
                    {
                        await _alfrescoHttpClient.DeleteSecondaryChildren(nodeId, component?.Entry?.Id);
                        await _alfrescoHttpClient.CreateNodeSecondaryChildren(nodeId, new ChildAssociationBody
                        {
                            ChildId = component.Entry.Id,
                            AssocType = SpisumNames.Associations.DeletedComponents
                        });
                    }
            }
            else
            {
                componentsRevertVersion = JsonConvert.DeserializeObject<List<VersionRecord>>(VersionProperties.GetNestedValueOrDefault(SpisumNames.Properties.ComponentVersionJSON)?.ToString());


                // 1. Revert components that changed their version
                foreach (var componentVersion in componentsVersion)
                {
                    var changedComponent = componentsRevertVersion.FirstOrDefault(x => x.Id == componentVersion.Id && x.Version != componentVersion.Version);

                    if (changedComponent == null)
                        continue;

                    await _alfrescoHttpClient.RevertVersion(changedComponent?.Id, changedComponent?.Version, new RevertBody { MajorVersion = false });
                }

                // 2. Remove components that are not presented in version which is reverting to (e.g. some components does not exists at this time)            
                var components = await _nodesService.GetSecondaryChildren(nodeId, SpisumNames.Associations.Components);
                var deletedComponents = await _nodesService.GetSecondaryChildren(nodeId, SpisumNames.Associations.DeletedComponents);
                foreach (var component in components)
                    if (!componentsRevertVersion.Exists(x => x.Id == component?.Entry?.Id))
                    {
                        await _alfrescoHttpClient.DeleteSecondaryChildren(nodeId, component?.Entry?.Id);
                        await _alfrescoHttpClient.CreateNodeSecondaryChildren(nodeId, new ChildAssociationBody
                        {
                            ChildId = component.Entry.Id,
                            AssocType = SpisumNames.Associations.DeletedComponents
                        });
                    }

                // 3. Recover and revert component that was deleted later but is present in this version
                foreach (var componentRevert in componentsRevertVersion)
                    if (!components.Exists(x => x.Entry.Id == componentRevert.Id))
                    {
                        var deletedComponent = deletedComponents.FirstOrDefault(x => x.Entry.Id == componentRevert.Id);

                        if (deletedComponent != null)
                        {
                            await _alfrescoHttpClient.DeleteSecondaryChildren(nodeId, componentRevert.Id);
                            await _alfrescoHttpClient.CreateNodeSecondaryChildren(nodeId, new ChildAssociationBody
                            {
                                ChildId = componentRevert.Id,
                                AssocType = SpisumNames.Associations.Components
                            });

                            // Revert version if neccesary
                            var deletedComponentProperties = versionEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
                            var version = conceptProperties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.VersionLabel)?.ToString();

                            if (componentRevert.Version != version)
                                await _alfrescoHttpClient.RevertVersion(componentRevert.Id, componentRevert.Version, new RevertBody { MajorVersion = false });
                        }
                    }
            }

            // Check output format for all components
            var currentComponents = await _nodesService.GetSecondaryChildren(nodeId, SpisumNames.Associations.Components);
            await currentComponents?.ForEachAsync(async component =>
            {
                await _validationService.CheckOutputFormat(component?.Entry?.Id);
            });

            var nodeInfo = await UpdateMainFileComponentVersionPropertiesRevert(nodeId, versionEntry);
            try
            {
                var documentPid = nodeInfo?.GetPid();

                var propertiesAfterUpdate = nodeInfo?.Entry?.Properties?.As<JObject>().ToDictionary();
                var versionAfterUpdate = propertiesAfterUpdate.GetNestedValueOrDefault(AlfrescoNames.ContentModel.VersionLabel)?.ToString();

                if (conceptEntry?.Entry.NodeType == SpisumNames.NodeTypes.Concept)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Concept, documentPid, NodeTypeCodes.Dokument, EventCodes.NovaVerze,
                        string.Format(TransactinoHistoryMessages.ConceptRevert, versionAfterUpdate, versionId));

                if (conceptEntry?.Entry.NodeType == SpisumNames.NodeTypes.Document)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.NovaVerze,
                        string.Format(TransactinoHistoryMessages.DocumentRevert, versionAfterUpdate, versionId));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return nodeInfo;
        }


        public async Task<NodeEntry> Settle(string nodeId, string settleMethod, DateTime settleDate, string movePath, string customSettleMethod = null, string settleReason = null)
        {
            string[] documentData = { settleMethod, settleDate.ToUniversalTime().ToString(CultureInfo.InvariantCulture), customSettleMethod, settleReason, _identityUser.Id };
            string[] fileData = { DateTime.UtcNow.ToAlfrescoDateTimeString(), _identityUser.Id };

            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);            
            var nodeProperties = nodeInfo?.Entry?.Properties?.As<JObject>().ToDictionary();

            int.TryParse(nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionPeriod)?.ToString(),
                out int retentionPeriod);

            var nodeBody = new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.SettleMethod, settleMethod)
                .AddProperty(SpisumNames.Properties.SettleDate, settleDate.ToUniversalTime())
                .AddProperty(SpisumNames.Properties.CustomSettleMethod, customSettleMethod)
                .AddProperty(SpisumNames.Properties.SettleReason, settleReason)
                .AddProperty(SpisumNames.Properties.Processor, _identityUser.Id)
                .AddProperty(SpisumNames.Properties.ProcessorId, _identityUser.OrganizationUserId)
                .AddProperty(SpisumNames.Properties.ProcessorOrgId, _identityUser.OrganizationId)
                .AddProperty(SpisumNames.Properties.ProcessorOrgName, _identityUser.OrganizationName)
                .AddProperty(SpisumNames.Properties.ProcessorOrgUnit, _identityUser.OrganizationUnit)
                .AddProperty(SpisumNames.Properties.ProcessorOrgAddress, _identityUser.OrganizationAddress)
                .AddProperty(SpisumNames.Properties.ProcessorJob, _identityUser.Job)
                .AddProperty(SpisumNames.Properties.ShreddingYear, new DateTime(settleDate.Year + 1 + retentionPeriod, 1, 1).ToAlfrescoDateTimeString())
                .AddProperty(SpisumNames.Properties.State, SpisumNames.State.Settled)
                .AddProperty(SpisumNames.Properties.TSettle, string.Join("; ", documentData.Where(x => x != null)))
                .AddProperty(SpisumNames.Properties.TClose, string.Join("; ", fileData.Where(x => x != null)));

            var node = await _alfrescoHttpClient.UpdateNode(nodeId, nodeBody, ImmutableList<Parameter>.Empty
                 .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            if (node?.Entry?.Path?.Name?.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsForProcessing(_identityUser.RequestGroup)) == true)
                await _nodesService.MoveByPath(nodeId, SpisumNames.Paths.EvidenceDocumentsProcessed(_identityUser.RequestGroup));
            else if (node?.Entry?.Path?.Name?.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(_identityUser.RequestGroup)) == true)
                await _nodesService.MoveByPath(nodeId, SpisumNames.Paths.EvidenceFilesDocumentsProcessed(_identityUser.RequestGroup));

            await _nodesService.LockAll(nodeId, new List<string>
            {
                SpisumNames.NodeTypes.ShipmentDatabox,
                SpisumNames.NodeTypes.ShipmentEmail,
                SpisumNames.NodeTypes.ShipmentPersonally,
                SpisumNames.NodeTypes.ShipmentPost,
                SpisumNames.NodeTypes.ShipmentPublish
            });

            var documentPid = node?.GetPid();

            try
            {
                if (settleMethod == SpisumNames.SettleMethod.Other)
                {
                    // Audit log for a document
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.Vyrizeni,
                        string.Format(TransactinoHistoryMessages.DocumentSettleMethodOther, settleMethod, customSettleMethod, settleReason));

                    // Audit log for a file
                    var fileId = await GetDocumentFileId(nodeId);

                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.Vyrizeni,
                            string.Format(TransactinoHistoryMessages.DocumentSettleMethodOther, settleMethod, customSettleMethod, settleReason));
                }
                else
                {
                    // Audit log for a document
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.Vyrizeni,
                        string.Format(TransactinoHistoryMessages.DocumentSettle, settleMethod));

                    // Audit log for a file
                    var fileId = await GetDocumentFileId(nodeId);

                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.Vyrizeni,
                            string.Format(TransactinoHistoryMessages.DocumentSettle, settleMethod));
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return node;
        }

        public async Task<NodeEntry> SettleCancel(string nodeId, string reason)
        {
            var finnalyLock = false;

            try
            {
                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (nodeInfo?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(nodeId);
                    finnalyLock = true;
                }

                var node = await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.State, SpisumNames.Other.Unprocessed)
                    .AddProperty(SpisumNames.Properties.SettleMethod, null)
                    .AddProperty(SpisumNames.Properties.SettleDate, null)
                    .AddProperty(SpisumNames.Properties.CustomSettleMethod, null)
                    .AddProperty(SpisumNames.Properties.SettleReason, null)
                    .AddProperty(SpisumNames.Properties.Processor, null)
                    .AddProperty(SpisumNames.Properties.ProcessorId, null)
                    .AddProperty(SpisumNames.Properties.ProcessorOrgId, null)
                    .AddProperty(SpisumNames.Properties.ProcessorOrgName, null)
                    .AddProperty(SpisumNames.Properties.ProcessorOrgUnit, null)
                    .AddProperty(SpisumNames.Properties.ProcessorOrgAddress, null)
                    .AddProperty(SpisumNames.Properties.ShreddingYear, null)
                    .AddProperty(SpisumNames.Properties.ProcessorJob, null));

                var file = await GetDocumentFile(nodeId);

                // Document is in file
                if (file != null)
                {
                    if (file.Path?.Name?.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceFilesClosed(_identityUser.RequestGroup)) == true)
                        // File is closed
                        await _nodesService.OpenFile(file.Id, reason);

                    await _nodesService.MoveByPath(nodeId, SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(_identityUser.RequestGroup));
                }
                else
                {
                    await _nodesService.MoveByPath(nodeId, SpisumNames.Paths.EvidenceDocumentsForProcessing(_identityUser.RequestGroup));
                }

                try
                {
                    var documentPid = node?.GetPid();

                    // Audit log for a document
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.Zruseni,
                        string.Format(TransactinoHistoryMessages.DocumentSettleCancel, reason));

                    // Audit log for a file
                    var fileId = await GetDocumentFileId(nodeId);

                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.Zruseni,
                            string.Format(TransactinoHistoryMessages.DocumentSettleCancel, reason));

                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return node;
            }
            catch (Exception ex)
            {
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
                throw ex;
            }
        }

        public async Task<NodeEntry> ShreddingCancelDiscard(string nodeId, string assocType, bool isDocumentInFile = false)
        {
            await _nodesService.TryUnlockNode(nodeId);

            await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.DiscardDate, null)
                .AddProperty(SpisumNames.Properties.DiscardReason, null)
                .AddProperty(SpisumNames.Properties.DiscardTo, null)
            );

            if (!isDocumentInFile)
            {
                var parentRM = await _alfrescoHttpClient.GetNodeParents(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{assocType}')", ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                var rmNodeId = parentRM?.List?.Entries?.First()?.Entry.Id;

                // Update record management record    
                // Undo Cut off
                try { await _repositoryService.UndoCutOff(rmNodeId); } catch { }

                // Incomplete Record
                await _repositoryService.UncompleteRecord(rmNodeId);

                // Update properties
                await _alfrescoHttpClient.UpdateNode(rmNodeId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.DiscardDate, null)
                    .AddProperty(SpisumNames.Properties.DiscardReason, null)
                    .AddProperty(SpisumNames.Properties.DiscardTo, null)
                );

                // Complete Record
                await _repositoryService.CompleteRecord(rmNodeId);

                // Cut off
                try { await _repositoryService.CutOff(rmNodeId); } catch { }
            }

            var node = await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock { Type = NodeBodyLockType.FULL });

            try
            {
                var documentPid = node?.GetPid();

                // Audit log for a document
                if (node?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.ZruseniPozastaveniSkartacniOperace,
                        TransactinoHistoryMessages.DocumentShreddingDiscardCancel);
                // Audit log for a file
                if (node?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, documentPid, NodeTypeCodes.Spis, EventCodes.ZruseniPozastaveniSkartacniOperace,
                        TransactinoHistoryMessages.FileShreddingDiscardCancel);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return node;
        }

        public async Task<NodeEntry> ShreddingDiscard(string nodeId, DateTime date, string reason, DateTime discardDate, string assocType = null, bool isDocumentInFile = false)
        {
            await _nodesService.TryUnlockNode(nodeId);

            await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.DiscardDate, discardDate.ToAlfrescoDateTimeString())
                .AddProperty(SpisumNames.Properties.DiscardReason, reason)
                .AddProperty(SpisumNames.Properties.DiscardTo, date.ToAlfrescoDateTimeString())
            );

            if (!isDocumentInFile)
            {
                var parentRM = await _alfrescoHttpClient.GetNodeParents(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{assocType}')", ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                var rmNodeId = parentRM?.List?.Entries?.First()?.Entry.Id;

                // Update record management record    
                // Undo Cut off
                await _repositoryService.UndoCutOff(rmNodeId);

                // Incomplete Record
                await _repositoryService.UncompleteRecord(rmNodeId);

                // Update properties
                await _alfrescoHttpClient.UpdateNode(rmNodeId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.DiscardDate, discardDate.ToAlfrescoDateTimeString())
                    .AddProperty(SpisumNames.Properties.DiscardReason, reason)
                    .AddProperty(SpisumNames.Properties.DiscardTo, date.ToAlfrescoDateTimeString())
                );

                // Complete Record
                await _repositoryService.CompleteRecord(rmNodeId);

                // Cut off
                await _repositoryService.CutOff(rmNodeId);
            }

            var node = await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock { Type = NodeBodyLockType.FULL });

            try
            {
                var documentPid = node?.GetPid();

                // Audit log for a document
                if (node?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.PozastaveniSkartacniOperace,
                        string.Format(TransactinoHistoryMessages.DocumentShreddingDiscard, _transactionHistoryService.GetDateFormatForTransaction(discardDate), reason));
                if (node?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, documentPid, NodeTypeCodes.Spis, EventCodes.PozastaveniSkartacniOperace,
                    string.Format(TransactinoHistoryMessages.FileShreddingDiscard, _transactionHistoryService.GetDateFormatForTransaction(discardDate), reason));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }


            return node;
        }

        public async Task ShreddingChange(string nodeId, string retentionMark)
        {
            await _nodesService.TryUnlockNode(nodeId);

            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);
            var documentProperties = nodeInfo?.Entry?.Properties?.As<JObject>().ToDictionary();            
            var retentionMarkOld = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionMark)?.ToString();

            int.TryParse(documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionPeriod)?.ToString(),
                out int retentionPeriod);

            var node = await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.RetentionMark, retentionMark)
                .AddProperty(SpisumNames.Properties.RetentionMode, $"{retentionMark}/{retentionPeriod}")
                .AddProperty(SpisumNames.Properties.RetentionPeriod, retentionPeriod)
                );

            var parentRm = await _nodesService.GetParentsByAssociation(nodeId, new List<string>
                {
                    SpisumNames.Associations.DocumentInRepository,
                    SpisumNames.Associations.FileInRepository
                });

            if (parentRm != null && parentRm?.Count > 0) await _repositoryService.ChangeRetention(parentRm?.FirstOrDefault()?.Entry.Id, retentionMark, retentionPeriod);

            await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock
            {
                Type = NodeBodyLockType.FULL
            });

            try
            {
                if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, node?.GetPid(), NodeTypeCodes.Dokument, EventCodes.Uprava,
                        string.Format(TransactinoHistoryMessages.DocumentChangeRetentionMark, retentionMarkOld, retentionMark));
                else if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, node?.GetPid(), NodeTypeCodes.Spis, EventCodes.Uprava,
                        string.Format(TransactinoHistoryMessages.FileShreddingChange, retentionMarkOld, retentionMark));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        public async Task<List<string>> ToRepository(string group, List<string> documents, bool moveDocument = true)
        {
            List<string> errorIds = new List<string>();
            var validator = new DocumentToRepositoryDocumentValidator(_alfrescoHttpClient, _nodesService);

            await documents.ForEachAsync(async document =>
            {
                try
                {
                    await validator.ValidateAsync(new DocumentProperties(document));

                    if (await _nodesService.IsNodeLocked(document))
                        await _nodesService.UnlockAll(document);

                    await _nodesService.UpdateHandOverRepositoryPermissionsAll(document, group);

                    var node = await _alfrescoHttpClient.UpdateNode(document, new NodeBodyUpdate()
                        .AddProperty(SpisumNames.Properties.State, SpisumNames.State.HandoverToRepository)
                        .AddProperty(SpisumNames.Properties.ToRepositoryDate, DateTime.UtcNow.ToAlfrescoDateTimeString()));

                    if (moveDocument)
                    {
                        var documentInfo = await _alfrescoHttpClient.GetNodeInfo(document, ImmutableList<Parameter>.Empty
                            .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                        var properties = documentInfo.Entry.Properties.As<JObject>().ToDictionary();
                        var groupDocument = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

                        await _nodesService.MoveHandOverPath(document, groupDocument, group, null);
                    }

                    await _nodesService.LockAll(document);

                    try
                    {
                        var documentPid = node?.GetPid();

                        // Audit log for a document
                        await _auditLogService.Record(document, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.PredaniNaSpisovnu,
                            TransactinoHistoryMessages.DocumentToRepository);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    errorIds.Add(document);
                }
            });

            return errorIds;
        }

        #endregion

        #region Private Methods

        private async Task AssociateAllChildren(string documentId, string nodeId)
        {
            if (nodeId == null)
                return;

            var documentPid = string.Empty;

            try
            {
                var documentEntry = await _alfrescoHttpClient.GetNodeInfo(documentId);
                documentPid = documentEntry?.GetPid();
            }
            catch { }

            await _alfrescoHttpClient.CreateNodeSecondaryChildren(documentId, new ChildAssociationBody
            {
                AssocType = SpisumNames.Associations.Components,
                ChildId = nodeId
            });

            var request = new AlfrescoPagingRequest<NodeChildAssociationPagingFixed, List19Fixed, NodeChildAssociationEntry>(
                parameters => _alfrescoHttpClient.GetNodeSecondaryChildren(nodeId, parameters)
            );

            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty);

            if (nodeInfo.Entry.Content.MimeType == "application/zfo")
                await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.Impossible)
                    .AddProperty(SpisumNames.Properties.SafetyElementsCheck, false)
                    .AddProperty(SpisumNames.Properties.CanBeSigned, false));

            var emlzfoFilePid = await _componentService.GenerateComponentPID(documentId, "/", GeneratePIDComponentType.Component);

            await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.Pid, emlzfoFilePid)
            );

            try
            {
                // Audit log for a document
                await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Component, emlzfoFilePid, NodeTypeCodes.Komponenta, EventCodes.VlozeniKDokumentu, TransactinoHistoryMessages.DocumentComponentCreateDocument);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            while (await request.Next())
            {
                var response = request.Response();
                if (!(response?.List?.Entries?.Count > 0))
                    break;

                foreach (var item in response.List.Entries.ToList())
                {
                    if (item.Entry.Content.MimeType == "application/zfo")
                        await _alfrescoHttpClient.UpdateNode(item.Entry.Id, new NodeBodyUpdate()
                            .AddProperty(SpisumNames.Properties.FileIsInOutputFormat, SpisumNames.Global.Impossible)
                            .AddProperty(SpisumNames.Properties.SafetyElementsCheck, false)
                            .AddProperty(SpisumNames.Properties.CanBeSigned, false));

                    await _alfrescoHttpClient.CreateNodeSecondaryChildren(documentId, new ChildAssociationBody
                    {
                        AssocType = SpisumNames.Associations.Components,
                        ChildId = item.Entry.Id
                    });

                    var componentPid = await _componentService.GenerateComponentPID(documentId, "/", GeneratePIDComponentType.Component);

                    await _alfrescoHttpClient.UpdateNode(item.Entry.Id, new NodeBodyUpdate()
                        .AddProperty(SpisumNames.Properties.Pid, componentPid)
                    );

                    // If this method will be use elsewhere, move this into email and databoxes                    
                    await _validationService.CheckOutputFormat(item.Entry.Id);

                    try
                    {
                        // Audit log for a document
                        await _auditLogService.Record(documentId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta,
                            EventCodes.VlozeniKDokumentu, TransactinoHistoryMessages.DocumentComponentCreateDocument);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
            }
        }

        private async Task AssociateEmailDataBox(string nodeId, string nodeMainFilePath, string nodeMainFileId, EmailOrDataboxEnum type)
        {
            var notRegisteredFolder = string.Empty;
            var unprocessedFolder = string.Empty;

            switch (type)
            {
                case EmailOrDataboxEnum.Email:
                    notRegisteredFolder = SpisumNames.Paths.MailRoomEmailNotRegistered;
                    unprocessedFolder = SpisumNames.Paths.MailRoomEmailUnprocessed;
                    break;

                case EmailOrDataboxEnum.Databox:
                    notRegisteredFolder = SpisumNames.Paths.MailRoomDataBoxNotRegistered;
                    unprocessedFolder = SpisumNames.Paths.MailRoomDataBoxUnprocessed;
                    break;
            }

            var pathRegex = new Regex($"({notRegisteredFolder}|{unprocessedFolder})$", RegexOptions.IgnoreCase);

            if (pathRegex.IsMatch(nodeMainFilePath))
                await AssociateAllChildren(nodeId, nodeMainFileId);



        }

        private async Task<NodeAssociation> GetDocumentFile(string documentId)
        {
            var documentFileParent = await _alfrescoHttpClient.GetNodeParents(documentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties}, {AlfrescoNames.Includes.Path}", ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

            return documentFileParent?.List?.Entries?.FirstOrDefault()?.Entry;
        }

        private string GetDocumentType(EmailOrDataboxEnum? type, string documentForm)
        {
            if (type == EmailOrDataboxEnum.Email)
                return "email";
            if (type == EmailOrDataboxEnum.Databox)
                return "databox";

            return documentForm == SpisumNames.Form.Digital ? "technicalDataCarries" : documentForm;
        }

        private EmailOrDataboxEnum? GetEmailDataboxTypeByPath(string nodePath)
        {
            if (nodePath.Contains(SpisumNames.Paths.MailRoomDataBox))
                return EmailOrDataboxEnum.Databox;
            if (nodePath.Contains(SpisumNames.Paths.MailRoomEmail))
                return EmailOrDataboxEnum.Email;
            return null;
        }

        private async Task<bool> IsDocument(string documentId)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(documentId, ImmutableList<Parameter>.Empty);

            return nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.Document;
        }

        private async Task<NodeEntry> UpdateMainFileComponentVersionPropertiesRevert(string nodeId, VersionEntry versionEntry)
        {
            var versionProperties = versionEntry?.Entry?.Properties?.As<JObject>().ToDictionary();

            await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
               .AddProperty(SpisumNames.Properties.ComponentVersion, versionProperties?.GetNestedValueOrDefault(SpisumNames.Properties.ComponentVersion)?.ToString())
               .AddProperty(SpisumNames.Properties.ComponentVersionId, versionProperties?.GetNestedValueOrDefault(SpisumNames.Properties.ComponentVersionId)?.ToString())
               .AddProperty(SpisumNames.Properties.ComponentVersionOperation, versionProperties?.GetNestedValueOrDefault(SpisumNames.Properties.ComponentVersionOperation)?.ToString()));

            return await _componentService.UpgradeDocumentVersion(nodeId);
        }

        #endregion
    }
}
