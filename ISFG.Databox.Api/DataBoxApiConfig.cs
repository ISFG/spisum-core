using ISFG.DataBox.Api.Interfaces;
using ISFG.DataBox.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.DataBox.Api
{
    public static class DataBoxApiConfig
    {
        #region Static Methods

        public static void AddDataBoxApi(this IServiceCollection services, IDataBoxApiConfiguration dataBoxApiConfiguration)
        {
            services.AddSingleton(dataBoxApiConfiguration);
            
            services.AddScoped<IDataBoxHttpClient, DataBoxHttpClient>();
        }

        #endregion
    }
}