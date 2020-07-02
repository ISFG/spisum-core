using ISFG.Pdf.Interfaces;
using ISFG.Pdf.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.Pdf
{
    public static class PdfConfig
    {
        #region Static Methods

        public static IServiceCollection AddPdf(this IServiceCollection services)
        {
            services.AddScoped<IPdfService, PdfService>();

            return services;
        }

        #endregion
    }
}