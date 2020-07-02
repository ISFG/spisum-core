using System.Collections.Immutable;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Models.V1;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/user")]
    public class UserController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public UserController(IAlfrescoHttpClient alfrescoHttpClient)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get user groups
        /// </summary>
        [HttpGet("{userId}/groups")]
        public async Task<GroupPagingFixed> GetGroups([FromRoute] string userId, [FromQuery] BasicNodeQueryParams queryParams)
        {
            return await _alfrescoHttpClient.GetPersonGroups(userId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));
        }

        /// <summary>
        /// Get user information
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<PersonEntryFixed> GetUserInfo([FromRoute] string userId)
        {
            return await _alfrescoHttpClient.GetPerson(userId);
        }

        /// <summary>
        /// Change password
        /// </summary>
        [HttpPost("change-password")]
        public async Task ChangePassword([FromBody] UserPasswordModel body)
        {
            await _alfrescoHttpClient.UpdatePerson(AlfrescoNames.Aliases.Me, new PersonBodyUpdate { OldPassword = body.OldPassword, Password = body.NewPassword });
        }

        #endregion
    }
}
