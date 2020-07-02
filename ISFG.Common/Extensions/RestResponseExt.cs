using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Newtonsoft.Json;
using RestSharp;

namespace ISFG.Common.Extensions
{
    public static class RestResponseExt
    {
        #region Static Methods

        public static string ToMessage(this IRestResponse response)
        {
            var logObj = new
            {
                Request = new
                {
                    Method = response?.Request?.Method.ToString(),
                    Url = response?.ResponseUri,
                    Paramaters = response?.Request?.Parameters?
                        .Where(x => x.Type != ParameterType.RequestBody)
                        .Select(param => new
                        {
                            param.Name, param.Value
                        }),
                    response?.Request?.Body?.ContentType,
                    Content = response?.Request?.Body?.ContentType != null && 
                              response?.Request?.Body?.Value != null && 
                              response.Request.Body.ContentType.Contains(MediaTypeNames.Application.Json) ? 
                        JsonConvert.DeserializeObject(response.Request.Body.Value.ToString()) : 
                        response?.Request?.Body?.Value?.ToString()
                },
                Response = new
                {
                    response?.StatusCode,
                    Headers = response?.Headers?.ToMessage(),
                    response?.ContentType,
                    Content = response?.ContentType != null && 
                              response?.Content != null && 
                              response.ContentType.Contains(MediaTypeNames.Application.Json) ? 
                        JsonConvert.DeserializeObject(response.Content) : 
                        response?.Content,
                    response?.ErrorMessage
                }
            };
            
            return JsonConvert.SerializeObject(logObj, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore, TypeNameHandling = TypeNameHandling.None
            });
        }

        private static string ToMessage(this IList<Parameter> parameters)
        {
            List<string> requestParameters = parameters?
                .Where(x => x.Type != ParameterType.RequestBody)
                .Select(param => $"{param.Name}: {param.Value}").ToList();

            if (requestParameters != null && requestParameters.Any())
                return string.Join(";", requestParameters.ToArray());

            return string.Empty;
        }

        #endregion
    }
}