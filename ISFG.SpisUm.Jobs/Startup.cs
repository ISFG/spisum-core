using System;
using ISFG.Alfresco.Api;
using ISFG.Alfresco.Api.Configurations;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Data;
using ISFG.Data.Configurations;
using ISFG.Data.Interfaces;
using ISFG.Pdf;
using ISFG.Signer.Client;
using ISFG.Signer.Client.Configuration;
using ISFG.Signer.Client.Interfaces;
using ISFG.SpisUm.Jobs.Authentication;
using ISFG.SpisUm.Jobs.Configuration;
using ISFG.SpisUm.Jobs.Extension;
using ISFG.SpisUm.Jobs.Interfaces;
using ISFG.SpisUm.Jobs.Jobs;
using ISFG.SpisUm.Jobs.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ISFG.SpisUm.Jobs
{
    public class Startup
    {
        #region Constructors

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            AlfrescoConfiguration = configuration.Bind<IAlfrescoConfiguration, AlfrescoConfiguration>();
            TransactionHistoryConfiguration = configuration.Bind<ITransactionHistoryConfiguration, TransactionHistoryConfiguration>();
            DataConfiguration =  configuration.Bind<IDataConfiguration, DataConfiguration>();
            SignerConfiguration =  configuration.Bind<ISignerConfiguration, SignerConfiguration>();
        }

        #endregion

        #region Properties

        private IConfiguration Configuration { get; }
        private IAlfrescoConfiguration AlfrescoConfiguration { get; }
        private ITransactionHistoryConfiguration TransactionHistoryConfiguration { get; }
        private IDataConfiguration DataConfiguration { get; }
        private ISignerConfiguration SignerConfiguration { get; }

        #endregion

        #region Public Methods

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) 
                app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddControllers();

            services.AddAlfrescoApi(AlfrescoConfiguration);
            services.AddScoped<IAuthenticationHandler, SystemAuthentication>();
            
            services.AddSingleton<IHttpUserContextService, SystemUserContextService>();
            services.AddScoped(p => p.GetService<IHttpUserContextService>().Current);
            
            services.AddDatabase(DataConfiguration);
            services.AddSigner(SignerConfiguration);
            services.AddPdf();

            services.AddCronJob<AuditLogThumbprintJob>(c =>
            {
                c.TimeZoneInfo = TimeZoneInfo.Local;
                c.CronExpression = $@"{TransactionHistoryConfiguration.CronExpression}"; // https://github.com/HangfireIO/Cronos
            });
            
            services.AddSingleton(TransactionHistoryConfiguration);
            services.AddScoped<ITransformTransactionHistory, TransformTransactionHistory>();
        }

        #endregion
    }
}