using System;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Controllers.Auth.V1
{
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.AuthRoute + "/authentication")]
    public class AuthenticationController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAuthorizationService _authorizationService;
        private readonly IIdentityUser _identityUser;
        private readonly ISimpleMemoryCache _simpleMemoryCache;

        #endregion

        #region Constructors

        public AuthenticationController(
            IAlfrescoHttpClient alfrescoHttpClient,
            IAuthorizationService authorizationService,
            IIdentityUser identityUser,
            ISimpleMemoryCache simpleMemoryCache
        )
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _authorizationService = authorizationService;
            _identityUser = identityUser;
            _simpleMemoryCache = simpleMemoryCache;
        }

        #endregion

        #region Public Methods

        [HttpPost("login")]
        public async Task<Authorization> Login([FromQuery] string username, [FromQuery] string password)
        {
            if (Array.IndexOf(new[] { SpisumNames.SystemUsers.Admin, SpisumNames.SystemUsers.Databox, SpisumNames.SystemUsers.Emailbox }, username?.ToLower()) != -1)
                throw new ForbiddenException("403", "Forbidden user");
            return await _authorizationService.Login(username, password);
        }

        [HttpPost("logout")]
        public async Task Logout()
        {
            string token = _identityUser?.Token;
            if (string.IsNullOrEmpty(token))
                return;

            _simpleMemoryCache.Delete(token);
            await _alfrescoHttpClient.Logout();
        }

        [HttpGet("validate")]
        public async Task ValidateTicket()
        {
            await _alfrescoHttpClient.ValidateTicket();
        }

        #endregion
    }
}