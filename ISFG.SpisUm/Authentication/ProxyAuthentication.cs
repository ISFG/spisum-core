using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using RestSharp;

namespace ISFG.SpisUm.Authentication
{
    public class ProxyAuthentication : IAuthenticationHandler
    {
        #region Fields

        private readonly string _authorizationToken;

        #endregion

        #region Constructors

        public ProxyAuthentication(IHttpContextAccessor httpContextAccessor) =>
            _authorizationToken = httpContextAccessor?.HttpContext?.Request?.Headers[HeaderNames.Authorization];

        #endregion

        #region Implementation of IAuthenticationHandler

        public void AuthenticateRequest(IRestRequest request)
        {
            if (!string.IsNullOrEmpty(_authorizationToken))
                request.AddHeader(HeaderNames.Authorization, _authorizationToken);
        }

        public Task<bool> HandleNotAuthenticated() => Task.FromResult(false);

        #endregion
    }
}