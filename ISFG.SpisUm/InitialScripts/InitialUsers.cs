using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.Interfaces;
using ISFG.SpisUm.Models.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace ISFG.SpisUm.InitialScripts
{
    public class InitialUsers : IInicializationScript
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfig;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IInitialUserService _initialUser;

        private readonly UserARM DataboxAdmin = new UserARM
        {
            Body = new PersonBodyCreate
            {
                Id = SpisumNames.SystemUsers.Databox,
                FirstName = "databox",
                Email = "spisum@spisum.cz",
                Enabled = true,
                EmailNotificationsEnabled = true,
                LastName = "databox",
                Password = SpisumNames.SystemUsers.Databox
            },
            Groups = new List<string> { "GROUP_DATABOX" }
        };

        private readonly UserARM EmailboxAdmin = new UserARM
        {
            Body = new PersonBodyCreate
            {
                Id = SpisumNames.SystemUsers.Emailbox,
                FirstName = "emailbox",
                Email = "spisum@spisum.cz",
                Enabled = true,
                EmailNotificationsEnabled = true,
                LastName = "emailbox",
                Password = SpisumNames.SystemUsers.Emailbox
            },
            Groups = new List<string> { "GROUP_EMAILBOX" }
        };

        private readonly UserARM SpisumAdmin = new UserARM
        {
            Body = new PersonBodyCreate
            {
                Id = SpisumNames.SystemUsers.SAdmin,
                FirstName = "Spisum",
                Email = "spisum@spisum.cz",
                Enabled = true,
                EmailNotificationsEnabled = true,
                LastName = "Admin",
                Password = SpisumNames.SystemUsers.SAdmin
            },
            Groups = new List<string> { "GROUP_ALFRESCO_ADMINISTRATORS" }
        };

        private readonly UserARM SpisumSuperuser = new UserARM
        {
            Body = new PersonBodyCreate
            {
                Id = SpisumNames.SystemUsers.Spisum,
                FirstName = "Spisum",
                Email = "spisum@spisum.cz",
                Enabled = true,
                EmailNotificationsEnabled = true,
                LastName = "Superuser",
                Password = SpisumNames.SystemUsers.Spisum
            },
            Groups = new List<string> { SpisumNames.Groups.SpisumAdmin },
            MainGroup = SpisumNames.Groups.MailroomGroup
        };

        private bool _initiated;

        #endregion

        #region Constructors

        public InitialUsers(
            IAlfrescoConfiguration alfrescoConfiguration,
            IInitialUserService initialUser,
            ISimpleMemoryCache simpleMemoryCache,
            ISystemLoginService systemLoginService
        )
        {
            _alfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));
            _alfrescoConfig = alfrescoConfiguration;
            _initialUser = initialUser;
        }

        #endregion

        #region Implementation of IInicializationScript

        public async Task Init()
        {
            if (_initiated)
                return;
            _initiated = true;

            var configGroups = (_alfrescoConfig?.Groups != null
                ? JsonConvert.DeserializeObject<List<GroupModel>>(File.ReadAllText(_alfrescoConfig.Groups))
                : new List<GroupModel>()).Select(x => x.Body).ToList();

            var configUsers = _alfrescoConfig?.Users != null
                ? JsonConvert.DeserializeObject<List<UserARM>>(File.ReadAllText(_alfrescoConfig.Users))
                : new List<UserARM>();

            configUsers.Insert(0, SpisumAdmin);
            configUsers.Insert(0, EmailboxAdmin);
            configUsers.Insert(0, DataboxAdmin);
            configUsers.Insert(0, SpisumSuperuser);
            
            foreach (var user in configUsers) 
                await CheckCreateUser(user);

            // add system user to all groups
            configGroups.Add(new GroupBodyCreate { Id = SpisumNames.Groups.RepositoryGroup });
            configGroups.Add(new GroupBodyCreate { Id = SpisumNames.Groups.MailroomGroup });

            foreach (var group in configGroups)
                await _initialUser.CheckCreateGroupAndAddPerson(SpisumNames.SystemUsers.Spisum, group.Id);

            // delete alfresco default users
            var deleteUsers = new[] { "abeecher", "mjackson" };

            foreach (var user in deleteUsers)
                try
                {
                    var userInfo = await _alfrescoHttpClient.GetPerson(user);
                    await _alfrescoHttpClient.DeletePerson(user);
                }
                catch
                {

                }
        }

        #endregion

        #region Private Methods

        private async Task CheckCreateUser(UserARM user)
        {
            if (user.Body == null)
                return;

            string userId = null;
            var updatePropertes = false;

            try
            {
                var userProperties = user.Body.Properties?.As<JObject>()?.ToDictionary();
                if (userProperties == null)
                    userProperties = new Dictionary<string, object>();
                if (user.MainGroup != null)
                    userProperties.Add(SpisumNames.Properties.Group, user.MainGroup);

                user.Body.Properties = userProperties;

                var userInfo = await _alfrescoHttpClient.GetPerson(user.Body.Id);
                userId = userInfo?.Entry?.Id;
                var properties = userInfo.Entry?.Properties?.As<JObject>()?.ToDictionary();

                updatePropertes = needUpdateProperties(properties, userProperties);
            }
            catch
            {

            }

            if (userId == null)
                try
                {
                    await _alfrescoHttpClient.CreatePerson(user.Body);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "CreatePerson Fail");
                }
            else if (updatePropertes)
                try
                {
                    await _alfrescoHttpClient.UpdatePerson(user.Body.Id, new PersonBodyUpdate { Properties = user.Body.Properties });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "UpdatePerson Fail");
                }

            await _initialUser.CheckCreateGroupAndAddPerson(user.Body.Id, $"{SpisumNames.Prefixes.UserGroup}{user.Body.Id}");
            await _initialUser.CheckCreateGroupAndAddPerson(user.Body.Id, user.MainGroup);

            if (user.Groups?.Count > 0)
                foreach (var group in user.Groups)
                    await _initialUser.CheckCreateGroupAndAddPerson(user.Body.Id, group);
        }

        private bool needUpdateProperties(Dictionary<string, object> properties, Dictionary<string, object> userProperties)
        {
            if (userProperties == null)
                return false;

            if (properties == null)
                return true;

            foreach (var userProp in userProperties)
                if (!properties.ContainsKey(userProp.Key) || properties[userProp.Key] != userProp.Value)
                    return true;

            return false;
        }

        #endregion
    }
}