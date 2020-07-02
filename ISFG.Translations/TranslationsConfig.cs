using ISFG.Translations.Infrastructure;
using ISFG.Translations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.Translations
{
    public static class TranslationsConfig
    {
        #region Static Methods

        public static void AddTranslations(this IServiceCollection services)
        {
            services.AddScoped<ITranslateService, TranslateService>();
        }

        #endregion
    }
}