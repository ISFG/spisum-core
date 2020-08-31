using System.Collections.Generic;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Common.Attributes;

namespace ISFG.Alfresco.Api.Configurations
{
    [Settings("Alfresco")]
    public class AlfrescoConfiguration : IAlfrescoConfiguration
    {
        #region Implementation of IAlfrescoConfiguration

        public ConfigurationFiles ConfigurationFiles { get; set; }
        public string Groups { get; set; }
        public string Roles { get; set; }
        public string ShreddingPlan { get; set; }
        public string SiteRM { get; set; }
        public string Sites { get; set; }
        public uint? TokenExpire { get; set; }
        public string Url { get; set; }
        public string Users { get; set; }

        #endregion
    }

    public class EmailConfiguration
    {
        #region Properties

        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }

        #endregion
    }

    #region Configuration Files
    public class ConfigurationFiles
    {
        #region Properties

        public string FolderName { get; set; }
        public ConfigurationFolder CodeLists { get; set; }
        public ConfigurationContentModelsFolder ContentModels { get; set; }
        public ConfigurationScripts Scripts { get; set; }

        #endregion
    }
    public class ConfigurationScripts
    {
        #region Properties

        public string FolderName { get; set; }
        public ScriptFile[] Files { get; set; }
        public List<string> DisableInheritanceRules { get; set; }

        #endregion
    }
    public class ScriptFile
    {
        #region Properties

        public string FileName { get; set; }
        public List<Rules> Rules { get; set; }
        public List<ReplaceTexts> Replaces { get; set; }

        #endregion
    }
    public class ConfigurationFolder
    {
        #region Properties

        public string FolderName { get; set; }

        #endregion
    }
    public class ConfigurationContentModelsFolder
    {
        #region Properties

        public string FolderName { get; set; }
        public string XSDValidationFile { get; set; }
        public string[] Files { get; set; }

        #endregion
    }
    public class ReplaceTexts
    {
        #region Properties

        public string ReplaceText { get; set; }
        public string ReplaceWithText { get; set; }

        #endregion
    }
    public class Rules
    {
        #region Properties

        public string RelativePath { get; set; }
        public List<string> RuleType { get; set; }

        #endregion
    }
    #endregion
}