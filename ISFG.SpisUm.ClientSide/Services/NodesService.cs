using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.Data.Models;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class NodesService : INodesService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _adminAlfrescoHttpClient;

        private readonly IAlfrescoConfiguration _alfrescoConfiguration;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAuditLogService _auditLogService;
        private readonly IIdentityUser _identityUser;
        private readonly IMapper _mapper;
        private readonly IRepositoryService _repositoryService;
        private readonly ISimpleMemoryCache _simpleMemoryCache;
        private readonly ITransactionHistoryService _transactionHistoryService;

        #endregion

        #region Constructors

        public NodesService(
            IAlfrescoConfiguration alfrescoConfiguration,
            IAlfrescoHttpClient alfrescoHttpClient,
            IAuditLogService auditLogService,
            IIdentityUser identityUser,
            IMapper mapper,
            ISimpleMemoryCache simpleMemoryCache,
            ITransactionHistoryService transactionHistoryService,
            ISystemLoginService systemLoginService,
            IRepositoryService repositoryService
        )
        {
            _alfrescoConfiguration = alfrescoConfiguration;
            _alfrescoHttpClient = alfrescoHttpClient;
            _auditLogService = auditLogService;
            _identityUser = identityUser;
            _mapper = mapper;
            _simpleMemoryCache = simpleMemoryCache;
            _transactionHistoryService = transactionHistoryService;
            _repositoryService = repositoryService;
            _adminAlfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));
        }

        #endregion

        #region Implementation of INodesService

        public async Task AcceptOwner(string nodeId)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties},{AlfrescoNames.Includes.Permissions},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

            if (nodeInfo?.Entry?.IsLocked != null && nodeInfo.Entry.IsLocked == true)
                await UnlockAll(nodeId);

            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
            var nextOwner = properties?.GetNestedValueOrDefault(SpisumNames.Properties.NextOwner)?.ToString();
            var nextGroup = properties?.GetNestedValueOrDefault(SpisumNames.Properties.NextGroup)?.ToString();
            var owner = properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString();
            var ownerGroup = properties?.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();
            var fileMark = properties.GetNestedValueOrDefault(SpisumNames.Properties.FileMark)?.ToString();

            var isMemberOfRepository = await IsMemberOfRepository(nextGroup);

            if (isMemberOfRepository && string.IsNullOrWhiteSpace(fileMark))
                throw new BadRequestException("", $"Property {SpisumNames.Properties.FileMark} is mandatory");

            if (string.IsNullOrEmpty(nextGroup)) throw new BadRequestException("CODE");

            var updateBody = new NodeBodyUpdateFixed();
            var locallySet = nodeInfo?.Entry?.Permissions?.LocallySet;
            updateBody.Permissions.LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>();

            updateBody.RemovePermissions(
                nextGroup != ownerGroup ? ownerGroup : null,
                nextOwner != null && nextOwner != AlfrescoNames.Aliases.Group && nextOwner != owner ? owner : null);

            if (ownerGroup == SpisumNames.Groups.MailroomGroup)
            {
                updateBody.RemovePermission(SpisumNames.Groups.MailroomGroup);
                updateBody.AddPermissions(nextGroup);
            }

            if (!isMemberOfRepository && nextOwner == AlfrescoNames.Aliases.Group)
                updateBody.RemovePermission(nextGroup);

            updateBody
                .AddProperty(SpisumNames.Properties.Group, nextGroup)
                .AddProperty(SpisumNames.Properties.NextGroup, string.Empty)
                .AddProperty(SpisumNames.Properties.NextOwner, string.Empty)
                .AddProperty(SpisumNames.Properties.HandoverDate, string.Empty);

            if (isMemberOfRepository)
            {
                updateBody.Permissions = new PermissionsBody();

                var aspects = nodeInfo?.Entry.AspectNames;

                aspects.Remove(AlfrescoNames.Aspects.Ownable);
                updateBody
                    .AddPermission(nextGroup, $"{GroupPermissionTypes.Coordinator}")
                    .AspectNames = aspects;
            }
            else
            {
                updateBody
                    .AddProperty(AlfrescoNames.ContentModel.Owner,
                        nextOwner != null && nextOwner != AlfrescoNames.Aliases.Group ? nextOwner : _identityUser.Id);
            }

            await UpdateFile(nodeId, updateBody, isMemberOfRepository);
            await MoveOwner(nodeId, nextGroup, isMemberOfRepository);

            if (isMemberOfRepository)
            {
                // Create a record in repository
                if (nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.Document)
                    await _repositoryService.CreateDocumentRecord(fileMark, nodeId);
                else if (nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.File)
                    await _repositoryService.CreateFileRecord(fileMark, nodeId);

                // final lock
                await LockAll(nodeId);
            }

            await _transactionHistoryService.LogHandoverAccept(nodeId, isMemberOfRepository);
        }

        public bool AreNodeTypesValid(List<NodeEntry> nodeEntries, string nodeType)
        {
            return nodeEntries.All(x => x?.Entry?.NodeType == nodeType);
        }

        public async Task CancelNode(string nodeId, string reason)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

            if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document &&
                nodeInfo?.Entry?.Path?.Name?.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomUnfinished) == true)
            {
                if (nodeInfo?.Entry?.IsLocked == true)
                    await UnlockAll(nodeId);

                await DeleteFileAndAllAssociatedComponents(nodeId);
                return;
            }

            if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document &&
                nodeInfo?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsForProcessing(_identityUser.RequestGroup), StringComparison.OrdinalIgnoreCase) != true)
                throw new BadRequestException("Document can't be cancelled");

            if (nodeInfo?.Entry?.IsLocked == true)
                await UnlockAll(nodeId);

            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
            var state = properties.GetNestedValueOrDefault(SpisumNames.Properties.State)?.ToString();

            await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.PreviousPath, nodeInfo.Entry.Path.Name)
                .AddProperty(SpisumNames.Properties.CancelDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                .AddProperty(SpisumNames.Properties.State, SpisumNames.Other.Storno)
                .AddProperty(SpisumNames.Properties.PreviousState, state)
                .AddProperty(SpisumNames.Properties.CancelReason, reason)
                .AddProperty(SpisumNames.Properties.PreviousIsLocked, nodeInfo?.Entry?.IsLocked == true ? bool.TrueString : null));

            if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
            {
                var parent = await _alfrescoHttpClient.GetNodeParents(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                if (parent.List.Entries.Count > 0)
                    throw new BadRequestException($"Document id: {nodeId} is in file.");
            }

            await MoveByPath(nodeId, SpisumNames.Paths.EvidenceCancelled(_identityUser.RequestGroup));

            var pid = nodeInfo?.GetPid();

            try
            {
                if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Concept, pid, NodeTypeCodes.Dokument, EventCodes.Storno,
                        string.Format(TransactinoHistoryMessages.ConceptCancel, reason));
                if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, pid, NodeTypeCodes.Dokument, EventCodes.Storno,
                        string.Format(TransactinoHistoryMessages.DocumentCancel, reason));
                if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, pid, NodeTypeCodes.Spis, EventCodes.Storno,
                        string.Format(TransactinoHistoryMessages.FileCancel, reason));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
            await LockAll(nodeId);
        }

        public async Task<NodeEntry> ComponentUpdate(ComponentUpdate body, IImmutableList<Parameter> parameters = null)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(body.ComponentId);
            var alfrescoBody = _mapper.Map<NodeBodyUpdate>(body);
            alfrescoBody.Properties = PropertiesProtector.Filter(nodeInfo.Entry.NodeType, alfrescoBody.Properties?.As<Dictionary<string, object>>());

            return await _alfrescoHttpClient.UpdateNode(body.ComponentId, alfrescoBody, parameters);
        }

        public async Task CreateAssociation(string parentId, string childId, string assocType)
        {
            await CreateAssociation(parentId, new List<string> { childId }, assocType);
        }

        public async Task CreateAssociation(string parentId, List<string> childsId, string assocType)
        {
            await childsId.ForEachAsync(async childId =>
            {
                await _alfrescoHttpClient.CreateNodeSecondaryChildren(parentId, new ChildAssociationBody
                {
                    AssocType = assocType,
                    ChildId = childId
                });
            });
        }

        public async Task<NodeEntry> CreateFolder(string relativePath)
        {
            return await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new NodeBodyCreate
            {
                Name = IdGenerator.GenerateId(),
                NodeType = AlfrescoNames.ContentModel.Folder,
                RelativePath = relativePath
            });
        }

        public async Task<NodeEntry> CreatePermissions(string nodeId, string prefix = null, string owner = null, bool isInheritanceEnabled = false)
        {
            var updateBody = SetPermissions(prefix, owner, isInheritanceEnabled);

            return await _alfrescoHttpClient.UpdateNode(nodeId, updateBody);
        }

        public async Task<NodeEntry> CreatePermissionsWithoutPostfixes(string nodeId, string prefix = null, string owner = null, bool isInheritanceEnabled = false)
        {
            var updateBody = SetPermissionsWithoutPostfixes(prefix, owner, isInheritanceEnabled);

            return await _alfrescoHttpClient.UpdateNode(nodeId, updateBody);
        }

        public async Task CreateSecondaryChildrenAsAdmin(string parentId, ChildAssociationBody body)
        {
            await _adminAlfrescoHttpClient.CreateNodeSecondaryChildren(parentId, body);
        }

        public async Task DeclineOwner(string nodeId, bool cancelAction)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties},{AlfrescoNames.Includes.Permissions},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

            if (nodeInfo?.Entry?.IsLocked != null && nodeInfo.Entry.IsLocked == true)
                await UnlockAll(nodeId);

            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
            var owner = properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString();
            var nextOwner = properties?.GetNestedValueOrDefault(SpisumNames.Properties.NextOwner)?.ToString();
            var nextGroup = properties?.GetNestedValueOrDefault(SpisumNames.Properties.NextGroup)?.ToString();
            var ownerGroup = properties?.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

            if (string.IsNullOrEmpty(nextGroup)) throw new BadRequestException("CODE");

            NodeBodyUpdateFixed updateBody = new NodeBodyUpdateFixed();
            var locallySet = nodeInfo?.Entry?.Permissions?.LocallySet;
            updateBody.Permissions.LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>();

            updateBody.RemovePermissions(
                nextGroup != ownerGroup ? nextGroup : null,
                nextOwner != null && nextOwner != AlfrescoNames.Aliases.Group && nextOwner != owner ? nextOwner : null);

            if (nextOwner == AlfrescoNames.Aliases.Group)
                updateBody.RemovePermission(nextGroup);

            updateBody
                .AddProperty(SpisumNames.Properties.NextOwnerDecline, true)
                .AddProperty(SpisumNames.Properties.NextGroup, string.Empty)
                .AddProperty(SpisumNames.Properties.NextOwner, string.Empty)
                .AddProperty(SpisumNames.Properties.HandoverDate, string.Empty);

            await MoveOwner(nodeId, ownerGroup);

            await UpdateNodeAsAdmin(nodeId, updateBody);

            if (!cancelAction) // Decline owner endpoints
                await _transactionHistoryService.LogHandoverDecline(nodeId, nodeInfo);
            else // Cancel action
                await _transactionHistoryService.LogHandoverCancel(nodeId, nodeInfo);
        }

        public async Task DeleteNodeAsAdmin(string nodeId)
        {
            await _adminAlfrescoHttpClient.DeleteNode(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Permanent, true, ParameterType.QueryString)));
        }

        public async Task DeleteNodePermanent(string nodeId, bool permanent = true)
        {
            await _alfrescoHttpClient.DeleteNode(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Permanent, permanent, ParameterType.QueryString)));
        }

        public async Task DeleteSecondaryChildrenAsAdmin(string parentId, string childrenId, IImmutableList<Parameter> parameters = null)
        {
            await _adminAlfrescoHttpClient.DeleteSecondaryChildren(parentId, childrenId, parameters);
        }

        public async Task<FileContentResult> Download(List<string> nodesId)
        {
            // Download single file
            if (nodesId.Count == 1)
                try
                {
                    var fileContent = await _alfrescoHttpClient.NodeContent(nodesId.FirstOrDefault());
                    string fileName = fileContent.FileName;

                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodesId.FirstOrDefault());
                    var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
                    fileName = properties.GetNestedValueOrDefault(SpisumNames.Properties.FileName)?.ToString() ?? fileContent.FileName;

                    return GetFileContentResult(fileContent, fileName);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            // Download multiple files in zip file
            using (var outStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
                {
                    List<DownloadFile> files = new List<DownloadFile>();

                    foreach (var id in nodesId)
                    {
                        var file = new DownloadFile
                        {
                            File = await _alfrescoHttpClient.NodeContent(id)
                        };

                        var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(id);

                        var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
                        var origFileName = properties.GetNestedValueOrDefault(SpisumNames.Properties.FileName)?.ToString();

                        file.OrigFileName = origFileName;
                        files.Add(file);
                    }

                    // If there are duplicate names - rename
                    var result =
                        files
                            .Select(f => new
                            {
                                f.File,
                                Name = Path.GetFileNameWithoutExtension(f.OrigFileName),
                                Ext = Path.GetExtension(f.OrigFileName)
                            })
                            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(g => g.Select((x, i) => new DownloadFile
                            {
                                File = new FormDataParam(
                               x.File.File,
                               $"{(i > 0 ? x.Name + " (" + i + ")" : x.Name)}{x.Ext}",
                               null,
                               x.File.ContentType),
                                OrigFileName = x.Name
                            }))
                            .SelectMany(x => x);

                    foreach (var file in result)
                    {
                        var fileInArchive = archive.CreateEntry(file.File.FileName, CompressionLevel.Optimal);
                        using (var entryStream = fileInArchive.Open())
                        using (var fileToCompressStream = new MemoryStream(file.File.File))
                        {
                            fileToCompressStream.CopyTo(entryStream);
                        }
                    }

                }

                return new FileContentResult(outStream.ToArray(), "application/zip")
                {
                    FileDownloadName = $"{DateTime.Now.ToString("dd-MM-yyyy-hh-mm-ss")}.zip"
                };
            }

        }

        public async Task<List<string>> FoundNode(string[] nodeIds, string nodeType)
        {
            var errorIds = new List<string>();

            foreach (var nodeId in nodeIds)
                try
                {
                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

                    if (nodeInfo?.Entry?.NodeType != nodeType ||
                        nodeInfo?.Entry?.Path?.Name?.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceLostDestroyed(_identityUser.RequestGroup)) != true)
                        throw new Exception();

                    if (nodeInfo.Entry.IsLocked == true)
                        await UnlockAll(nodeId);

                    var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
                    var previousState = properties.GetNestedValueOrDefault(SpisumNames.Properties.PreviousState)?.ToString();
                    var previousPath = properties.GetNestedValueOrDefault(SpisumNames.Properties.PreviousPath)?.ToString();
                    var previousIsLocked = properties.GetNestedValueOrDefault(SpisumNames.Properties.PreviousIsLocked)?.ToString();

                    var node = await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                        .AddProperty(SpisumNames.Properties.State, previousState)
                        .AddProperty(SpisumNames.Properties.LostDate, null)
                        .AddProperty(SpisumNames.Properties.LostReason, null)
                        .AddProperty(SpisumNames.Properties.PreviousState, null)
                        .AddProperty(SpisumNames.Properties.PreviousPath, null)
                        .AddProperty(SpisumNames.Properties.PreviousIsLocked, null));

                    await MoveByPath(nodeId, previousPath);

                    if (nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.File)
                    {
                        var childrenIds = (await GetSecondaryChildren(nodeId, SpisumNames.Associations.Documents)).Select(x => x.Entry.Id).ToList();
                        await childrenIds.ForEachAsync(async document => await MoveByPath(document, previousPath));
                    }

                    if (previousIsLocked == bool.TrueString)
                        await LockAll(nodeId);

                    try
                    {
                        var documentPid = node?.GetPid();

                        if (node?.Entry.NodeType == SpisumNames.NodeTypes.Document)
                            await _auditLogService.Record(nodeId, node?.Entry.NodeType, documentPid, NodeTypeCodes.Dokument, EventCodes.Zruseni, TransactinoHistoryMessages.ConceptRecover);
                        if (node?.Entry.NodeType == SpisumNames.NodeTypes.File)
                            await _auditLogService.Record(nodeId, node?.Entry.NodeType, documentPid, NodeTypeCodes.Spis, EventCodes.Zruseni, TransactinoHistoryMessages.FileRecover);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    errorIds.Add(nodeId);
                }

            return errorIds;
        }

        public async Task<NodeEntry> GenerateSsid(string nodeId, GenerateSsid ssidSettings)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);
            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();

            if (int.TryParse(properties.GetNestedValueOrDefault(SpisumNames.Properties.SsidNumber)?.ToString(), out int ssidNumber))
            {
                string ssid = ssidSettings.Pattern
                    .Replace("{shortcut}", ssidSettings.Shortcut)
                    .Replace("{ssid_number}", ssidNumber.ToString($"D{ssidSettings.SsidNumberPlaces}"))
                    .Replace("{year}", DateTime.Today.Year.ToString());

                return await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate { Properties = new Dictionary<string, object> { { SpisumNames.Properties.Ssid, ssid } } });
            }

            return null;
        }

        public async Task<List<NodeChildAssociationEntry>> GetChildren(string nodeId, bool includeProperties = false, bool includePath = false)
        {
            ImmutableList<Parameter> parameters = ImmutableList<Parameter>.Empty;

            if (includeProperties)
                parameters = ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString));

            if (includePath)
                parameters = ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString));

            if (includePath && includeProperties)
                parameters = ImmutableList<Parameter>.Empty.AddRange(new List<Parameter>
                {
                    new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString),
                    new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)
                });

            var children = await _alfrescoHttpClient.GetNodeSecondaryChildren(nodeId, parameters);

            return children?.List?.Entries?.ToList();
        }

        public async Task<NodeBodyUpdate> GetNodeBodyUpdateWithPermissions(string nodeId)
        {
            var documentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{ AlfrescoNames.Includes.Permissions }", ParameterType.QueryString)));

            var locallySet = documentInfo?.Entry?.Permissions?.LocallySet;

            return new NodeBodyUpdateFixed
            {
                Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
            };
        }

        public async Task<NodeBodyUpdate> GetNodeBodyUpdateWithPermissionsAsAdmin(string nodeId)
        {
            var documentInfo = await _adminAlfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{ AlfrescoNames.Includes.Permissions }", ParameterType.QueryString)));

            var locallySet = documentInfo?.Entry?.Permissions?.LocallySet;

            return new NodeBodyUpdateFixed
            {
                Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
            };
        }

        public async Task<List<NodeEntry>> GetNodesInfo(List<string> nodesId)
        {
            List<NodeEntry> nodeEntries = new List<NodeEntry>();

            await nodesId.ForEachAsync(async componentId =>
            {
                nodeEntries.Add(await _alfrescoHttpClient.GetNodeInfo(componentId));
            });

            return nodeEntries;
        }

        public async Task<List<NodeAssociationPaging>> GetParents(List<string> nodeIds)
        {
            List<NodeAssociationPaging> parents = new List<NodeAssociationPaging>();

            await nodeIds.ForEachAsync(async x =>
            {
                parents.Add(await _alfrescoHttpClient.GetNodeParents(x));
            });

            return parents;
        }

        public async Task<List<NodeAssociationEntry>> GetParentsByAssociation(string nodeId, List<string> associations)
        {
            var result = await _alfrescoHttpClient.GetNodeParents(nodeId);

            return result?.List?.Entries?.Where(x => associations?.Contains(x?.Entry?.Association?.AssocType) ?? false).ToList();
        }

        public async Task<List<NodeChildAssociationEntry>> GetSecondaryChildren(string nodeId, string association, bool includePath = false, bool includeProperties = false)
        {
            return await GetSecondaryChildren(nodeId, new List<string> { association }, includePath, includeProperties);
        }

        public async Task<List<NodeChildAssociationEntry>> GetSecondaryChildren(string nodeId, List<string> associations, bool includePath = false, bool includeProperties = false)
        {
            List<NodeChildAssociationEntry> result = new List<NodeChildAssociationEntry>();
            ImmutableList<Parameter> parameters = ImmutableList<Parameter>.Empty;

            foreach (string association in associations)
            {
                parameters = ImmutableList<Parameter>.Empty
                   .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{association}')", ParameterType.QueryString));

                if (includeProperties)
                    parameters = ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{association}')", ParameterType.QueryString));

                if (includePath)
                    parameters = ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{association}')", ParameterType.QueryString));

                if (includePath && includeProperties)
                    parameters = ImmutableList<Parameter>.Empty.AddRange(new List<Parameter>
                    {
                        new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString),
                        new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString),
                        new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{association}')", ParameterType.QueryString)
                    });

                var secondaryChildrens = await _alfrescoHttpClient.GetNodeSecondaryChildren(nodeId, parameters);

                result.AddRange(secondaryChildrens?.List?.Entries?.ToList());
            }

            return result;
        }

        public List<ShreddingPlanModel> GetShreddingPlans()
        {
            return _alfrescoConfiguration?.ShreddingPlan != null
                ? JsonConvert.DeserializeObject<List<ShreddingPlanModel>>(File.ReadAllText(_alfrescoConfiguration.ShreddingPlan))
                : new List<ShreddingPlanModel>();
        }

        public async Task<bool> IsMemberOfRepository(string requestGroup)
        {
            var groups = await _alfrescoHttpClient.GetGroupMembers(SpisumNames.Groups.RepositoryGroup,
                ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Where, AlfrescoNames.MemberType.Group, ParameterType.QueryString)));

            return groups?.List?.Entries?.Any(x => x.Entry.Id == requestGroup) ?? false;
        }

        public async Task<bool> IsNodeLocked(string nodeId)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

            return nodeInfo?.Entry?.IsLocked != null && nodeInfo.Entry.IsLocked == true;
        }

        public async Task LockAll(string nodeId)
        {
            await TraverseAllChildren(nodeId, async locNodeId => await _alfrescoHttpClient.NodeLock(locNodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL)));
        }

        public async Task LostDestroyedNode(string nodeId, string reason)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

            if (nodeInfo?.Entry?.IsLocked == true)
                await UnlockAll(nodeId);

            var properties = nodeInfo?.Entry?.Properties.As<JObject>().ToDictionary();
            var state = properties.GetNestedValueOrDefault(SpisumNames.Properties.State)?.ToString();

            var node = await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.LostDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                .AddProperty(SpisumNames.Properties.LostReason, reason)
                .AddProperty(SpisumNames.Properties.PreviousState, state)
                .AddProperty(SpisumNames.Properties.State, SpisumNames.Other.Storno)
                .AddProperty(SpisumNames.Properties.PreviousPath,
                    nodeInfo?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path) == true ?
                        nodeInfo?.Entry?.Path?.Name.Substring(AlfrescoNames.Prefixes.Path.Length, nodeInfo.Entry.Path.Name.Length - AlfrescoNames.Prefixes.Path.Length) :
                        nodeInfo?.Entry?.Path?.Name)
                .AddProperty(SpisumNames.Properties.PreviousIsLocked, nodeInfo?.Entry?.IsLocked == true ? bool.TrueString : null));

            if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
            {
                var parent = await _alfrescoHttpClient.GetNodeParents(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                    .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                if (parent.List.Entries.Count > 0)
                    throw new BadRequestException($"Document id: {nodeId} is in file.");

                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceLostDestroyed(_identityUser.RequestGroup));

                try
                {
                    var documentPid = node?.GetPid();

                    // Audit log for a document
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.Zniceni,
                        TransactinoHistoryMessages.DocumentLostDestroy);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }
            }

            if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.File)
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceLostDestroyed(_identityUser.RequestGroup));

                var childrenIds = (await GetSecondaryChildren(nodeId, SpisumNames.Associations.Documents)).Select(x => x.Entry.Id).ToList();
                await childrenIds.ForEachAsync(async document => await MoveByPath(document, SpisumNames.Paths.EvidenceFilesLostDestroyed(_identityUser.RequestGroup)));

                try
                {
                    var documentPid = node?.GetPid();

                    // Audit log for a document
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, documentPid, NodeTypeCodes.Dokument, EventCodes.Zniceni,
                        TransactinoHistoryMessages.DocumentLostDestroy);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }
            }

            await LockAll(nodeId);
        }

        public async Task<List<NodeEntry>> MoveAllComponets(string nodeId)
        {
            List<NodeEntry> result = new List<NodeEntry>();

            var entries = (await GetSecondaryChildren(nodeId, SpisumNames.Associations.Components, true)).Select(x => x.Entry).ToList();

            foreach (var entry in entries)
                if ((entry.NodeType == SpisumNames.NodeTypes.Component
                     || entry.NodeType == SpisumNames.NodeTypes.EmailComponent
                     || entry.NodeType == SpisumNames.NodeTypes.DataBoxComponent)
                    && entry.Path.Name != SpisumNames.Paths.Components
                )
                {
                    var resultNode = await MoveByPath(entry.Id, SpisumNames.Paths.Components);
                    result.Add(resultNode);
                }

            return result;
        }

        public async Task<List<NodeEntry>> MoveByPath(List<string> nodesId, string targetPath)
        {
            List<NodeEntry> entries = new List<NodeEntry>();

            await nodesId.ForEachAsync(async x =>
            {
                entries.Add(await MoveByPath(x, targetPath));
            });

            return entries;
        }

        public async Task<NodeEntry> MoveByPath(string nodeId, string targetPath)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.RelativePath, targetPath, ParameterType.QueryString)));

            return await _alfrescoHttpClient.NodeMove(nodeId, new NodeBodyMove(nodeInfo.Entry.Id));
        }

        public async Task<NodeEntry> MoveByPathAsAdmin(string nodeId, string targetPath)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.RelativePath, targetPath, ParameterType.QueryString)));

            return await _adminAlfrescoHttpClient.NodeMove(nodeId, new NodeBodyMove(nodeInfo.Entry.Id));
        }

        public async Task<NodeEntry> MoveByPathCached(string nodeId, string destinationPath)
        {
            if (string.IsNullOrEmpty(destinationPath))
                throw new ArgumentException();

            return await _alfrescoHttpClient.NodeMove(nodeId, new NodeBodyMove(await GetOrCreateCachedNode(destinationPath)));

            async Task<string> GetOrCreateCachedNode(string path)
            {
                var localNodeId = _simpleMemoryCache.Get<string>(path);
                if (localNodeId != null)
                    return localNodeId;

                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.RelativePath, path, ParameterType.QueryString)));

                _simpleMemoryCache.Create(path, localNodeId = nodeInfo.Entry.Id);

                return localNodeId;
            }
        }

        public async Task MoveForSignature(string nodeId, string group)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            string path = nodeInfo.Entry.Path.Name;

            if (path.EndsWith(SpisumNames.Paths.DocumentsForProcessing, StringComparison.OrdinalIgnoreCase))
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceDocumentsForProcessingForSignature(group));

            if (path.EndsWith(SpisumNames.Paths.FilesDocumentsForProcessing, StringComparison.OrdinalIgnoreCase))
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceFilesDocumentsForProcessingForSignature(group));
        }

        public async Task<NodeEntry> MoveFromSignature(string nodeId)
        {
            await UnlockAll(nodeId);

            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
            var forSignatureUser = properties.GetNestedValueOrDefault(SpisumNames.Properties.ForSignatureUser)?.ToString();
            var forSignatureGroup = properties.GetNestedValueOrDefault(SpisumNames.Properties.ForSignatureGroup)?.ToString();
            var owner = properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString();
            var group = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

            await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.ForSignatureUser, null)
                .AddProperty(SpisumNames.Properties.ForSignatureGroup, null)
                .AddProperty(SpisumNames.Properties.ForSignatureDate, null)
            );

            // Move
            var path = nodeInfo.Entry.Path.Name;

            if (path.EndsWith(SpisumNames.Paths.DocumentsForSignature, StringComparison.OrdinalIgnoreCase))
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceDocumentsForProcessing(group));

            if (path.EndsWith(SpisumNames.Paths.FilesDocumentsForSignature, StringComparison.OrdinalIgnoreCase))
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(group));

            // Remove permission as admin
            var updateBody = await GetNodeBodyUpdateWithPermissions(nodeId);
            var node = await UpdateNodeAsAdmin(nodeId, updateBody
                .RemovePermission($"{forSignatureGroup}_Sign")
                .RemovePermission($"{SpisumNames.Prefixes.UserGroup}{forSignatureUser}")
            );

            try
            {
                var personEntry = await _alfrescoHttpClient.GetPerson(owner);
                var groupEntry = await _alfrescoHttpClient.GetGroup(group);

                var documentPid = node?.GetPid();

                // Audit log for a document
                await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.VraceniZAgendy,
                    string.Format(TransactinoHistoryMessages.DocumentFromSignature, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));

                // Audit log for a file
                var fileId = (await _alfrescoHttpClient.GetNodeParents(nodeId, ImmutableList<Parameter>.Empty
                                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                                .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)))
                    )?.List?.Entries?.FirstOrDefault()?.Entry?.Id;

                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Document, documentPid, NodeTypeCodes.Dokument, EventCodes.VraceniZAgendy,
                        string.Format(TransactinoHistoryMessages.DocumentFromSignature, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return node;
        }

        public async Task MoveHandOverPath(string nodeId, string group, string nextGroup, string nextOwner)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            string path = nodeInfo.Entry.Path.Name;
            string nodeType = null;

            if (path.EndsWith(SpisumNames.Paths.Documents, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceDocumentsForProcessingWaitingForTakeOver(group));
                nodeType = SpisumNames.NodeTypes.TakeDocumentForProcessing;
            }

            else if (path.EndsWith(SpisumNames.Paths.DocumentsForProcessing, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceDocumentsForProcessingWaitingForTakeOver(group));
                nodeType = SpisumNames.NodeTypes.TakeDocumentForProcessing;
            }

            else if (path.EndsWith(SpisumNames.Paths.DocumentsProcessed, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceDocumentsProcessedWaitingForTakeOver(group));
                nodeType = SpisumNames.NodeTypes.TakeDocumentProcessed;
            }

            else if (path.EndsWith(SpisumNames.Paths.FilesOpen, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceFilesOpenWaitingForTakeOver(group));
                nodeType = SpisumNames.NodeTypes.TakeFileOpen;
            }

            else if (path.EndsWith(SpisumNames.Paths.FilesClosed, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceFilesClosedWaitingForTakeOver(group));
                nodeType = SpisumNames.NodeTypes.TakeFileClosed;
            }

            else if (path.EndsWith(SpisumNames.Paths.Concepts, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceConceptsWaitingForTakeOver(group));
                nodeType = SpisumNames.NodeTypes.TakeConcept;
            }

            if (nodeType != null)
            {
                var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
                var owner = properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString();

                var parameters = ImmutableList<Parameter>.Empty
                  .Add(new Parameter(AlfrescoNames.Headers.NodeType, nodeType, ParameterType.GetOrPost))
                  .Add(new Parameter(AlfrescoNames.Headers.RelativePath, SpisumNames.Paths.EvidenceToTakeOver(nextGroup), ParameterType.GetOrPost))
                  .Add(new Parameter(SpisumNames.Properties.TakeRef, nodeId, ParameterType.GetOrPost))
                  .Add(new Parameter(SpisumNames.Properties.CurrentOwner, owner, ParameterType.GetOrPost));

                foreach (var item in properties)
                    if (item.Key.StartsWith("ssl:"))
                    {
                        if (DateTime.TryParse(item.Value?.ToString(), out DateTime datetime))
                            parameters = parameters.Add(new Parameter(item.Key, datetime.ToAlfrescoDateTimeString(), ParameterType.GetOrPost));
                        else
                            parameters = parameters.Add(new Parameter(item.Key == "ssl:pid" ? "ssl:pidRef" : item.Key, item.Value, ParameterType.GetOrPost));
                    }

                var toTakeOverInfo = await CreateFileLink(nodeId, nodeType, SpisumNames.Paths.EvidenceToTakeOver(nextGroup), properties, SpisumNames.Properties.TakeRef);
                var waitingForTakeOverInfo = await CreateFileLink(nodeId, nodeType, SpisumNames.Paths.EvidenceWaitingForTakeOver(group), properties, SpisumNames.Properties.WaitingRef);

                await UpdateHandOverPermissions(toTakeOverInfo.Entry.Id, nextGroup, nextOwner);
                await UpdateHandOverPermissions(waitingForTakeOverInfo.Entry.Id, group, owner);

                await _adminAlfrescoHttpClient.UpdateNode(toTakeOverInfo.Entry.Id, new NodeBodyUpdate().AddProperty(AlfrescoNames.ContentModel.Owner, "admin"));
                await _adminAlfrescoHttpClient.UpdateNode(waitingForTakeOverInfo.Entry.Id, new NodeBodyUpdate()
                    .AddProperty(AlfrescoNames.ContentModel.Owner, "admin")
                    .AddProperty(SpisumNames.Properties.NextGroup, nextGroup ?? string.Empty)
                    .AddProperty(SpisumNames.Properties.NextOwner, nextOwner ?? (nextGroup != null ? AlfrescoNames.Aliases.Group : string.Empty)));
                await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.TakeRef, toTakeOverInfo.Entry.Id)
                    .AddProperty(SpisumNames.Properties.WaitingRef, waitingForTakeOverInfo.Entry.Id));
            }
        }

        private async Task<NodeEntry> CreateFileLink(string nodeId, string nodeType, string path, Dictionary<string, object> properties, string reference)
        {
            var parameters = ImmutableList<Parameter>.Empty
                 .Add(new Parameter(AlfrescoNames.Headers.NodeType, nodeType, ParameterType.GetOrPost))
                 .Add(new Parameter(AlfrescoNames.Headers.RelativePath, path, ParameterType.GetOrPost))
                 .Add(new Parameter(reference, nodeId, ParameterType.GetOrPost))
                 .Add(new Parameter(SpisumNames.Properties.CurrentOwner, properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString(), ParameterType.GetOrPost));

            foreach (var item in properties)
                if (item.Key.StartsWith("ssl:"))
                {
                    if (DateTime.TryParse(item.Value?.ToString(), out DateTime datetime))
                        parameters = parameters.Add(new Parameter(item.Key, datetime.ToAlfrescoDateTimeString(), ParameterType.GetOrPost));
                    else
                        parameters = parameters.Add(new Parameter(item.Key == "ssl:pid" ? "ssl:pidRef" : item.Key, item.Value, ParameterType.GetOrPost));
                }

            return await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new FormDataParam(new byte[] { 01 }), parameters);
        }

        public async Task<List<NodeEntry>> MoveChildren(string nodeId, string targetNodeId)
        {
            var result = new List<NodeEntry>();

            result.Add(await MoveByNode(nodeId, targetNodeId));

            var childrenIds = (await GetChildren(nodeId)).Select(x => x.Entry.Id).ToList();

            foreach (var id in childrenIds)
            {
                var resultNode = await MoveByNode(id, targetNodeId);
                result.Add(resultNode);
            }

            return result;
        }

        public async Task<List<NodeEntry>> MoveChildrenByPath(string nodeId, string targetPath)
        {
            var targetPathInfo = await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.RelativePath, targetPath, ParameterType.QueryString)));

            return await MoveChildren(nodeId, targetPathInfo?.Entry?.Id);
        }

        public async Task MoveOwner(string nodeId, string group, bool isMemberOfDispatch = false)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            string path = nodeInfo.Entry.Path.Name;

            if (isMemberOfDispatch)
            {
                if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                {
                    var childrenIds = (await GetSecondaryChildren(nodeId, SpisumNames.Associations.Documents)).Select(x => x.Entry.Id).ToList();

                    await childrenIds?.ForEachAsync(async x =>
                    {
                        await MoveByPath(x, SpisumNames.Paths.RepositoryFilesDocuments);
                    });

                    await MoveByPath(nodeId, SpisumNames.Paths.RepositoryFilesStored);
                } 
                else if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                {
                    await MoveByPath(nodeId, SpisumNames.Paths.RepositoryDocumentsStored);
                }
            }

            else if (path.EndsWith(SpisumNames.Paths.DocumentsForProcessingWaitingForTakeOver, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceDocumentsForProcessing(group));
            }

            else if (path.EndsWith(SpisumNames.Paths.DocumentsProcessedWaitingForTakeOver, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceDocumentsProcessed(group));
            }

            else if (path.EndsWith(SpisumNames.Paths.FilesOpenWaitingForTakeOver, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceFilesOpen(group));
            }

            else if (path.EndsWith(SpisumNames.Paths.FilesClosedWaitingForTakeOver, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceFilesClosed(group));
            }

            else if (path.EndsWith(SpisumNames.Paths.ConceptsWaitingForTakeOver, StringComparison.OrdinalIgnoreCase))
            {
                await MoveByPath(nodeId, SpisumNames.Paths.EvidenceConcepts(group));
            }

            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
            var takeRef = properties.GetNestedValueOrDefault(SpisumNames.Properties.TakeRef)?.ToString();
            var waitingRef = properties.GetNestedValueOrDefault(SpisumNames.Properties.WaitingRef)?.ToString();

            if (takeRef != null)
                await DeleteNodeAsAdmin(takeRef);
            if (waitingRef != null)
                await DeleteNodeAsAdmin(waitingRef);
        }

        public async Task<NodeEntry> NodeLockAsAdmin(string nodeId)
        {
            return await _adminAlfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
        }

        public async Task<NodeEntry> OpenFile(string nodeId, string reason)
        {
            var lockedComponents = new List<string>();
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

                var fileInfo = await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.ClosureDate, null)
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
                    .AddProperty(SpisumNames.Properties.ProcessorJob, null));

                await TraverseAllChildren(nodeId, async locNodeId =>
                {
                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(locNodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                    if (nodeInfo.Entry.IsLocked == true)
                    {
                        await _alfrescoHttpClient.NodeUnlock(locNodeId);
                        lockedComponents.Add(nodeInfo.Entry.Id);
                    }

                    return nodeInfo;
                });

                var node = await MoveByPath(nodeId, SpisumNames.Paths.EvidenceFilesOpen(_identityUser.RequestGroup));

                try
                {
                    // Audit log for a file
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, fileInfo?.GetPid(), NodeTypeCodes.Spis, EventCodes.Otevreni,
                        string.Format(TransactinoHistoryMessages.FileOpen, reason));
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return node;
            }
            catch (Exception ex)
            {
                if (lockedComponents.Count > 0)
                    await lockedComponents.ForEachAsync(async locNodeId => await _alfrescoHttpClient.NodeLock(locNodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL)));
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
                throw ex;
            }
        }

        public async Task<List<string>> Recover(List<string> nodeIds, string reason, string nodeType, string allowedPath)
        {
            List<string> unprocessedNodeIds = new List<string>();

            foreach (var nodeId in nodeIds)
                try
                {
                    // Check if correct nodeType and allowed path
                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.Properties},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

                    if (nodeInfo?.Entry?.NodeType != nodeType && nodeInfo?.Entry?.Path?.Name != allowedPath)
                        throw new Exception();

                    if (nodeInfo?.Entry?.IsLocked == true)
                        await UnlockAll(nodeId);

                    // Update node
                    var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
                    var previousPath = properties.GetNestedValueOrDefault(SpisumNames.Properties.PreviousPath)?.ToString();
                    var previousIsLocked = properties.GetNestedValueOrDefault(SpisumNames.Properties.PreviousIsLocked)?.ToString();

                    // Move main file to origin
                    await MoveByPath(nodeInfo.Entry.Id, previousPath.Replace(AlfrescoNames.Prefixes.Path, string.Empty));

                    // Move node first, to prevent loosing data
                    var node = await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                        .AddProperty(SpisumNames.Properties.State, properties.GetNestedValueOrDefault(SpisumNames.Properties.PreviousState)?.ToString())
                        .AddProperty(SpisumNames.Properties.CancelDate, null)
                        .AddProperty(SpisumNames.Properties.PreviousPath, null)
                        .AddProperty(SpisumNames.Properties.CancelReason, null)
                        .AddProperty(SpisumNames.Properties.PreviousState, null)
                        .AddProperty(SpisumNames.Properties.PreviousIsLocked, null));

                    if (previousIsLocked == bool.TrueString)
                        await LockAll(nodeId);

                    try
                    {
                        var documentPid = node?.GetPid();

                        if (node?.Entry.NodeType == SpisumNames.NodeTypes.Concept)
                            await _auditLogService.Record(nodeId, node?.Entry.NodeType, documentPid, NodeTypeCodes.Dokument, EventCodes.Zruseni, 
                                string.Format(TransactinoHistoryMessages.ConceptRecover, reason));
                        if (node?.Entry.NodeType == SpisumNames.NodeTypes.Document)
                            await _auditLogService.Record(nodeId, node?.Entry.NodeType, documentPid, NodeTypeCodes.Dokument, EventCodes.Zruseni,
                                string.Format(TransactinoHistoryMessages.ConceptRecover, reason));
                        if (node?.Entry.NodeType == SpisumNames.NodeTypes.File)
                            await _auditLogService.Record(nodeId, node?.Entry.NodeType, documentPid, NodeTypeCodes.Spis, EventCodes.Zruseni,
                                string.Format(TransactinoHistoryMessages.ConceptRecover, reason));
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    unprocessedNodeIds.Add(nodeId);
                }

            return unprocessedNodeIds;
        }

        public async Task<NodeEntry> RemoveAllVersions(string nodeId)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            var aspects = nodeInfo?.Entry.AspectNames;
            aspects.Remove(AlfrescoNames.Aspects.Versionable);

            await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate
            {
                AspectNames = aspects
            });

            aspects.Add(AlfrescoNames.Aspects.Versionable);

            var updatedNode = await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate
            {
                AspectNames = aspects
            });

            return updatedNode;
        }

        public NodeBodyUpdate SetPermissions(string prefix = null, string owner = null, bool isInheritanceEnabled = false)
        {
            var bodyUpdate = new NodeBodyUpdate();
            bodyUpdate.SetPermissionInheritance(isInheritanceEnabled);

            return bodyUpdate.AddPermissions(prefix, owner);
        }

        public NodeBodyUpdate SetPermissionsWithoutPostfixes(string prefix = null, string owner = null, bool isInheritanceEnabled = false)
        {
            var bodyUpdate = new NodeBodyUpdate();
            bodyUpdate.SetPermissionInheritance(isInheritanceEnabled);

            return bodyUpdate.AddPermissionsWithoutPostfixes(prefix, owner);
        }

        public async Task TraverseAllChildren<T>(string nodeId, Func<string, Task<T>> funcAction)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            if (nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.File)
                await TraverseChildren(nodeId, true);

            else if (nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.Document ||
                nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.Concept ||
                nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.ShipmentDatabox ||
                nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.ShipmentEmail ||
                nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.ShipmentPersonally ||
                nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.ShipmentPost ||
                nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.ShipmentPublish)
                await TraverseChildren(nodeId);

            else if (nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.Component)
                await funcAction(nodeId);

            async Task TraverseChildren(string localNodeId, bool isFile = false)
            {
                await funcAction(localNodeId);

                var childrenIds = (await GetChildren(localNodeId)).Select(x => x.Entry.Id).ToList();
                if (isFile)
                    await childrenIds.ForEachAsync(async childrenNodeId => await TraverseChildren(childrenNodeId));
                else
                    await childrenIds.ForEachAsync(async childrenNodeId => await funcAction(childrenNodeId));
            }
        }

        public async Task<NodeEntry> TryUnlockNode(string nodeId)
        {
            try
            {
                return await _alfrescoHttpClient.NodeUnlock(nodeId);
            }
            catch
            {
                // It wasnt loocked
                return null;
            }
        }

        public async Task UnlockAll(string nodeId)
        {
            await TraverseAllChildren(nodeId, async locNodeId => await _alfrescoHttpClient.NodeUnlock(locNodeId));
        }

        public async Task<NodeEntry> UnlockNodeAsAdmin(string nodeId)
        {
            return await _adminAlfrescoHttpClient.NodeUnlock(nodeId);
        }

        public async Task<NodeEntry> Update(NodeUpdate body, IImmutableList<Parameter> parameters = null)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(body.NodeId);
            var alfrescoBody = _mapper.Map<NodeBodyUpdate>(body);

            alfrescoBody.Properties = PropertiesProtector.Filter(nodeInfo.Entry.NodeType, alfrescoBody.Properties?.As<Dictionary<string, object>>());

            return await _alfrescoHttpClient.UpdateNode(body.NodeId, alfrescoBody, parameters);
        }

        public async Task UpdateForSignaturePermisionsAll(string nodeId, string user, string group)
        {
            await TraverseAllChildren(nodeId, async locNodeId => await UpdateForSignaturePermisions(locNodeId, user, group));
        }

        public async Task<NodeEntry> UpdateHandOverPermissions(string nodeId, string nextGroup = null, string nextOwner = null)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties},{AlfrescoNames.Includes.Permissions}", ParameterType.QueryString)));

            var updateBody = new NodeBodyUpdateFixed();
            var locallySet = nodeInfo?.Entry?.Permissions?.LocallySet;
            updateBody.Permissions.LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>();

            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
            var owner = properties?.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString();
            var ownerGroup = properties?.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

            updateBody.AddPermissions(
                ownerGroup != nextGroup ? nextGroup : null,
                nextOwner != owner ? nextOwner : null);
            updateBody.SetPermissionInheritance();

            if (nextOwner == null)
                updateBody.AddPermission(nextGroup, $"{GroupPermissionTypes.Coordinator}");

            if (nodeInfo.Entry.NodeType != SpisumNames.NodeTypes.Component)
                updateBody
                    .AddProperty(SpisumNames.Properties.NextGroup, nextGroup ?? string.Empty)
                    .AddProperty(SpisumNames.Properties.NextOwner, nextOwner ?? (nextGroup != null ? AlfrescoNames.Aliases.Group : string.Empty))
                    .AddProperty(SpisumNames.Properties.NextOwnerDecline, null)
                    .AddProperty(SpisumNames.Properties.HandoverDate, DateTime.UtcNow.ToAlfrescoDateTimeString());

            return await _alfrescoHttpClient.UpdateNode(nodeId, updateBody);
        }

        public async Task UpdateHandOverPermissionsAll(string nodeId, string nextGroup = null, string nextOwner = null)
        {
            await TraverseAllChildren(nodeId, async locNodeId => await UpdateHandOverPermissions(locNodeId, nextGroup, nextOwner));
        }

        public async Task<NodeEntry> UpdateHandOverRepositoryPermissions(string nodeId, string nextGroup)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties},{AlfrescoNames.Includes.Permissions}", ParameterType.QueryString)));

            var updateBody = new NodeBodyUpdateFixed();
            var locallySet = nodeInfo?.Entry?.Permissions?.LocallySet;
            updateBody.Permissions.LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>();

            updateBody.AddPermission(nextGroup, $"{GroupPermissionTypes.Coordinator}");

            if (nodeInfo?.Entry?.NodeType != SpisumNames.NodeTypes.Component)
                updateBody
                    .AddProperty(SpisumNames.Properties.NextGroup, nextGroup ?? string.Empty)
                    .AddProperty(SpisumNames.Properties.NextOwner, AlfrescoNames.Aliases.Group)
                    .AddProperty(SpisumNames.Properties.NextOwnerDecline, null)
                    .AddProperty(SpisumNames.Properties.HandoverDate, DateTime.UtcNow.ToAlfrescoDateTimeString());

            return await _alfrescoHttpClient.UpdateNode(nodeId, updateBody);
        }

        public async Task UpdateHandOverRepositoryPermissionsAll(string nodeId, string group)
        {
            await TraverseAllChildren(nodeId, async locNodeId => await UpdateHandOverRepositoryPermissions(locNodeId, group));
        }

        public async Task<NodeEntry> UpdateNodeAsAdmin(string nodeId, NodeBodyUpdate updateBody)
        {
            return await _adminAlfrescoHttpClient.UpdateNode(nodeId, updateBody);
        }

        #endregion

        #region Private Methods

        private async Task DeleteFileAndAllAssociatedComponents(string nodeId)
        {
            var request = new AlfrescoPagingRequest<NodeChildAssociationPagingFixed, List19Fixed, NodeChildAssociationEntry>(
                parameters => _alfrescoHttpClient.GetNodeSecondaryChildren(nodeId, parameters)
            );

            while (await request.Next())
            {
                var response = request.Response();
                if (!(response?.List?.Entries?.Count > 0))
                    break;

                foreach (var item in response.List.Entries.ToList())
                    if (item.Entry.NodeType == SpisumNames.NodeTypes.Component)
                        await _alfrescoHttpClient.DeleteNode(item.Entry.Id, ImmutableList<Parameter>.Empty
                            .Add(new Parameter(AlfrescoNames.Headers.Permanent, true, ParameterType.QueryString)));
            }

            await _alfrescoHttpClient.DeleteNode(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Permanent, true, ParameterType.QueryString)));
        }

        private async Task<List<string>> GetAspectNamesWithoutOwnable(string nodeId)
        {
            var documentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);
            var aspects = documentInfo?.Entry?.AspectNames;
            aspects?.Remove(AlfrescoNames.Aspects.Ownable);

            return aspects?.ToList();
        }

        private FileContentResult GetFileContentResult(FormDataParam file, string fileName)
        {
            return new FileContentResult(file.File, file.ContentType)
            {
                FileDownloadName = $"{fileName}"
            };
        }

        private async Task<NodeEntry> MoveByNode(string nodeId, string targetNodeId)
        {
            return await _alfrescoHttpClient.NodeMove(nodeId, new NodeBodyMove { TargetParentId = targetNodeId });
        }

        private async Task UpdateFile(string nodeId, NodeBodyUpdateFixed updateBody, bool isMemberOfRepository) //NEEDS FIX AND RENAME
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            if (nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.File)
            {
                await _alfrescoHttpClient.UpdateNode(nodeId, updateBody);

                var childrenIds = (await GetSecondaryChildren(nodeId, SpisumNames.Associations.Documents)).Select(x => x.Entry.Id).ToList();
                await childrenIds.ForEachAsync(async document =>
                {
                    if (isMemberOfRepository)
                        updateBody.AspectNames = await GetAspectNamesWithoutOwnable(document);

                    await _alfrescoHttpClient.UpdateNode(document, updateBody);

                    var documentChildrenIds = (await GetSecondaryChildren(document, SpisumNames.Associations.Components)).Select(x => x.Entry.Id).ToList();
                    await documentChildrenIds.ForEachAsync(async component =>
                    {
                        if (isMemberOfRepository)
                            updateBody.AspectNames = await GetAspectNamesWithoutOwnable(component);

                        await _alfrescoHttpClient.UpdateNode(component, updateBody);
                    });
                });
            }

            else if (nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.Document || nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.Concept)
            {
                await _alfrescoHttpClient.UpdateNode(nodeId, updateBody);

                var childrenIds = (await GetSecondaryChildren(nodeId, SpisumNames.Associations.Components)).Select(x => x.Entry.Id).ToList();
                await childrenIds.ForEachAsync(async x =>
                {
                    if (isMemberOfRepository)
                        updateBody.AspectNames = await GetAspectNamesWithoutOwnable(x);

                    await _alfrescoHttpClient.UpdateNode(x, updateBody);
                });
            }

            else if (nodeInfo.Entry.NodeType == SpisumNames.NodeTypes.Component)
                await _alfrescoHttpClient.UpdateNode(nodeId, updateBody);
        }

        private async Task<NodeEntry> UpdateForSignaturePermisions(string nodeId, string user, string group)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Permissions}", ParameterType.QueryString)));

            var updateBody = new NodeBodyUpdateFixed();
            var locallySet = nodeInfo?.Entry?.Permissions?.LocallySet;
            updateBody.Permissions.LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>();

            updateBody
                .AddPermission($"{group}_Sign", $"{GroupPermissionTypes.Coordinator}")
                .AddPermission($"{SpisumNames.Prefixes.UserGroup}{user}", $"{GroupPermissionTypes.Coordinator}")
                .AddProperty(SpisumNames.Properties.ForSignatureUser, user)
                .AddProperty(SpisumNames.Properties.ForSignatureGroup, group)
                .AddProperty(SpisumNames.Properties.ForSignatureDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                .AddProperty(SpisumNames.Properties.ReasonForRework, null);

            return await _alfrescoHttpClient.UpdateNode(nodeId, updateBody);
        }

        #endregion
    }
}