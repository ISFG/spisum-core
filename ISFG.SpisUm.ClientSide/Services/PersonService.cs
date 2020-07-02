using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class PersonService : IPersonService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IIdentityUser _identityUser;

        #endregion

        #region Constructors

        public PersonService(IIdentityUser identityUser, IAlfrescoHttpClient alfrescoHttpClient)
        {
            _identityUser = identityUser;
            _alfrescoHttpClient = alfrescoHttpClient;
        }

        #endregion

        #region Implementation of IPersonService

        public async Task<PersonGroup> GetCreateUserGroup()
        {
            if (string.IsNullOrWhiteSpace(_identityUser.RequestGroup))
                throw new BadRequestException("", "User has not properties set");

            GroupEntry groupInfo = null;

            var personGroupName = $"{SpisumNames.Prefixes.UserGroup}{_identityUser.Id}";

            try
            {
                groupInfo = await _alfrescoHttpClient.GetGroup(personGroupName);
            }
            catch
            {
                throw new BadRequestException("", "User group not found");
            }

            var groupMembers = await _alfrescoHttpClient.GetGroupMembers(personGroupName);

            if (groupMembers?.List?.Entries?.ToList().Find(x => x.Entry?.Id == _identityUser.Id) == null)

                await _alfrescoHttpClient.CreateGroupMember(personGroupName, new GroupMembershipBodyCreate { Id = _identityUser.Id, MemberType = GroupMembershipBodyCreateMemberType.PERSON });

            return new PersonGroup { PersonId = _identityUser.Id, GroupId = groupInfo.Entry.Id, GroupPrefix = _identityUser.RequestGroup };
        }

        #endregion
    }
}
