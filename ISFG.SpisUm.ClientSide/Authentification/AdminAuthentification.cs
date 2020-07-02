using System;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.AuthApi;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Authentification
{
    public class AdminAuthentification : IAuthenticationHandler
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfiguration;
        private readonly ISimpleMemoryCache _simpleMemoryCache;
        private readonly ISystemLoginService _systemLoginService;

        #endregion

        #region Constructors

        public AdminAuthentification(ISimpleMemoryCache simpleMemoryCache, IAlfrescoConfiguration alfrescoConfiguration, ISystemLoginService systemLoginService)
        {
            _simpleMemoryCache = simpleMemoryCache;
            _alfrescoConfiguration = alfrescoConfiguration;
            _systemLoginService = systemLoginService;
        }

        #endregion

        #region Implementation of IAuthenticationHandler

        public void AuthenticateRequest(IRestRequest request)
        {
            string sysToken = _simpleMemoryCache.Get<string>(nameof(AdminAuthentification));
            if (!string.IsNullOrEmpty(sysToken))
                request.AddHeader(HeaderNames.Authorization, sysToken);
        }

        public async Task<bool> HandleNotAuthenticated()
        {
            HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(new Uri(new Uri(_alfrescoConfiguration.Url), "alfresco/api/-default-/public/authentication/versions/1/tickets"));
            webRequest.Method = "POST";
            webRequest.ContentType = MediaTypeNames.Application.Json;

            try
            {
                await CreateRequestBody(webRequest);

                HttpWebResponse response = (HttpWebResponse) webRequest.GetResponse();

                if (response?.StatusCode != HttpStatusCode.Created) 
                    return false;

                var responseModel = CreateResponse(response);

                if (responseModel?.Entry?.Id == null)
                    return false;

                
                if (_simpleMemoryCache.IsExist(nameof(AdminAuthentification)))
                    _simpleMemoryCache.Delete(nameof(AdminAuthentification));
                
                _simpleMemoryCache.Create(
                    nameof(AdminAuthentification), 
                    $"Basic {responseModel.Entry.Id.ToAlfrescoAuthentication()}", 
                    new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(_alfrescoConfiguration.TokenExpire ?? 30)
                    });

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "HandleNotAuthenticated");
                return false;
            }
        }

        #endregion

        #region Private Methods

        private async Task CreateRequestBody(HttpWebRequest webRequest)
        {
            var sendData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new TicketBody("admin", await _systemLoginService.GetPassword("admin"))));

            webRequest.ContentLength = sendData.Length;

            Stream newStream = webRequest.GetRequestStream();
            newStream.Write(sendData, 0, sendData.Length);
            newStream.Close();
        }

        private TicketEntry CreateResponse(HttpWebResponse response)
        {
            using Stream responseData = response.GetResponseStream();
            using StreamReader streamReader = new StreamReader(responseData);

            string contributorsAsJson = streamReader.ReadToEnd();

            return JsonConvert.DeserializeObject<TicketEntry>(contributorsAsJson,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                });
        }

        #endregion
    }
}