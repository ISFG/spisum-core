using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.AuthApi;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Exceptions;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.Interfaces;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;

namespace ISFG.SpisUm.InitialScripts
{
    public class InitialChangeAdminPassword : IInicializationScript
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfiguration;

        private readonly string _defaultUser = "admin";

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly ISystemLoginService _systemLoginService;

        #endregion

        #region Constructors

        public InitialChangeAdminPassword(
            IAlfrescoConfiguration alfrescoConfiguration,
            ISimpleMemoryCache simpleMemoryCache,
            ISystemLoginService systemLoginService)
        {
            _systemLoginService = systemLoginService;
            _alfrescoConfiguration = alfrescoConfiguration;
        }

        #endregion

        #region Implementation of IInicializationScript

        public async Task Init()
        {
            var dbUser = await _systemLoginService.IsUserExists("admin");
            if (dbUser)
                return;
            
            var restClient = new RestClient(_alfrescoConfiguration.Url);
            
            var authRequest = new RestRequest("alfresco/api/-default-/public/authentication/versions/1/tickets", Method.POST);
            authRequest.AddJsonBody(JsonConvert.SerializeObject(new TicketBody(_defaultUser, _defaultUser), Formatting.None, _jsonSettings));
            var authResponse = await restClient.ExecuteAsync(authRequest);

            if ((int) authResponse.StatusCode < 200 || (int) authResponse.StatusCode >= 300)
                throw new HttpClientException(authResponse.StatusCode, authResponse.Content);
            
            var authContent = JsonConvert.DeserializeObject<TicketEntry>(authResponse.Content, _jsonSettings);
            
            var changeRequest = new RestRequest($"alfresco/api/-default-/public/alfresco/versions/1/people/{AlfrescoNames.Aliases.Me}", Method.PUT);
            changeRequest.AddJsonBody(JsonConvert.SerializeObject(new PersonBodyUpdate { OldPassword = _defaultUser, Password = await _systemLoginService.GetPassword(_defaultUser) }, Formatting.None, _jsonSettings));
            changeRequest.AddHeader(HeaderNames.Authorization, "Basic " + authContent.Entry.Id.ToAlfrescoAuthentication());
            var changeResponse = await restClient.ExecuteAsync(changeRequest);

            if ((int) changeResponse.StatusCode < 200 || (int) changeResponse.StatusCode >= 300)
                throw new HttpClientException(changeResponse.StatusCode, changeResponse.Content);
        }

        #endregion
    }
}