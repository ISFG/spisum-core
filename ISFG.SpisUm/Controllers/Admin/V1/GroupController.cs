using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.InitialScripts;
using ISFG.SpisUm.Interfaces;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.Controllers.Admin.V1
{
    [AlfrescoAdminAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.AdminRoute + "/group")]
    public class GroupController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IInitialGroupService _initialGroup;
        private readonly InitialSites _initScript;

        #endregion

        #region Constructors

        public GroupController(IAlfrescoHttpClient alfrescoHttpClient, IEnumerable<IInicializationScript> initScript, IInitialGroupService initialGroup)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _initialGroup = initialGroup;
            _initScript = initScript.FirstOrDefault(x => x.GetType().Name == nameof(InitialSites)) as InitialSites;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update group info
        /// </summary>
        /// <param name="groupId">Group id</param>
        /// <param name="body">Update body</param>
        /// <returns>200 OK</returns>
        [HttpPost("{groupId}/update")]
        public async Task Create([FromRoute] string groupId, [FromBody] UpdateGroup body)
        {
            await _alfrescoHttpClient.UpdateGroup(groupId, new GroupBodyUpdate { DisplayName = body.Name });
        }

        /// <summary>
        /// Create group
        /// </summary>
        /// <param name="body">Create body</param>
        /// <returns>200 OK</returns>
        [HttpPost("create")]
        public async Task Create([FromBody] CreateGroup body)
        {
            var groupId = $"{SpisumNames.Prefixes.Group}{body.Id}";

            try
            {
                var permissions = Enum.GetValues(typeof(GroupPermissionTypes)).Cast<GroupPermissionTypes>().Select(x => $"_{x}").ToList();
                permissions.Insert(0, ""); // for basic name, for list all people in
                permissions.Add(SpisumNames.Postfixes.Sign); // for getting people who can sign

                foreach (var permission in permissions)
                    await _alfrescoHttpClient.CreateGroup(new GroupBodyCreate
                    {
                        Id = $"{groupId}{permission}",
                        DisplayName = $"{body.Name}{permission}"
                    });

                // add to main group
                await _initialGroup.AddMainGroupMember(SpisumNames.Groups.MainGroup, groupId);
                await _alfrescoHttpClient.CreateGroupMember(groupId, new GroupMembershipBodyCreate { Id = SpisumNames.SystemUsers.Spisum, MemberType = GroupMembershipBodyCreateMemberType.PERSON });

                if (body.Type?.ToLower() == "dispatch")
                    await _initialGroup.AddMainGroupMember(SpisumNames.Groups.DispatchGroup, groupId);
                else if (body.Type?.ToLower() == "repository") await _initialGroup.AddMainGroupMember(SpisumNames.Groups.RepositoryGroup, groupId);

                await _initScript.ProcessGroup(groupId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Create group structure failed");
                await _alfrescoHttpClient.DeleteGroup(groupId);
            }
        }

        /// <summary>
        /// Delete group
        /// </summary>
        /// <param name="groupId">Group id</param>
        /// <returns>200 OK</returns>
        [HttpPost("{groupId}/delete")]
        public async Task Delete([FromRoute] string groupId)
        {
            if (Array.IndexOf(new[] {
                SpisumNames.Groups.DispatchGroup,
                SpisumNames.Groups.MainGroup,
                SpisumNames.Groups.MailroomGroup,
                SpisumNames.Groups.RepositoryGroup,
                SpisumNames.Groups.RolesGroup,
                SpisumNames.Groups.SpisumAdmin
            }, groupId) == -1)
            {
                await _alfrescoHttpClient.DeleteGroupMember(SpisumNames.Groups.MainGroup, groupId);
                await _alfrescoHttpClient.DeleteGroup(groupId);
            }
            else
                throw new ForbiddenException("403", "This Group can't be deleted");
        }

        /// <summary>
        /// Get members
        /// </summary>
        /// <param name="groupId">Group id</param>
        /// <param name="queryParams">Parameters</param>
        /// <returns>Group member list</returns>
        [HttpGet("{groupId}/members")]
        public async Task<GroupMemberPaging> GetGroupMembers([FromRoute] string groupId, [FromQuery] AdvancedBasicQueryParams queryParams)
        {
            var response = await _alfrescoHttpClient.GetGroupMembers(groupId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));

            var responseList = response?.List?.Entries?.ToList();
            var systemUser = responseList?.Find(x => x?.Entry?.Id == SpisumNames.SystemUsers.Spisum);

            if (systemUser != null)
            {
                responseList.Remove(systemUser);
                response.List.Entries = responseList;
            }

            return response;
        }

        #endregion
    }
}