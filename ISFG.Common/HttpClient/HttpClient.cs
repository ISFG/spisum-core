using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Common.Exceptions;
using ISFG.Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;

namespace ISFG.Common.HttpClient
{
    public abstract class HttpClient
    {
        #region Fields

        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly RestClient _restClient;

        #endregion

        #region Constructors

        protected HttpClient(string baseUrl)
        {
            BaseUrl = baseUrl;
            _restClient = new RestClient(baseUrl);
        }

        #endregion

        #region Properties

        protected string BaseUrl { get; }

        #endregion

        #region Protected Methods

        protected virtual void BuildContent(RestRequest request, object body) =>
            request.AddJsonBody(JsonConvert.SerializeObject(body, Formatting.None, _jsonSettings));

        protected virtual object BuildResponse<T>(IRestResponse response) =>
            !string.IsNullOrEmpty(response.Content)
                ? JsonConvert.DeserializeObject<T>(response.Content, _jsonSettings)
                : default;

        protected virtual async Task<T> ExecuteRequest<T>(Method httpMethod, string url, object body = null, IImmutableList<Parameter> parameters = null) =>
            await ExecuteHttpRequest<T>(httpMethod, url, body, parameters);

        protected virtual void HandleException(Exception ex) => throw ex;

        protected virtual void LogHttpRequest(IRestResponse response)
        {
        }

        protected virtual void PrepareRequest(IRestRequest request)
        {
        }

        #endregion

        #region Private Methods

        private async Task<T> ExecuteHttpRequest<T>(Method httpMethod, string url, object body, IImmutableList<Parameter> parameters)
        {
            if (url == null)
                url = string.Empty;

            if (!string.IsNullOrEmpty(url) && url.StartsWith('/'))
                url = url.Substring(1);

            try
            {
                var request = new RestRequest(url, httpMethod);

                if (parameters != null && parameters.Any())
                    parameters.ForEach(x => request.AddParameter(x));

                if (body != null)
                    BuildContent(request, body);

                PrepareRequest(request);

                var response = await _restClient.ExecuteAsync(request);
                
                LogHttpRequest(response);

                if ((int) response.StatusCode < 200 || (int) response.StatusCode >= 300)
                    throw new HttpClientException(response.StatusCode, response.Content);

                return (T) BuildResponse<T>(response);
            }
            catch (Exception ex)
            {
                HandleException(ex);
                throw;
            }
        }

        #endregion
    }
}