using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using ISFG.Exceptions.Extensions;
using ISFG.Exceptions.Interfaces;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace ISFG.Exceptions
{
    public class ErrorHandlingMiddleware
    {
        #region Fields

        private readonly RequestDelegate _next;

        #endregion

        #region Constructors

        public ErrorHandlingMiddleware(RequestDelegate next) => _next = next;

        #endregion

        #region Public Methods

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                IExceptionHandler handler = ex.ResolveHandler(context.RequestServices);
                if (handler == null)
                {
                    Log.Error($"Can't find exception handler. Message:{ex.Message}, StackTrace:{ex.StackTrace}");
                    throw;
                }

                handler.LogException(ex);
                var exceptionModel = handler.HandleModel(ex);

                context.Response.ContentType = MediaTypeNames.Application.Json;
                context.Response.StatusCode = (int) handler.StatusCode;
                
                if (exceptionModel.GetType().GetProperties()
                    .Select(pi => pi.GetValue(exceptionModel))
                    .All(value => value == null))
                    return;
                
                var text = JsonConvert.SerializeObject(exceptionModel, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(), 
                    NullValueHandling = NullValueHandling.Ignore
                });
                
                await context.Response.WriteAsync(text);
            }
        }

        #endregion
    }
}