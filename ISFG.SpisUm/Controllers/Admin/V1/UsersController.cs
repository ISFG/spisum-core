using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Interfaces;
using ISFG.SpisUm.Models.V1;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace ISFG.SpisUm.Controllers.Admin.V1
{
    [AlfrescoAdminAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.AdminRoute + "/users")]
    public class UsersController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IInitialUserService _initialUser;
        private readonly IUsersService _usersService;

        #endregion

        #region Constructors

        public UsersController(IAlfrescoHttpClient alfrescoHttpClient, IInitialUserService initialUser, IUsersService usersService)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _initialUser = initialUser;
            _usersService = usersService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Create user
        /// </summary>
        /// <param name="body">Create body</param>
        /// <returns>User info</returns>
        [HttpPost("create")]
        public async Task<PersonEntryFixed> CreateUser([FromBody] UserCreate body)
        {
            var userInfo = await _alfrescoHttpClient.CreatePerson(new PersonBodyCreate
            {
                Email = body.Email,
                FirstName = body.FirstName,
                Id = body.Id,
                LastName = body.LastName,
                Password = body.Password,
                Properties = new Dictionary<string, object>
                {
                    { SpisumNames.Properties.Group, body.MainGroup },
                    { SpisumNames.Properties.UserJob, body.UserJob },
                    { SpisumNames.Properties.UserOrgAddress, body.UserOrgAddress },
                    { SpisumNames.Properties.UserOrgId, body.UserOrgId },
                    { SpisumNames.Properties.UserOrgName, body.UserOrgName },
                    { SpisumNames.Properties.UserOrgUnit, body.UserOrgUnit },
                    { SpisumNames.Properties.UserName, $"{body.FirstName} {body.LastName}".Trim() },
                    { SpisumNames.Properties.UserId, body.UserId }
                }
            });

            var signGroups = body.SignGroups ?? new List<string>();
            var groupList = body.Groups?.ToList() ?? new List<string>();
            groupList.AddUnique(body.MainGroup);
            groupList.AddRangeUnique(signGroups);

            // user group
            await _initialUser.CheckCreateGroupAndAddPerson(userInfo.Entry.Id, $"{SpisumNames.Prefixes.UserGroup}{userInfo.Entry.Id}");

            foreach (var group in groupList)
                await _alfrescoHttpClient.CreateGroupMember(group, new GroupMembershipBodyCreate { Id = userInfo.Entry.Id, MemberType = GroupMembershipBodyCreateMemberType.PERSON });

            foreach (var group in signGroups)
                await _initialUser.CheckCreateGroupAndAddPerson(userInfo.Entry.Id, group + SpisumNames.Postfixes.Sign);

            return userInfo;
        }

        /// <summary>
        /// Deactivate user
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns>User info</returns>
        [HttpPost("{userId}/deactivate")]
        public async Task<PersonEntryFixed> DeactivateUser([FromRoute] string userId)
        {
            return await _alfrescoHttpClient.UpdatePerson(userId, new PersonBodyUpdate
            {
                Enabled = false
            });
        }

        /// <summary>
        /// Get user groups
        /// </summary>
        /// <param name="userId">User id</param>
        /// <param name="queryParams">Parameters</param>
        /// <returns>Groups list</returns>
        [HttpGet("{userId}/groups")]
        public async Task<GroupPagingFixed> GetGroups([FromRoute] string userId, [FromQuery] BasicNodeQueryParams queryParams)
        {
            return await _alfrescoHttpClient.GetPersonGroups(userId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));
        }

        /// <summary>
        /// Get users
        /// </summary>
        /// <param name="queryParams">Parameters</param>
        /// <returns>Users list</returns>
        [HttpGet]
        public async Task<PersonPagingFixed> GetUsers([FromQuery] BasicQueryParams queryParams)
        {
            return await _usersService.GetUsers(queryParams);
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="body">Change body</param>
        /// <returns>200 OK</returns>
        [HttpPost("change-password")]
        public async Task ChangePassword([FromBody] UserPasswordModel body)
        {
            await _alfrescoHttpClient.UpdatePerson(AlfrescoNames.Aliases.Me, new PersonBodyUpdate { OldPassword = body.OldPassword, Password = body.NewPassword });
        }

        /// <summary>
        /// Update user
        /// </summary>
        /// <param name="userId">User id</param>
        /// <param name="body">Update body</param>
        /// <returns>User info</returns>
        [HttpPost("{userId}/update")]
        public async Task<PersonEntryFixed> UpdateUser([FromRoute] string userId, [FromBody] UserUpdate body)
        {
            var update = new PersonBodyUpdate
            {
                Email = body.Email,
                FirstName = body.FirstName,
                LastName = body.LastName,
                Properties = new Dictionary<string, object>
                {
                    { SpisumNames.Properties.Group, body.MainGroup },
                    { SpisumNames.Properties.UserJob, body.UserJob },
                    { SpisumNames.Properties.UserOrgAddress, body.UserOrgAddress },
                    { SpisumNames.Properties.UserOrgId, body.UserOrgId },
                    { SpisumNames.Properties.UserOrgName, body.UserOrgName },
                    { SpisumNames.Properties.UserOrgUnit, body.UserOrgUnit },
                    { SpisumNames.Properties.UserName, $"{body.FirstName} {body.LastName}".Trim() },
                    { SpisumNames.Properties.UserId, body.UserId }
                }
            };

            if (!string.IsNullOrWhiteSpace(body.Password))
                update.Password = body.Password;

            var userInfo = await _alfrescoHttpClient.UpdatePerson(userId, update);

            var userGroupsInfo = await _alfrescoHttpClient.GetPersonGroups(userId);

            var userGroups = userGroupsInfo?.List?.Entries?.Select(x => x.Entry?.Id)?.Where(
                    x => Array.IndexOf(new[] { SpisumNames.Groups.MainGroup, SpisumNames.Groups.Everyone }, x) == -1 && !x.StartsWith(SpisumNames.Prefixes.UserGroup)
                )?.ToList() ?? new List<string>();

            var allGroupsInfo = await _alfrescoHttpClient.GetGroupMembers(SpisumNames.Groups.MainGroup,
               ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Where, AlfrescoNames.MemberType.Group, ParameterType.QueryString)));

            var allGroups = allGroupsInfo?.List?.Entries?.Select(x => x.Entry?.Id)?.ToList() ?? new List<string>();
            var signGroups = body.SignGroups ?? new List<string>();

            var groupList = body.Groups?.ToList() ?? new List<string>();
            groupList.AddUnique(body.MainGroup);
            groupList.AddRangeUnique(signGroups);

            // remove all groups included in main groups
            for (int i = userGroups.Count - 1; i >= 0; i--)
            {
                var group = userGroups[i];
                if (allGroups.Exists(x => x.StartsWith(@group)) && !groupList.Contains(@group)
                    || @group.EndsWith(SpisumNames.Postfixes.Sign) && !signGroups.Contains(@group))
                    try
                    {
                        await _alfrescoHttpClient.DeleteGroupMember(@group, userId);
                        userGroups.RemoveAt(i);
                    }
                    catch
                    {

                    }
            }

            foreach (var group in groupList.Where(x => !userGroups.Contains(x)))
                try
                {
                    await _alfrescoHttpClient.CreateGroupMember(@group, new GroupMembershipBodyCreate { Id = userInfo.Entry.Id, MemberType = GroupMembershipBodyCreateMemberType.PERSON });
                }
                catch
                {

                }

            foreach (var group in signGroups.Where(x => !userGroups.Contains(x + SpisumNames.Postfixes.Sign)))
                try
                {
                    await _initialUser.CheckCreateGroupAndAddPerson(userInfo.Entry.Id, @group + SpisumNames.Postfixes.Sign);
                }
                catch
                {

                }

            return userInfo;
        }

        #endregion
    }
}