using ISFG.Common.Attributes;
using Microsoft.Extensions.Configuration;

namespace ISFG.Common.Extensions
{
    public static class ConfigurationExt
    {
        #region Static Methods

        public static TService Bind<TService, TImplementation>(this IConfiguration configuration)
            where TService : class
            where TImplementation : class, TService, new()
        {
            TService implConfig = new TImplementation();

            var selection = typeof(TImplementation).GetAttributeValue((SettingsAttribute x) => x.Selection) ?? string.Empty;
            configuration.GetSection(selection).Bind(implConfig);

            return implConfig;
        }

        #endregion
    }
}
