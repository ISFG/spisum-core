using ISFG.Common.Wcf.Configurations;
using ISFG.Common.Wcf.Interfaces;
using ISFG.Signer.Client.Generated.Signer;
using ISFG.Signer.Client.Interfaces;
using ISFG.Signer.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ISFG.Signer.Client
{
    public static class SignerConfig
    {
        #region Static Methods

        public static IServiceCollection AddSigner(this IServiceCollection services, ISignerConfiguration signerConfiguration)
        {
            services.AddScoped<ISignerClient, SignerBaseClient>();
            //services.AddSingleton<ISignerRequestFactory, SignerRequestFactory>();
            
            services.AddSingleton(signerConfiguration);
            
            if (signerConfiguration.Base != null)
                services.AddSingleton<IChannelConfig<TSPWebServiceSoap>>(new CredentialChannelConfig<TSPWebServiceSoap>(signerConfiguration.Base));
            else
                services.AddSingleton<IChannelConfig<TSPWebServiceSoap>>(new BasicChannelConfig<TSPWebServiceSoap>(signerConfiguration.Url));

            return services;
        }

        #endregion
    }
}