using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Common.Interfaces;
using Microsoft.Net.Http.Headers;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Authentification
{
    public class SignerAuthentification : IAuthenticationHandler
    {
        #region Fields

        private readonly ISimpleMemoryCache _simpleMemoryCache;
        private readonly string _token;

        #endregion

        #region Constructors

        public SignerAuthentification(ISimpleMemoryCache simpleMemoryCache, string token)
        {
            _simpleMemoryCache = simpleMemoryCache;
            _token = token;
        }

        #endregion

        #region Implementation of IAuthenticationHandler

        public void AuthenticateRequest(IRestRequest request)
        {
            var user = _simpleMemoryCache.Get<ClaimsPrincipal>(_token);
            var token = user?.Claims?.FirstOrDefault(x => x?.Type == @"http://spisum.cz/identity/claims/token");
            if (token != null)
                request.AddHeader(HeaderNames.Authorization, token.Value);
        }

        public Task<bool> HandleNotAuthenticated() => Task.FromResult(false);

        #endregion
    }
}