using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.Data.Models;
using ISFG.SpisUm.ClientSide.Enums;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class ComponentService : IComponentService
    {
        #region Fields

        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAuditLogService _auditLogService;
        private readonly IIdentityUser _identityUser;

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly INodesService _nodesService;
        private readonly IPersonService _personService;

        #endregion

        #region Constructors

        public ComponentService(IAlfrescoHttpClient alfrescoHttpClient, INodesService nodesService, IPersonService personService,
            IIdentityUser identityUser, IAuditLogService auditLogService)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _nodesService = nodesService;
            _personService = personService;
            _identityUser = identityUser;
            _auditLogService = auditLogService;
        }

        #endregion

        #region Implementation of IComponentService

        public async Task<List<string>> CancelComponent(string nodeId, List<string> componentsId)
        {
            List<string> unprocessedId = new List<string>();

            var parentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

            if (parentInfo?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Components, StringComparison.OrdinalIgnoreCase) != true)
            {
                foreach (var componentId in componentsId)
                    try
                    {
                        var componentEntryBeforeDelete = await _alfrescoHttpClient.GetNodeInfo(nodeId);

                        await _alfrescoHttpClient.DeleteSecondaryChildren(nodeId, componentId);
                        await _alfrescoHttpClient.CreateNodeSecondaryChildren(nodeId, new ChildAssociationBody
                        {
                            ChildId = componentId,
                            AssocType = SpisumNames.Associations.DeletedComponents
                        });

                        await UpdateMainFileComponentVersionProperties(nodeId, componentId, SpisumNames.VersionOperation.Remove);

                        try
                        {
                            var componentPid = componentEntryBeforeDelete?.GetPid();


                            if (parentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                                await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.VyjmutiZDokumentu,
                                    TransactinoHistoryMessages.ConceptComponentDeleteDocument);
                            else if (parentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                                await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.VyjmutiZDokumentu,
                                    TransactinoHistoryMessages.DocumentComponentDeleteDocument);

                            var documentFileParent = await _alfrescoHttpClient.GetNodeParents(nodeId, ImmutableList<Parameter>.Empty
                                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                                .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                            var fileId = documentFileParent?.List?.Entries?.FirstOrDefault()?.Entry?.Id;

                            if (fileId != null)
                                await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Component, componentPid, NodeTypeCodes.Komponenta, EventCodes.VyjmutiZDokumentu,
                                    TransactinoHistoryMessages.DocumentComponentDeleteFile);
                        }
                        catch (Exception ex)
                        {
                            Log.Logger?.Error(ex, "Audit log failed");
                        }
                    }
                    catch
                    {
                        unprocessedId.Add(componentId);
                    }

                ;

                return unprocessedId;
            }

            return await DeleteComponent(componentsId);
        }

        public async Task<NodeEntry> CreateVersionedComponent(string nodeId, IFormFile component)
        {
            var componentCreated = await CreateComponent(nodeId, component);

            await UpdateMainFileComponentVersionProperties(nodeId, componentCreated, SpisumNames.VersionOperation.Add);

            return componentCreated;
        }

        public async Task<string> GenerateComponentPID(string parentId, string separator, GeneratePIDComponentType type)
        {
            await semaphoreSlim.WaitAsync();

            try
            {
                var parentInfo = await _alfrescoHttpClient.GetNodeInfo(parentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

                var properties = parentInfo.Entry.Properties.As<JObject>().ToDictionary();
                var parentPID = properties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString();
                string propertyName = type == GeneratePIDComponentType.Component ? SpisumNames.Properties.ComponentCounter : SpisumNames.Properties.ShipmentCounter;

                if (int.TryParse(properties.GetNestedValueOrDefault(propertyName)?.ToString(), out int counter))
                    counter++;
                else
                    counter = 1;

                await _alfrescoHttpClient.UpdateNode(parentId, new NodeBodyUpdate()
                     .AddProperty(propertyName, counter));
                return $"{parentPID}{separator}{counter}";
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<string> GetComponentDocument(string nodeId)
        {
            var documentFileParent = await _alfrescoHttpClient.GetNodeParents(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Components}')", ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

            return documentFileParent?.List?.Entries?.FirstOrDefault()?.Entry?.Id;
        }

        public async Task UpdateAssociationCount(string nodeId)
        {
            var childrens = await _alfrescoHttpClient.GetNodeSecondaryChildren(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString))
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Components}')", ParameterType.QueryString)));

            await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.AssociationCount, childrens?.List.Pagination?.TotalItems ?? 0));
        }

        /// <summary>
        /// Upgrade version of the provided document
        /// </summary>
        /// <param name="nodeId">Document or Concept nodeId</param>
        /// <returns></returns>        
        public async Task<NodeEntry> UpgradeDocumentVersion(string nodeId)
        {
            List<VersionRecord> componentsVersionList = new List<VersionRecord>();
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            var nodeProperties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
            int.TryParse(nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.Version)?.ToString(), out int version);

            // Get all components
            var components = await _nodesService.GetSecondaryChildren(nodeId, SpisumNames.Associations.Components, false, true);
            components.ForEach(x =>
            {
                var properties = x?.Entry?.Properties.As<JObject>().ToDictionary();
                var versionLabel = properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.VersionLabel)?.ToString();

                componentsVersionList.Add(new VersionRecord { Id = x?.Entry?.Id, Version = versionLabel });
            });

            await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.ComponentVersionJSON, componentsVersionList) //JsonConvert.SerializeObject(componentsVersionList, Formatting.None, _jsonSettings))
                .AddProperty(SpisumNames.Properties.Version, ++version));

            // Create a new version of node
            return await _alfrescoHttpClient.UpdateContent(nodeId, new byte[] { 01 },
                ImmutableList<Parameter>.Empty
                .Add(new Parameter(HeaderNames.ContentType, MediaTypeNames.Application.Octet, ParameterType.HttpHeader)));
        }

        public async Task<NodeEntry> UploadNewVersionComponent(string nodeId, string componentId, IFormFile component)
        {
            return await UploadNewVersionComponent(nodeId, componentId, await component.GetBytes(), component.FileName, component.ContentType);
        }

        public async Task<NodeEntry> UploadNewVersionComponent(string nodeId, string componentId, byte[] component, string componentName, string mimeType)
        {
            var componentEntryBeforeUpload = await _alfrescoHttpClient.GetNodeInfo(componentId);
            string newName = $"{IdGenerator.GenerateId()}{ System.IO.Path.GetExtension(componentName) }";

            var body = new NodeBodyUpdate();

            if (System.IO.Path.GetExtension(componentEntryBeforeUpload?.Entry?.Name) != System.IO.Path.GetExtension(componentName))
                try
                {
                    var nameSplit = componentEntryBeforeUpload?.Entry?.Name?.Split(".");
                    newName = $"{nameSplit[0]}{System.IO.Path.GetExtension(componentName)}";
                }
                catch
                {
                    newName = $"{IdGenerator.GenerateId()}{ System.IO.Path.GetExtension(componentName) }"; // Default name if chanhing extension fails
                }

            // Name must be included in uploading the content. Otherwise Alfresco will not update content properties
            await _alfrescoHttpClient.UploadContent(new FormDataParam(component, newName, "filedata", mimeType), ImmutableList<Parameter>.Empty
                .Add(new Parameter("filename", newName, ParameterType.GetOrPost))
                .Add(new Parameter("destination", "null", ParameterType.GetOrPost))
                .Add(new Parameter("uploaddirectory", "", ParameterType.GetOrPost))
                .Add(new Parameter("createdirectory", "true", ParameterType.GetOrPost))
                .Add(new Parameter("majorVersion", "true", ParameterType.GetOrPost))
                .Add(new Parameter("username", "null", ParameterType.GetOrPost))
                .Add(new Parameter("overwrite", "true", ParameterType.GetOrPost))
                .Add(new Parameter("thumbnails", "null", ParameterType.GetOrPost))
                .Add(new Parameter("updatenameandmimetype", "true", ParameterType.GetOrPost))
                .Add(new Parameter("updateNodeRef", $"workspace://SpacesStore/{componentId}", ParameterType.GetOrPost)));

            var componentPID = await GenerateComponentPID(nodeId, "/", GeneratePIDComponentType.Component);

            await _alfrescoHttpClient.UpdateNode(componentId, body
                .AddProperty(SpisumNames.Properties.Pid, componentPID)
                .AddProperty(SpisumNames.Properties.FileName, componentName)
              );

            return await UpdateMainFileComponentVersionProperties(nodeId, componentId, SpisumNames.VersionOperation.Update);
        }

        #endregion

        #region Private Methods

        private async Task<NodeEntry> CreateComponent(string documentId, IFormFile component)
        {
            var personGroup = await _personService.GetCreateUserGroup();

            var componentResponse = await CreateComponentByPath(documentId, SpisumNames.Paths.Components, component, personGroup.PersonId);

            await _nodesService.CreatePermissions(componentResponse.Entry.Id, personGroup.GroupPrefix, _identityUser.Id);

            await _alfrescoHttpClient.CreateNodeSecondaryChildren(documentId, new ChildAssociationBody
            {
                AssocType = SpisumNames.Associations.Components,
                ChildId = componentResponse.Entry.Id
            });

            await UpdateAssociationCount(documentId);

            return componentResponse;
        }

        private async Task<NodeEntry> CreateComponentByPath(string documentId, string componentPath, IFormFile component, string ownerId)
        {
            var componentPID = await GenerateComponentPID(documentId, "/", GeneratePIDComponentType.Component);

            return await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new FormDataParam(
                    await component.GetBytes(),
                    $"{IdGenerator.GenerateId()}{ System.IO.Path.GetExtension(component.FileName) }"
                ), ImmutableList<Parameter>.Empty
                .Add(new Parameter(SpisumNames.Properties.Pid, componentPID, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Ref, documentId, ParameterType.GetOrPost))
                .Add(new Parameter(AlfrescoNames.Headers.NodeType, SpisumNames.NodeTypes.Component, ParameterType.GetOrPost))
                .Add(new Parameter(AlfrescoNames.Headers.RelativePath, componentPath ?? SpisumNames.Paths.MailRoomUnfinished, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.FileName, component.FileName, ParameterType.GetOrPost))
                .Add(new Parameter(AlfrescoNames.ContentModel.Owner, ownerId, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Group, _identityUser.RequestGroup, ParameterType.GetOrPost)));
        }

        private async Task<List<string>> DeleteComponent(IEnumerable<string> nodeIds)
        {
            List<string> errorIds = new List<string>();

            foreach (var nodeId in nodeIds)
                try
                {
                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                    if (nodeInfo?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Components, StringComparison.OrdinalIgnoreCase) != true)
                        throw new Exception();

                    var componentParent = await _alfrescoHttpClient.GetNodeParents(nodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Components}')", ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                    await _alfrescoHttpClient.DeleteNode(nodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Permanent, true, ParameterType.QueryString)));

                    await UpdateAssociationCount(componentParent?.List?.Entries?.FirstOrDefault()?.Entry?.Id);
                }
                catch
                {
                    errorIds.Add(nodeId);
                }

            return errorIds;
        }

        private async Task<NodeEntry> UpdateMainFileComponentVersionProperties(string nodeId, string componentId, string operation)
        {
            var componentEntry = await _alfrescoHttpClient.GetNodeInfo(componentId);

            return await UpdateMainFileComponentVersionProperties(nodeId, componentEntry, operation);
        }

        private async Task<NodeEntry> UpdateMainFileComponentVersionProperties(string nodeId, NodeEntry component, string operation)
        {
            var componentProperties = component?.Entry?.Properties?.As<JObject>().ToDictionary();

            await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate()
               .AddProperty(SpisumNames.Properties.ComponentVersion, componentProperties?.GetNestedValueOrDefault(AlfrescoNames.ContentModel.VersionLabel)?.ToString())
               .AddProperty(SpisumNames.Properties.ComponentVersionId, component?.Entry?.Id)
               .AddProperty(SpisumNames.Properties.ComponentVersionOperation, operation));

            return await UpgradeDocumentVersion(nodeId);
        }

        #endregion
    }
}
