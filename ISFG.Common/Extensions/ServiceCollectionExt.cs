using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.Common.Extensions
{
    public static class ServiceCollectionExt
    {
        #region Static Methods

        public static TService AddConfiguration<TService, TImplementation>(this IServiceCollection services, IConfiguration configuration)
            where TService : class
            where TImplementation : class, TService, new()
        {
            TService implConfig = configuration.Bind<TService, TImplementation>();

            services.AddSingleton(implConfig);

            return implConfig;
        }

        #endregion
    }
}
