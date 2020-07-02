using System;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.Interfaces;
using Serilog;

namespace ISFG.SpisUm.Services
{
    internal class InitialGroupService : IInitialGroupService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public InitialGroupService(IAlfrescoConfiguration alfrescoConfiguration, ISimpleMemoryCache simpleMemoryCache, ISystemLoginService systemLoginService) => 
            _alfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));

        #endregion

        #region Implementation of IInitialGroupService

        public async Task AddMainGroupMember(string mainGroup, string groupId)
        {
            var groupMembers = await _alfrescoHttpClient.GetGroupMembers(mainGroup);

            try
            {
                if (groupMembers?.List?.Entries?.ToList().Find(x => x.Entry?.Id == groupId) == null)
                    await _alfrescoHttpClient.CreateGroupMember(mainGroup, new GroupMembershipBodyCreate { Id = groupId, MemberType = GroupMembershipBodyCreateMemberType.GROUP });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "AddMainGroupMember Fail");
            }
        }

        #endregion
    }
}