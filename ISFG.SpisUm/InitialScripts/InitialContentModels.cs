using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.Configurations;
using ISFG.SpisUm.Interfaces;
using Microsoft.Net.Http.Headers;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.InitialScripts
{
    public class InitialContentModels : IInicializationScript
    {
        #region Fields

        private readonly IAlfrescoConfiguration _alfrescoConfig;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public InitialContentModels(
            IAlfrescoConfiguration alfrescoConfiguration, 
            ISimpleMemoryCache simpleMemoryCache,
            ISystemLoginService systemLoginService)
        {
            _alfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));
            _alfrescoConfig = alfrescoConfiguration;
        }

        #endregion

        #region Implementation of IInicializationScript

        public async Task Init()
        {
            string XSDFilePath = Path.Combine(_alfrescoConfig.ConfigurationFiles.FolderName,
                                              _alfrescoConfig.ConfigurationFiles.ContentModels.FolderName,
                                              _alfrescoConfig.ConfigurationFiles.ContentModels.XSDValidationFile);

            List<ConfigurationContent> files = (from string file in _alfrescoConfig.ConfigurationFiles.ContentModels.Files select new ConfigurationContent
            {
                FileName = file,
                FilePath = Path.Combine(_alfrescoConfig.ConfigurationFiles.FolderName,
                                        _alfrescoConfig.ConfigurationFiles.ContentModels.FolderName,
                                        file)
            }).ToList();

            foreach (var file in files)
                try
                {
                    var validationResult = XMLValidator.ValidateXML("http://www.alfresco.org/model/dictionary/1.0", file.FilePath, XSDFilePath);

                    if (validationResult.IsOK)
                    {
                        // Company Home
                        var repositoryRootFolder = await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root);
                        
                        var modelsNode = await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                            .Add(new Parameter(AlfrescoNames.Headers.RelativePath, $"{DataDictionaryConfiguration.DataDictionary}/{DataDictionaryConfiguration.Models}", ParameterType.QueryString)));
                        
                        FormDataParam fileParams;
                        using (var memstream = new MemoryStream())
                        {
                            File.OpenRead(file.FilePath).CopyTo(memstream);
                            
                            fileParams = new FormDataParam(memstream.ToArray(), file.FileName);
                        };        
                      
                        var createdChild = await _alfrescoHttpClient.CreateNode(modelsNode.Entry.Id, fileParams, ImmutableList<Parameter>.Empty
                            .Add(new Parameter(HeaderNames.ContentType, "multipart/form-data", ParameterType.HttpHeader))
                            .Add(new Parameter(AlfrescoNames.Headers.OverWrite, true, ParameterType.GetOrPost)));
                            

                        var properties = new NodeBodyUpdate
                        {
                            Properties = new Dictionary<string, object>
                            {
                                { AlfrescoNames.ContentModel.ModelActive, true }
                            }
                        };

                        await _alfrescoHttpClient.UpdateNode(createdChild.Entry.Id, properties,
                            ImmutableList<Parameter>.Empty
                                .Add(new Parameter(HeaderNames.ContentType, MediaTypeNames.Application.Json, ParameterType.HttpHeader)));
                    }
                    else
                    {
                        Log.Error(validationResult.ErrorMessage);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "XML or XML file not found");
                }
        }

        #endregion

        #region Nested Types, Enums, Delegates

        private class ConfigurationContent
        {
            #region Properties

            public string FileName { get; set; }
            public string FilePath { get; set; }

            #endregion
        }

        #endregion
    }
}