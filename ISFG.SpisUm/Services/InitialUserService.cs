using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.Interfaces;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.Services
{
    public class InitialUserService : IInitialUserService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public InitialUserService(IAlfrescoConfiguration alfrescoConfiguration, ISimpleMemoryCache simpleMemoryCache, ISystemLoginService systemLoginService) => 
            _alfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));

        #endregion

        #region Implementation of IInitialUserService

        public async Task CheckCreateGroupAndAddPerson(string userId, string groupName)
        {
            string groupId = null;

            try
            {
                var groupInfo = await _alfrescoHttpClient.GetGroup(groupName);
                groupId = groupInfo?.Entry?.Id;
            }
            catch
            {

            }

            if (groupId == null)
                try
                {
                    var group = await _alfrescoHttpClient.CreateGroup(new GroupBodyCreate { Id = groupName, DisplayName = groupName });
                    groupId = group?.Entry?.Id;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "CheckCreateGroupAndAddPerson group Fail");
                }

            if (groupId == null)
                return;

            try
            {
                var groupMembers = await _alfrescoHttpClient.GetGroupMembers(groupName, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Where, $"(memberType = '{GroupMembershipBodyCreateMemberType.PERSON}')", ParameterType.QueryString)));

                if (groupMembers?.List?.Entries?.ToList().Find(x => x.Entry?.Id == userId) == null)
                    await _alfrescoHttpClient.CreateGroupMember(groupName, new GroupMembershipBodyCreate { Id = userId, MemberType = GroupMembershipBodyCreateMemberType.PERSON });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CheckCreateGroupAndAddPerson groupMembers Fail");
            }
        }

        #endregion
    }
}