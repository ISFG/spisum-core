using ISFG.Alfresco.Api.Configurations;

namespace ISFG.Alfresco.Api.Interfaces
{
    public interface IAlfrescoConfiguration
    {
        #region Properties

        ConfigurationFiles ConfigurationFiles { get; }
        string Groups { get; }
        string Roles { get; }
        string ShreddingPlan { get; }
        string SiteRM { get; }
        string Sites { get; }
        uint? TokenExpire { get; }
        string Url { get; }
        string Users { get; }

        #endregion
    }
}