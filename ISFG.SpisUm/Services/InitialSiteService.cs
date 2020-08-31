using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.Site;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.Interfaces;
using ISFG.SpisUm.Models.V1;
using RestSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace ISFG.SpisUm.Services
{
    internal class InitialSiteService : IInitialSiteService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public InitialSiteService(
            IAlfrescoConfiguration alfrescoConfiguration, 
            ISimpleMemoryCache simpleMemoryCache, 
            ISystemLoginService systemLoginService)
        {
            _alfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));
        }

        #endregion

        #region Implementation of IInitialSiteService

        public async Task CreateSitesAndFolders(SitePaging sites, List<SiteARM> configSites, List<GroupModel> configGroups)
        {
            foreach (SiteARM configSite in configSites)
            {
                if (configSite.Body?.Id == null)
                    continue;

                var guid = sites.List.Entries.ToList().Find(x => x.Entry.Id == configSite.Body.Id)?.Entry?.Guid;
                
                configSite.FillQueryParams();

                if (guid == null)
                    try
                    {
                        var createdSite = await _alfrescoHttpClient.CreateSite(configSite.Body);

                        guid = createdSite.Entry.Guid;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "CreateSitesAndFolders Fail");
                    }

                await CheckCreatePermissions(guid, configSite.Permissions);
                await CheckCreatePermissionsForDocumentLibrary(configSite.Body.Id, configSite.Permissions);

                if (configSite?.Childs?.Count > 0 && guid != null)
                    await CheckSiteChilds(configSite.Childs, configSite.GroupStructure == true, guid, configGroups);
            }
        }

        public async Task CheckCreatePermissions(string nodeId, List<Permission> permissions)
        {
            if (permissions == null)
                return;

            try
            {
                var parameters = ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Permissions, ParameterType.QueryString));

                var info = await _alfrescoHttpClient.GetNodeInfo(nodeId, parameters);

                if (info?.Entry?.Id == null)
                    return;

                var locallySet = info?.Entry?.Permissions?.LocallySet == null ? new List<PermissionElement>() : info.Entry.Permissions.LocallySet.ToList();
                var changed = false;

                foreach (var permission in permissions)
                    if (!locallySet.Exists(x => x.AuthorityId == permission.Id))
                    {
                        changed = true;
                        locallySet.Add(new PermissionElement
                        {
                            AccessStatus = PermissionElementAccessStatus.ALLOWED,
                            AuthorityId = permission.Id,
                            Name = permission.Role
                        });
                    }

                if (changed)
                    await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate { Permissions = new PermissionsBody { LocallySet = locallySet } }, parameters);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckCreatePermissions Fail");
            }
        }

        public async Task CheckSiteChilds<T, U>(List<ChildrenARM<T, U>> childs, bool groupStructure, string guid, List<GroupModel> configGroups)
        {
            var documentLibrary = await _alfrescoHttpClient.GetNodeInfo(guid, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.RelativePath, AlfrescoNames.DocumentLibrary, ParameterType.QueryString)));

            var documentLibraryId = documentLibrary?.Entry?.Id;

            if (documentLibraryId == null)
            {
                Log.Error($"{guid} documentlibrary doesn't exist");
                return;
            }

            if (!groupStructure)
            {
                await CreateChilds(childs, documentLibraryId);
                return;
            }

            foreach (var group in configGroups.Where(x => x?.Body?.Id != null))
            {
                var permissions = Enum.GetValues(typeof(GroupPermissionTypes)).Cast<GroupPermissionTypes>().Select(x => new Permission { Id = $"{group.Body.Id}_{x}", Role = x.ToString() }).ToList();
                permissions.Insert(0, new Permission { Id = group.Body.Id, Role = "Consumer" });
                permissions.Insert(0, new Permission { Id = $"{group.Body.Id}_Sign", Role = "Collaborator" });

                string nodeId = null;

                try
                {
                    var groupFolder = await _alfrescoHttpClient.GetNodeInfo(documentLibraryId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.RelativePath, group.Body.Id, ParameterType.QueryString)));

                    nodeId = groupFolder?.Entry?.Id;
                }
                catch
                {
                }

                if (nodeId == null)
                    try
                    {
                        var result = await _alfrescoHttpClient.CreateNode(documentLibraryId, new NodeBodyCreate
                        {
                            Name = group.Body.Id,
                            NodeType = "cm:folder",
                            Properties = new Dictionary<string, object> {
                                { "cm:title", group.Body.DisplayName },
                                { "cm:description", group.Body.DisplayName }
                            }
                        });

                        nodeId = result?.Entry?.Id;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "CheckSiteChilds Fail");
                    }

                await CheckCreatePermissions(nodeId, permissions);
                await CreateChilds(childs, nodeId);
            }
        }

        #endregion

        #region Private Methods

        private async Task CreateChilds<T, U>(List<ChildrenARM<T, U>> childs, string parentId)
        {
            foreach (var child in childs)
            {
                string name = null;

                if (typeof(T) == typeof(NodeBodyCreate))
                    name = (child as ChildrenARM<NodeBodyCreate, U>)?.Body?.Name;

                if (name == null)
                    continue;

                string nodeId = null;

                try
                {
                    var node = await _alfrescoHttpClient.GetNodeInfo(parentId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.RelativePath, name, ParameterType.QueryString)));

                    nodeId = node?.Entry?.Id;
                }
                catch
                {
                }

                if (nodeId == null)
                    try
                    {
                        child.FillQueryParams();

                        if (typeof(T) == typeof(NodeBodyCreate))
                        {
                            var createdChild = await _alfrescoHttpClient.CreateNode(parentId, child.Body);

                            nodeId = createdChild?.Entry?.Id;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "CreateChilds Fail");
                    }

                await CheckCreatePermissions(nodeId, child.Permissions);

                if (child?.Childs?.Count > 0 && nodeId != null)
                    await CreateChilds(child.Childs, nodeId);
            }
        }

        private async Task CheckCreatePermissionsForDocumentLibrary(string site, List<Permission> permissions)
        {
            if (permissions == null)
                return;

            try
            {
                var parameters = ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.RelativePath, $"Sites/{site}/documentLibrary", ParameterType.QueryString));

                var documentLibraryInfo = await _alfrescoHttpClient.GetNodeInfo("-root-", parameters);

                if (documentLibraryInfo?.Entry?.Id != null)
                    await CheckCreatePermissions(documentLibraryInfo.Entry.Id, permissions);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckCreatePermissionsForDocumentLibrary Fail");
            }
        }

        private async Task SetPermissionInheritence(string nodeId)
        {
            try
            {
                await _alfrescoHttpClient.UpdateNode(nodeId, new NodeBodyUpdate
                {
                    Permissions = new PermissionsBody { IsInheritanceEnabled = true }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Permission Inheritence Fail");
            }
        }

        #endregion
    }
}