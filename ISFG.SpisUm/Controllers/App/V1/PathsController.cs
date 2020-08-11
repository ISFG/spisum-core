using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.GsApi.GsApi;
using ISFG.Alfresco.Api.Models.Site;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Models.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/paths")]
    public class PathsController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfig;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly string _cacheKey = nameof(PathsController);
        private readonly ISimpleMemoryCache _simpleMemoryCache;

        #endregion

        #region Constructors

        public PathsController(
            IAlfrescoConfiguration alfrescoConfiguration,
            ISimpleMemoryCache simpleMemoryCache,
            ISystemLoginService systemLoginService
        )
        {
            _alfrescoConfig = alfrescoConfiguration;
            _alfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));
            _simpleMemoryCache = simpleMemoryCache;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Get all paths info
        /// </summary>
        /// <returns></returns>
        [HttpGet("all")]
        public async Task<List<PathsModel>> GetAllPaths()
        {
            if (_simpleMemoryCache.IsExist(_cacheKey))
                return _simpleMemoryCache.Get<List<PathsModel>>(_cacheKey);
            
            var paths = await FindAllPaths();

            _simpleMemoryCache.Create(_cacheKey, paths, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
            return paths;
        }

        #endregion

        #region Private Methods

        private bool FilterChildren<T, U>(List<ChildrenARM<T, U>> childs)
        {
            if (!(childs?.Count > 0))
                return false;

            for (var i = childs.Count - 1; i >= 0; i--)
            {
                var deleteItem = true;

                if (childs[i].ReturnInNodesInfo == true || FilterChildren(childs[i].Childs))
                    deleteItem = false;

                if (deleteItem)
                    childs.RemoveAt(i);
            }

            return childs.Count > 0;
        }

        private async Task<List<PathsModel>> FindAllPaths()
        {
            var configSites = _alfrescoConfig?.Sites != null
                ? JsonConvert.DeserializeObject<List<SiteARM>>(System.IO.File.ReadAllText(_alfrescoConfig.Sites))
                : new List<SiteARM>();
            var configSitesRM = _alfrescoConfig.SiteRM != null
                ? JsonConvert.DeserializeObject<RMSiteARM>(System.IO.File.ReadAllText(_alfrescoConfig.SiteRM))
                : null;

            var result = new List<PathsModel>();

            foreach (var configSite in configSites)
                FilterChildren(configSite.Childs);
            await GetAllPaths(configSites, result);

            FilterChildren(configSitesRM.Childs);

            if (configSitesRM != null)
            {
                var path = "rm/documentLibrary";
                result.Add(new PathsModel
                {
                    Name = "RM",
                    Childs = await GetAllChilds(configSitesRM.Childs, path, null),
                    Path = path,
                    Permissions = configSitesRM?.Permissions?.Select(x => x.Id)?.ToList() ?? new List<string>()
                });
            }

            return result;
        }

        private async Task<List<PathsModel>> GetAllChilds<T, U>(List<ChildrenARM<T, U>> childs, string path, string key)
        {
            if (childs == null)
                return null;

            var result = new List<PathsModel>();

            foreach (var child in childs)
            {
                string name = null;

                if (typeof(T) == typeof(NodeBodyCreate))
                    name = (child as ChildrenARM<NodeBodyCreate, U>)?.Body?.Name;
                else if (typeof(T) == typeof(RootCategoryBodyCreate))
                    name = (child as ChildrenARM<RootCategoryBodyCreate, U>)?.Body?.Name;
                else if (typeof(T) == typeof(RMNodeBodyCreateWithRelativePath))
                    name = (child as ChildrenARM<RMNodeBodyCreateWithRelativePath, U>)?.Body?.Name;

                if (name == null)
                    continue;

                var childPath = $"{path}/{name}";

                result.Add(new PathsModel
                {
                    Childs = await GetAllChilds(child.Childs, childPath, key == null ? name : key + name),
                    Name = name,
                    Path = childPath,
                    Permissions = child?.Permissions?.Select(x => x.Id)?.ToList() ?? new List<string>()
                });
            }

            return result;
        }

        private async Task GetAllPaths(List<SiteARM> configSites, List<PathsModel> result)
        {
            foreach (var configSite in configSites.Where(x => x.Body.Id != null))
            {
                var path = $"{configSite.Body.Id}/documentLibrary";
                result.Add(new PathsModel
                {
                    Name = configSite.Body.Id,
                    Childs = await GetAllChilds(configSite.Childs, path, null),
                    Path = path,
                    Permissions = configSite?.Permissions?.Select(x => x.Id)?.ToList() ?? new List<string>()
                });
            }
        }

        #endregion
    }
}