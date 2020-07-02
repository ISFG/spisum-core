using System.Collections.Immutable;
using System.Threading.Tasks;
using AutoMapper;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.AuthApi;
using ISFG.Signer.Client.Interfaces;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using Microsoft.Net.Http.Headers;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfiguration;
        private readonly ISignerConfiguration _signerConfiguration;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IMapper _mapper;

        #endregion

        #region Constructors

        public AuthorizationService(
            IAlfrescoHttpClient alfrescoHttpClient,
            IAlfrescoConfiguration alfrescoConfiguration,
            IMapper mapper, 
            ISignerConfiguration signerConfiguration)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _alfrescoConfiguration = alfrescoConfiguration;
            _mapper = mapper;
            _signerConfiguration = signerConfiguration;
        }

        #endregion

        #region Implementation of IAuthorizationService

        public async Task<Authorization> Login(string username, string password)
        {
            var response = await _alfrescoHttpClient.Login(new TicketBody {UserId = username, Password = password});
            
            var alfrescoProfile = await _alfrescoHttpClient.GetPerson("-me-", ImmutableList<Parameter>.Empty
                .Add(new Parameter(HeaderNames.Authorization, $"Basic {response.Entry.Id.ToAlfrescoAuthentication()}", ParameterType.HttpHeader)));

            return _mapper.Map<Authorization>((response, alfrescoProfile, _alfrescoConfiguration, _signerConfiguration));
        }

        #endregion
    }
}