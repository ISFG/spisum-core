using System;
using ISFG.SpisUm.Filters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.SpisUm.Authentication
{
    public static class AuthenticationConfig
    {
        #region Static Methods

        public static void RegisterAlfrescoAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Alfresco Scheme";
                    options.DefaultChallengeScheme = "Alfresco Scheme";
                })
                .AddAlfrescoAuthentication(options => { });

            services.AddScoped<AlfrescoAuthenticateFilter>();
            services.AddScoped<AlfrescoAdminAuthenticateFilter>();
        }

        private static AuthenticationBuilder AddAlfrescoAuthentication(this AuthenticationBuilder authBuilder,
            Action<AlfrescoAuthenticationOptions> options) =>
            authBuilder.AddScheme<AlfrescoAuthenticationOptions, AlfrescoAuthenticationHandler>("Alfresco Scheme",
                "Alfresco Auth", options);

        #endregion
    }
}