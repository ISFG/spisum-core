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
using ISFG.Common.Utils;
using ISFG.Data.Interfaces;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.Jobs.Authentication
{
    public class SystemAuthentication : IAuthenticationHandler
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfiguration;

        private readonly string _passwordSalt = "spisUm_Salt_2020";
        private readonly ISystemLoginRepository _systemLoginRepository;
        private readonly IHttpUserContextService _userContextService;

        #endregion

        #region Constructors

        public SystemAuthentication(
            IAlfrescoConfiguration alfrescoConfiguration,
            IHttpUserContextService userContextService, 
            ISystemLoginRepository systemLoginRepository)
        {
            _alfrescoConfiguration = alfrescoConfiguration;
            _userContextService = userContextService;
            _systemLoginRepository = systemLoginRepository;
        }

        #endregion

        #region Implementation of IAuthenticationHandler

        public async void AuthenticateRequest(IRestRequest request)
        {
            if (!string.IsNullOrEmpty(_userContextService.Current.Token))
                request.AddHeader(HeaderNames.Authorization, _userContextService.Current.Token);
        }

        public async Task<bool> HandleNotAuthenticated()
        {
            HttpWebRequest webRequest = (HttpWebRequest) WebRequest.Create(new Uri(new Uri(_alfrescoConfiguration.Url),
                "alfresco/api/-default-/public/authentication/versions/1/tickets"));
            webRequest.Method = "POST";
            webRequest.Timeout = 15000;
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

                _userContextService.Current.Token = $"Basic {responseModel.Entry.Id.ToAlfrescoAuthentication()}";
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
            var sysUser = await _systemLoginRepository.GetUser("admin");
            
            var sendData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new TicketBody
            {
                UserId = "admin",
                Password = sysUser?.Password != null ? Cipher.Decrypt(sysUser.Password, _passwordSalt) : "admin"
            }));

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