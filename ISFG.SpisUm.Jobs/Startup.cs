using System;
using Cronos;
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
using Serilog;

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
                c.TimeZoneInfo = GetTimeZoneFromConfiguration();
                c.CronExpression = GetCronExpressionFromConfiguration();
            });
            
            services.AddSingleton(TransactionHistoryConfiguration);
            services.AddScoped<ITransformTransactionHistory, TransformTransactionHistory>();
        }

        #endregion

        #region Private Methods

        private string GetCronExpressionFromConfiguration()
        {
            try
            {
                CronExpression.Parse(TransactionHistoryConfiguration?.Schedule?.CronExpression); // https://github.com/HangfireIO/Cronos
                return $@"{TransactionHistoryConfiguration?.Schedule?.CronExpression}"; 
            }
            catch
            {
                Log.Warning($"Couldn't parse cronExpression '{TransactionHistoryConfiguration?.Schedule?.CronExpression}' from configuration. The default cronExpression '0 2 * * *' has been set.");
                return @"0 2 * * *";
            }
        }

        private TimeZoneInfo GetTimeZoneFromConfiguration()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(TransactionHistoryConfiguration?.Schedule?.TimeZone ?? string.Empty);
            }
            catch
            {
                Log.Warning($"Couldn't parse timezone '{TransactionHistoryConfiguration?.Schedule?.TimeZone}' from configuration. The default 'Local' time zone has been set.");
                return TimeZoneInfo.Local;
            }
        }

        #endregion
    }
}