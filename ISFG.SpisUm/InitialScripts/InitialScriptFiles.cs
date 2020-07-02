using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Configurations;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.Rules;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.Configurations;
using ISFG.SpisUm.Interfaces;
using Microsoft.Net.Http.Headers;
using RestSharp;

namespace ISFG.SpisUm.InitialScripts
{
    public class InitialScriptFiles : IInicializationScript
    {
        #region Fields

        private const string StoreType = "workspace";
        private const string StoreId = "SpacesStore";

        private readonly IAlfrescoConfiguration _alfrescoConfig;
        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public InitialScriptFiles(IAlfrescoConfiguration alfrescoConfiguration, ISimpleMemoryCache simpleMemoryCache, ISystemLoginService systemLoginService)
        {
            _alfrescoHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));
            _alfrescoConfig = alfrescoConfiguration;
        }

        #endregion

        #region Properties

        private string scriptsNodeId { get; set; }

        #endregion

        #region Implementation of IInicializationScript

        public async Task Init()
        {
            scriptsNodeId = (await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.RelativePath, $"{DataDictionaryConfiguration.DataDictionary}/{DataDictionaryConfiguration.Scripts}", ParameterType.QueryString))))
                .Entry.Id;

            List<ConfigurationContent> scriptFiles = (from ScriptFile file in _alfrescoConfig.ConfigurationFiles.Scripts.Files select new ConfigurationContent
            {
                FileName = file.FileName,
                FilePath = Path.Combine(_alfrescoConfig.ConfigurationFiles.FolderName,
                                        _alfrescoConfig.ConfigurationFiles.Scripts.FolderName,
                                        file.FileName),
                Rules = file.Rules,
                Replaces = file.Replaces
            }).ToList();

            foreach (ConfigurationContent scriptFile in scriptFiles)
                try
                {
                    var existingPidGeneratorScriptFile = await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.RelativePath, $"{DataDictionaryConfiguration.DataDictionary}/{DataDictionaryConfiguration.Scripts}/{scriptFile.FileName}", ParameterType.QueryString)));

                    var content = await _alfrescoHttpClient.NodeContent(existingPidGeneratorScriptFile.Entry.Id);

                    var fileAfterReplaces = GetScriptFileAfterReplaces(scriptFile.FilePath, scriptFile.Replaces);

                    if (!FileUtils.IsFileEquals(fileAfterReplaces, content.File))
                        // If file change - Upload a new file
                        await UploadPidGeneratorFile(fileAfterReplaces, scriptFile.FileName, scriptFile.FileNameNoExtensions, scriptFile.Rules);
                    else
                        // IF file didn't changed - Recreate rule to be sure they are set properly
                        await CreateRules(scriptFile.FileNameNoExtensions, existingPidGeneratorScriptFile.Entry.Id, scriptFile.Rules);
                }
                catch
                {
                    // File don't exists - Upload it                    
                    var fileAfterReplaces = GetScriptFileAfterReplaces(scriptFile.FilePath, scriptFile.Replaces);
                    await UploadPidGeneratorFile(fileAfterReplaces, scriptFile.FileName, scriptFile.FileNameNoExtensions, scriptFile.Rules);
                }
        }

        #endregion

        #region Private Methods

        private async Task CreateRule(string fileNameWithoutExtension, string scriptId, string nodeIdForRule, List<string> ruleType)
        {
            // Check if there are any existing rules. If so, delete them
            var existingRules = await CheckIfRuleExists(fileNameWithoutExtension, nodeIdForRule);

            if (existingRules.Count > 0)
                await DeleteRules(nodeIdForRule, existingRules);

            var body = new CreateRuleAM
            {
                Title = fileNameWithoutExtension,
                ApplyToChildren = true,
                Disabled = false,
                ExecuteAsynchronously = false,
                RuleType = ruleType,
                Description = string.Empty,
                Action = new Action
                {
                    ActionDefinitionName = "composite-action"
                }
            };

            body.Action.Actions.Add(new Actions
            {
                ActionDefinitionName = "script",
                ParameterValues = new ActionParameterValues
                {
                    ScriptRef = $"{StoreType}://{StoreId}/{scriptId}"
                }
            });

            body.Action.Conditions.Add(new Conditions
            {
                ConditionDefinitionName = "no-condition",
                ParameterValues = new ConditionParameterValues()
            });

            await _alfrescoHttpClient.WebScriptsRuleCreate(StoreType, StoreId, nodeIdForRule, body);
        }

        private async Task CreateRules(string fileNameWithoutExtension, string scriptId, List<Rules> rules)
        {
            foreach (var rule in rules)
            {
                var nodeIdForRule = (await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.RelativePath, $"{rule.RelativePath}", ParameterType.QueryString))))
                    .Entry.Id;

                await CreateRule(fileNameWithoutExtension, scriptId, nodeIdForRule, rule.RuleType);
            }
        }

        private async Task DeleteRules(string nodeIdForRule, List<string> Ids)
        {
            foreach (string id in Ids) await _alfrescoHttpClient.WebScriptsNodeRuleDelete(StoreType, StoreId, nodeIdForRule, id);
        }

        private byte[] GetScriptFileAfterReplaces(string filePath, List<ReplaceTexts> replaceTexts)
        {
            var scriptText = File.ReadAllText(filePath);

            using (var memstream = new MemoryStream())
            {
                if (replaceTexts != null)
                    replaceTexts.ForEach(x =>
                    {
                        scriptText = scriptText.Replace(x.ReplaceText, x.ReplaceWithText);
                    });

                byte[] bytesData = Encoding.UTF8.GetBytes(scriptText);

                memstream.Write(bytesData);

                return memstream.ToArray();
            };            
        }

        private async Task<List<string>> CheckIfRuleExists(string fileName, string parentId)
        {
            var rules = await _alfrescoHttpClient.WebScriptsNodeRules(StoreType, StoreId, parentId);

            return rules.Data.Where(x => x.Title == fileName).Select(x => x.Id).ToList();
        }

        private async Task UploadPidGeneratorFile(byte[] sctipFile, string fileName, string fileNameWithoutExtension, List<Rules> rules)
        {
            var fileParams = new FormDataParam(sctipFile, fileName);

            var createdScriptPidGenerator = await _alfrescoHttpClient.CreateNode(scriptsNodeId, fileParams, ImmutableList<Parameter>.Empty
                .Add(new Parameter(HeaderNames.ContentType, "multipart/form-data", ParameterType.HttpHeader))
                .Add(new Parameter(AlfrescoNames.Headers.OverWrite, true, ParameterType.GetOrPost)));

            await CreateRules(fileNameWithoutExtension, createdScriptPidGenerator.Entry.Id, rules);
        }

        #endregion

        #region Nested Types, Enums, Delegates

        private class ConfigurationContent
        {
            #region Properties

            public string FileName { get; set; }
            public string FileNameNoExtensions => FileName.Replace(".js", "");
            public string FilePath { get; set; }
            public string RuleFolderPath { get; set; }
            public List<Rules> Rules { get; set; }
            public List<ReplaceTexts> Replaces { get; set; }

            #endregion
        }

        #endregion
    }
}