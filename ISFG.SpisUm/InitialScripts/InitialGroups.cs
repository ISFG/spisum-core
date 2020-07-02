using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.Interfaces;
using ISFG.SpisUm.Models.V1;
using Newtonsoft.Json;
using Serilog;

namespace ISFG.SpisUm.InitialScripts
{
    public class InitialGroups : IInicializationScript
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfig;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IInitialGroupService _initialGroup;
        private bool _initiated;

        #endregion

        #region Constructors

        public InitialGroups(
            IAlfrescoConfiguration alfrescoConfiguration,
            IInitialGroupService initialGroup,
            ISimpleMemoryCache simpleMemoryCache,
            ISystemLoginService systemLoginService
        )
        {
            _alfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));
            _alfrescoConfig = alfrescoConfiguration;
            _initialGroup = initialGroup;
        }

        #endregion

        #region Implementation of IInicializationScript

        public async Task Init()
        {
            if (_initiated)
                return;
            _initiated = true;

            var configGroups = _alfrescoConfig?.Groups != null
                ? JsonConvert.DeserializeObject<List<GroupModel>>(File.ReadAllText(_alfrescoConfig.Groups))
                : new List<GroupModel>();

            var configRoles = _alfrescoConfig?.Roles != null
                ? JsonConvert.DeserializeObject<List<GroupBodyCreate>>(File.ReadAllText(_alfrescoConfig.Roles))
                : new List<GroupBodyCreate>();

            var permissions = Enum.GetValues(typeof(GroupPermissionTypes)).Cast<GroupPermissionTypes>().Select(x => $"_{x}").ToList();
            permissions.Insert(0, ""); // for basic name, for list all people in
            permissions.Add(SpisumNames.Postfixes.Sign); // for getting people who can sign

            var groups = configGroups.Select(x => x.Body).ToList();

            // mandatory groups
            groups.Insert(0, new GroupBodyCreate { Id = SpisumNames.Groups.DispatchGroup, DisplayName = "Výpravna" });          
            groups.Insert(0, new GroupBodyCreate { Id = SpisumNames.Groups.MailroomGroup, DisplayName = "Podatelna" });
            groups.Insert(0, new GroupBodyCreate { Id = SpisumNames.Groups.RepositoryGroup, DisplayName = "Spisovna" });

            // custom groups
            foreach (var group in groups)
            foreach (var permission in permissions)
                await CreateGroup(new GroupBodyCreate { Id = $"{group.Id}{permission}", DisplayName = $"{group.DisplayName}{permission}" });

            // system groups
            var systemGroups = new List<GroupBodyCreate>
            {
                new GroupBodyCreate { Id = SpisumNames.Groups.SpisumAdmin, DisplayName = SpisumNames.Groups.SpisumAdmin },
                new GroupBodyCreate { Id = SpisumNames.Groups.MainGroup, DisplayName = SpisumNames.Groups.MainGroup },
                new GroupBodyCreate { Id = SpisumNames.Groups.RolesGroup, DisplayName = "Role" }
            };

            foreach (var group in systemGroups)
                await CreateGroup(new GroupBodyCreate { Id = group.Id, DisplayName = group.DisplayName });

            foreach (var role in configRoles)
            {
                await CreateGroup(role);
                await _initialGroup.AddMainGroupMember(SpisumNames.Groups.RolesGroup, role.Id);
            }

            // add all groups to main group for access
            foreach (var group in groups.Where(x => Array.IndexOf(new[] {
                SpisumNames.Groups.DispatchGroup,
                SpisumNames.Groups.MainGroup, 
                SpisumNames.Groups.RolesGroup, 
                SpisumNames.Groups.RepositoryGroup,
                SpisumNames.Groups.SpisumAdmin
            }, x.Id) == -1))
                await _initialGroup.AddMainGroupMember(SpisumNames.Groups.MainGroup, group.Id);

            // add all repository groups
            foreach (var group in configGroups.Where(x => x.IsRepository == true))
                await _initialGroup.AddMainGroupMember(SpisumNames.Groups.RepositoryGroup, group.Body.Id);

            // add all dispatch groups
            foreach (var group in configGroups.Where(x => x.IsDispatch == true))
                await _initialGroup.AddMainGroupMember(SpisumNames.Groups.DispatchGroup, group.Body.Id);
        }

        #endregion

        #region Private Methods

        private async Task CreateGroup(GroupBodyCreate body)
        {
            string groupId = null;

            try 
            {
                var groupInfo = await _alfrescoHttpClient.GetGroup(body.Id);
                groupId = groupInfo?.Entry?.Id;
            }
            catch
            {

            }

            if (groupId == null)
                try
                {
                    await _alfrescoHttpClient.CreateGroup(body);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "CreateGroup Fail");
                }
        }

        #endregion
    }
}