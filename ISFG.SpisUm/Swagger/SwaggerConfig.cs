using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ISFG.Common.Extensions;
using ISFG.SpisUm.Configurations;
using ISFG.SpisUm.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ISFG.SpisUm.Swagger
{
    public static class SwaggerConfig
    {
        #region Static Methods

        public static void UseSwaggerAll(this IApplicationBuilder app, IApiConfiguration apiConfiguration)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                var swaggers = apiConfiguration?.SwaggerOptions?
                    .Where(x => x != null)
                    .Where(x => x.Versions != null && x.Versions.Any())
                    .Select(x => x) ?? new List<SwaggerOptions>();
                
                swaggers.ForEach(swagger => swagger.Versions.ForEach(version =>
                {
                    if (version.Enabled)
                        c.SwaggerEndpoint($"/swagger/{swagger.Route}-{version.Version}/swagger.json", $"{swagger.Route}-{version.Version}");
                }));

                c.DisplayRequestDuration();
                c.RoutePrefix = string.Empty;
            });
        }

        public static void AddSwaggerAll(this IServiceCollection services, IApiConfiguration apiConfiguration)
        {
            services.AddApiVersioning(o =>
            {
                o.ReportApiVersions = true;
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
                //o.UseApiBehavior = false;
            });

            services.AddSwaggerGen(c =>
            {
                var swaggers = apiConfiguration?.SwaggerOptions?
                    .Where(x => x != null)
                    .Where(x => x.Versions != null && x.Versions.Any())
                    .Select(x => x) ?? new List<SwaggerOptions>();
                
                swaggers.ForEach(swagger => swagger.Versions.ForEach(version =>
                {
                    if (version.Enabled)
                        c.SwaggerDoc($"{swagger.Route}-{version.Version}", new OpenApiInfo
                        {
                            Title = swagger.DisplayName, Version = version.Version,
                            Description = swagger.Description
                        });
                }));

                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                
                // This Adds Authorization window into swagger
                c.AddSecurityDefinition("Alfresco Scheme", new OpenApiSecurityScheme
                {
                  Name = "Authorization",
                  In  = ParameterLocation.Header,
                  Type = SecuritySchemeType.ApiKey,
                  Description = "Please enter 'Basic' authentication."
                });
                
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference 
                            { 
                                Type = ReferenceType.SecurityScheme, 
                                Id = "Alfresco Scheme"
                            },
                            Scheme = "Basic",
                            Name = "Alfresco Scheme",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
                
                //Added custom Group in request filter
                c.OperationFilter<GroupFilter>();
                
                // This call remove version from parameter, without it we will have version as parameter 
                c.OperationFilter<RemoveParameterVersionFilter>();

                // This make replacement of v{version:apiVersion} to real version of corresponding swagger doc.
                c.DocumentFilter<ReplaceValueVersionFilter>();
                c.DocumentFilter<LowercaseDocumentFilter>();

                // This on used to exclude endpoint mapped to not specified in swagger version.
                c.DocInclusionPredicate((version, desc) =>
                {
                    var swaggerId = version?.Split("-");
                    if (swaggerId == null || swaggerId.Length != 2)
                        return false;
                    
                    if (!desc.TryGetMethodInfo(out MethodInfo methodInfo)) return false;
                    var versions = methodInfo.DeclaringType
                        .GetCustomAttributes(true)
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions);

                    return desc.RelativePath.Contains($"/{swaggerId[0]}/") && versions.Any(ver => $"v{ver}" == swaggerId[1]);
                });
                
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                // Uses full schema names to avoid v1/v2/v3 schema collisions see: https://github.com/domaindrivendev/Swashbuckle/issues/442
                c.CustomSchemaIds(x => x.FullName);
            });
        }

        #endregion
    }
}

/*
OLD VERSION MAPPING

                c.DocInclusionPredicate((version, desc) =>
                {
                    if (!desc.TryGetMethodInfo(out MethodInfo methodInfo)) return false;
                    var versions = methodInfo.DeclaringType
                        .GetCustomAttributes(true)
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions);
                    return versions.Any(v => $"v{v.ToString()}" == version);
                });
*/