using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using AutoMapper;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Common.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace ISFG.SpisUm.Authentication
{
    public class AlfrescoAuthenticationHandler : AuthenticationHandler<AlfrescoAuthenticationOptions>
    {
        #region Fields

        private static readonly object Sync = new object();

        private readonly IAlfrescoConfiguration _alfrescoConfiguration;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IMapper _mapper;
        private readonly ISimpleMemoryCache _simpleMemoryCache;

        #endregion

        #region Constructors

        public AlfrescoAuthenticationHandler(
            IOptionsMonitor<AlfrescoAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            ISimpleMemoryCache simpleMemoryCache,
            IAlfrescoHttpClient alfrescoHttpClient,
            IMapper mapper,
            IAlfrescoConfiguration alfrescoConfiguration) : base(options, logger, encoder, clock)
        {
            _simpleMemoryCache = simpleMemoryCache;
            _alfrescoHttpClient = alfrescoHttpClient;
            _mapper = mapper;
            _alfrescoConfiguration = alfrescoConfiguration;
        }

        #endregion

        #region Override of AuthenticationHandler<AlfrescoAuthenticationOptions>

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string authorization = Request?.Headers[HeaderNames.Authorization];
            if (string.IsNullOrWhiteSpace(authorization)) 
                authorization = Request?.Query["token"];            

            if (string.IsNullOrEmpty(authorization))
                return AuthenticateResult.Fail("Authentication not provided.");

            ClaimsPrincipal principal = null;

            if (!_simpleMemoryCache.IsExist(authorization))
            {
                var alfrescoProfile = await _alfrescoHttpClient.GetPerson("-me-");
                
                if (alfrescoProfile?.Entry?.Id != null)
                    principal = _mapper.Map<ClaimsPrincipal>((alfrescoProfile, authorization));

                if (principal?.Claims == null)
                    return AuthenticateResult.Fail("Authentication not provided.");
                
                lock (Sync)
                {
                    if (!_simpleMemoryCache.IsExist(authorization))
                        _simpleMemoryCache.Create(authorization, principal, new MemoryCacheEntryOptions
                        {
                            SlidingExpiration = TimeSpan.FromMinutes(_alfrescoConfiguration.TokenExpire ?? 30),
                            Priority = CacheItemPriority.High
                        });
                }
            }
            else
            {
                principal = _simpleMemoryCache.Get<ClaimsPrincipal>(authorization);
            }

            return AuthenticateResult.Success(new AuthenticationTicket(principal, new AuthenticationProperties
            {
                AllowRefresh = false,
                IsPersistent = true
            }, "Alfresco Scheme"));
        }

        #endregion
    }
}