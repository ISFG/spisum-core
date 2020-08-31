using AutoMapper;
using FluentValidation.AspNetCore;
using ISFG.Alfresco.Api;
using ISFG.Alfresco.Api.Configurations;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Data;
using ISFG.Data.Configurations;
using ISFG.Data.Interfaces;
using ISFG.DataBox.Api;
using ISFG.DataBox.Api.Configurations;
using ISFG.DataBox.Api.Interfaces;
using ISFG.Email.Api;
using ISFG.Email.Api.Configurations;
using ISFG.Email.Api.Interfaces;
using ISFG.Exceptions;
using ISFG.Pdf;
using ISFG.Signer.Client;
using ISFG.Signer.Client.Configuration;
using ISFG.Signer.Client.Interfaces;
using ISFG.SpisUm.Authentication;
using ISFG.SpisUm.ClientSide;
using ISFG.SpisUm.Configurations;
using ISFG.SpisUm.Filters;
using ISFG.SpisUm.InitialScripts;
using ISFG.SpisUm.Interfaces;
using ISFG.SpisUm.Services;
using ISFG.SpisUm.Swagger;
using ISFG.Translations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ISFG.SpisUm
{
    public class Startup
    {
        #region Fields

        private const string CorsPolicy = "CorsPolicy";
        private readonly IWebHostEnvironment _environment;

        #endregion

        #region Constructors

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            ApiConfiguration = configuration.Bind<IApiConfiguration, ApiConfiguration>();
            CorsConfiguration = configuration.Bind<ICorsConfiguration, CorsConfiguration>();
            AlfrescoConfiguration = configuration.Bind<IAlfrescoConfiguration, AlfrescoConfiguration>();
            SpisUmConfiguration = configuration.Bind<ISpisUmConfiguration, SpisUmConfiguration>();
            DataBoxApiConfiguration = configuration.Bind<IDataBoxApiConfiguration, DataBoxApiConfiguration>();
            EmailApiConfiguration = configuration.Bind<IEmailApiConfiguration, EmailApiConfiguration>();
            DataConfiguration =  configuration.Bind<IDataConfiguration, DataConfiguration>();
            SignerConfiguration =  configuration.Bind<ISignerConfiguration, SignerConfiguration>();

            _environment = env;
        }

        #endregion

        #region Properties

        private IAlfrescoConfiguration AlfrescoConfiguration { get; }
        private IApiConfiguration ApiConfiguration { get; }
        private ICorsConfiguration CorsConfiguration { get; }
        private ISpisUmConfiguration SpisUmConfiguration { get; }
        private IDataBoxApiConfiguration DataBoxApiConfiguration { get; }
        private IEmailApiConfiguration EmailApiConfiguration { get; }
        private IDataConfiguration DataConfiguration { get; }
        private ISignerConfiguration SignerConfiguration { get; }

        #endregion

        #region Public Methods

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();

            app.UseSwaggerAll(ApiConfiguration);

            app.UseRouting();
            app.UseCors(CorsPolicy);

            app.UseMiddleware<ErrorHandlingMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public async void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(o => o.AddPolicy(CorsPolicy, builder =>
            {
                if (CorsConfiguration.Origins?.Length > 0)
                    builder.WithOrigins(CorsConfiguration.Origins)
                        .WithExposedHeaders("Content-Disposition")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                else
                    builder.AllowAnyOrigin()
                        .WithExposedHeaders("Content-Disposition")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
            }));

            services.AddAutoMapper(typeof(ClientSideConfig));
            services.AddClientSide();
            services.RegisterAlfrescoAuthentication();

            services
                .AddMvc(options =>
                {
                    options.Filters.Add<ValidationFilter>(1);
                    options.Filters.Add<LanguageFilter>(2);
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver(); // Json CamelCase
                    options.SerializerSettings.Converters.Add(new StringEnumConverter()); // Json enum to string
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore; // Json ignore null values
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                })
                .AddFluentValidation();
                //.AddXmlSerializerFormatters();

            services
                .AddControllers()
                .ConfigureApiBehaviorOptions(options => 
                {
                    // options.SuppressInferBindingSourcesForParameters = true; 
                });

            services.AddSwaggerAll(ApiConfiguration);

            services.AddSingleton(ApiConfiguration);
            services.AddSingleton(CorsConfiguration);
            services.AddSingleton(SpisUmConfiguration);

            services.AddExceptions();

            services.AddSigner(SignerConfiguration);
            services.AddDataBoxApi(DataBoxApiConfiguration);
            services.AddEmailApi(EmailApiConfiguration);
            services.AddAlfrescoApi(AlfrescoConfiguration);
            services.AddDatabase(DataConfiguration);
            services.AddPdf();
            services.AddTranslations();
            
            services.AddScoped<IAuthenticationHandler, ProxyAuthentication>();

            services.AddHttpContextAccessor();
            services.AddScoped<IHttpUserContextService, HttpUserContextService>();
            services.AddScoped<IInitialSiteService, InitialSiteService>();
            services.AddScoped<IInitialGroupService, InitialGroupService>();
            services.AddScoped<IInitialUserService, InitialUserService>();
            services.AddScoped(p => p.GetService<IHttpUserContextService>().Current);

            services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });

            services.AddScoped<InitialChangeAdminPassword>();
            services.AddScoped<InitialContentModels>();
            services.AddScoped<IInicializationScript, InitialGroups>();
            services.AddScoped<IInicializationScript, InitialUsers>();
            services.AddScoped<IInicializationScript, InitialSites>();
            services.AddScoped<IInicializationScript, InitialScriptFiles>();

            if (_environment.IsEnvironment("Localhost"))
                return;

            var serviceProvider = services.BuildServiceProvider();
            
            var adminPassword = serviceProvider.GetService<InitialChangeAdminPassword>().As<IInicializationScript>();
            await adminPassword.Init();

            // Init content model first
            var contentModel = serviceProvider.GetService<InitialContentModels>().As<IInicializationScript>();
            await contentModel.Init();

            // Init rest of the scripts
            var initScripts = serviceProvider.GetServices<IInicializationScript>();
            await initScripts.ForEachAsync(async x => await x.Init());
            
            //-----------------------------------------------------
        }

        #endregion
    }
}