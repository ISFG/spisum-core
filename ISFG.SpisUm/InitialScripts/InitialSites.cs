using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.Interfaces;
using ISFG.SpisUm.Models.V1;
using Newtonsoft.Json;
using RestSharp;

namespace ISFG.SpisUm.InitialScripts
{
    public class InitialSites : IInicializationScript
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfig;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IInitialSiteService _initialSite;
        private bool _initiated;

        #endregion

        #region Constructors

        public InitialSites(
            IAlfrescoConfiguration alfrescoConfiguration,
            IInitialSiteService initialSite,
            ISimpleMemoryCache simpleMemoryCache,
            ISystemLoginService systemLoginService
        )
        {
            _alfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));;
            _alfrescoConfig = alfrescoConfiguration;
            _initialSite = initialSite;
        }

        #endregion

        #region Implementation of IInicializationScript

        public async Task Init()
        {
            if (_initiated)
                return;
            _initiated = true;

            var configSites = _alfrescoConfig?.Sites != null
                ? JsonConvert.DeserializeObject<List<SiteARM>>(File.ReadAllText(_alfrescoConfig.Sites))
                : null;
            var configSitesRM = _alfrescoConfig?.SiteRM != null
                ? JsonConvert.DeserializeObject<RMSiteARM>(File.ReadAllText(_alfrescoConfig.SiteRM))
                : null;
            
            var configShreddingPlan = _alfrescoConfig?.ShreddingPlan != null
              ? JsonConvert.DeserializeObject<List<ShreddingPlanModel>>(File.ReadAllText(_alfrescoConfig.ShreddingPlan))
              : null;

            var configGroups = _alfrescoConfig?.Groups != null
                ? JsonConvert.DeserializeObject<List<GroupModel>>(File.ReadAllText(_alfrescoConfig.Groups))
                : new List<GroupModel>();

            configGroups.Insert(0, new GroupModel { Body = new GroupBodyCreate { Id = SpisumNames.Groups.MailroomGroup } });

            if (!(configSites?.Count > 0) && configSitesRM == null)
                return;
            
            var sites = await _alfrescoHttpClient.GetSites(ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "100", ParameterType.QueryString)));

            if (configSites != null)
                await _initialSite.CreateSitesAndFolders(sites, configSites, configGroups);

            if (configSitesRM != null && configSitesRM.Body != null)
                await _initialSite.CreateSiteRmAndFolders(sites, configSitesRM, configGroups);

            if (configShreddingPlan?.Count > 0)
            {
                foreach (var shreddingPlan in configShreddingPlan)
                    await _initialSite.CreateRMShreddingPlan(shreddingPlan);
            }
            
            // update ROOT permissions for all users because of PID generator script
            if (SpisumNames.Groups.MainGroup != null)
                await _initialSite.CheckCreatePermissions(AlfrescoNames.Aliases.Root, new List<Permission>
                {
                    new Permission
                    {
                        Id = SpisumNames.Groups.SpisumAdmin,
                        Role = AlfrescoNames.Permissions.SiteManager
                    },
                    new Permission
                    {
                        Id = SpisumNames.Groups.MainGroup,
                        Role = AlfrescoNames.Permissions.Editor
                    },
                    new Permission
                    {
                        Id = SpisumNames.Groups.EmailBox,
                        Role = AlfrescoNames.Permissions.Editor
                    },
                    new Permission
                    {
                        Id = SpisumNames.Groups.DataBox,
                        Role = AlfrescoNames.Permissions.Editor
                    }
                });
        }

        #endregion

        #region Public Methods

        public async Task ProcessGroup(string groupId)
        {
            var configSites = _alfrescoConfig?.Sites != null
               ? JsonConvert.DeserializeObject<List<SiteARM>>(File.ReadAllText(_alfrescoConfig.Sites))
               : null;

            var sites = await _alfrescoHttpClient.GetSites(
                ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "100", ParameterType.QueryString)));

            if (configSites != null)
                await _initialSite.CreateSitesAndFolders(sites, configSites, new List<GroupModel> { new GroupModel { Body = new GroupBodyCreate { Id = groupId } } });
        }

        #endregion
    }
}