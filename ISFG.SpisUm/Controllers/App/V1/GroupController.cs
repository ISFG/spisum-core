using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Models.V1;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/group")]
    public class GroupController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public GroupController(IAlfrescoHttpClient alfrescoHttpClient)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get members of the group
        /// </summary>
        [HttpGet("{groupId}/members")]
        public async Task<GroupMemberPaging> GetGroupMembers([FromRoute] string groupId, [FromQuery] AdvancedBasicQueryParams queryParams)
        {
            var response = await _alfrescoHttpClient.GetGroupMembers(groupId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));

            var responseList = response?.List?.Entries?.ToList();
            var systemUser= responseList?.Find(x => x?.Entry?.Id == SpisumNames.SystemUsers.Spisum);
            if (systemUser != null)
            {
                responseList.Remove(systemUser);
                response.List.Entries = responseList;
            }

            return response;
        }

        /// <summary>
        /// GET - Get all groups
        /// </summary>
        /// <returns></returns>
        [HttpGet("all")]
        public async Task<AllGroupModel> GetGroups()
        {
            var mainGroup = await _alfrescoHttpClient.GetGroupMembers(SpisumNames.Groups.MainGroup,
                ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Where, AlfrescoNames.MemberType.Group, ParameterType.QueryString)));
            var dispatchGroup = await _alfrescoHttpClient.GetGroupMembers(SpisumNames.Groups.DispatchGroup,
                ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Where, AlfrescoNames.MemberType.Group, ParameterType.QueryString)));
            var repositoryGroup = await _alfrescoHttpClient.GetGroupMembers(SpisumNames.Groups.RepositoryGroup,
                ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Where, AlfrescoNames.MemberType.Group, ParameterType.QueryString)));

            var mainGroupList = mainGroup?.List?.Entries?.ToList();
            var dispatchGroupList = RemoveUnusedGroups(mainGroupList, dispatchGroup?.List?.Entries?.ToList());
            var repositoryGroupList = RemoveUnusedGroups(mainGroupList, repositoryGroup?.List?.Entries?.ToList());

            return new AllGroupModel
            {
                Dispatch = dispatchGroupList?.Select(x => x.Entry),
                Main = mainGroupList?.Select(x => x.Entry),
                Repository = repositoryGroupList?.Select(x => x.Entry)
            };
        }

        #endregion

        #region Private Methods

        private List<GroupMemberEntry> RemoveUnusedGroups(List<GroupMemberEntry> mainGroup, List<GroupMemberEntry> groups)
        {
            if (groups == null)
                return null;

            for (var i = groups.Count - 1; i >= 0; i--)
                if (!mainGroup.Exists(x => x.Entry?.Id == groups[i]?.Entry?.Id))
                    groups.RemoveAt(i);

            return groups;
        }

        #endregion
    }
}