using ISFG.Email.Api.Interfaces;
using ISFG.Email.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.Email.Api
{
    public static class EmailApiConfig
    {
        #region Static Methods

        public static void AddEmailApi(this IServiceCollection services, IEmailApiConfiguration emailApiConfiguration)
        {
            services.AddSingleton(emailApiConfiguration);
            
            services.AddScoped<IEmailHttpClient, EmailHttpClient>();
        }

        #endregion
    }
}