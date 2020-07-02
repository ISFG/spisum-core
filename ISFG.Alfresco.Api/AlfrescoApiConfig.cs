using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.Alfresco.Api
{
    public static class AlfrescoApiConfig
    {
        #region Static Methods

        public static void AddAlfrescoApi(this IServiceCollection services,
            IAlfrescoConfiguration alfrescoConfiguration)
        {
            services.AddSingleton(alfrescoConfiguration);

            services.AddTransient<IAlfrescoModelComparer, AlfrescoModelComparer>();
            services.AddScoped<IAlfrescoHttpClient, AlfrescoHttpClient>();
        }

        #endregion
    }
}