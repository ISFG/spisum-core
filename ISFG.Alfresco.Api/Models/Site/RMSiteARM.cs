using System.Collections.Generic;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.GsApi.GsApi;
using ISFG.Alfresco.Api.Models.Site;

namespace ISFG.Alfresco.Api.Models.Sites
{
    public class RMSiteARM : AlfrescoRequest<RMSiteQueryParamsARM>
    {
        #region Properties

        public RMSiteBodyCreate Body { get; set; }
        public List<ChildrenARM<RootCategoryBodyCreate, RMNodeBodyCreateWithRelativePath>> Childs { get; set; }
        public List<Permission> Permissions { get; set; }

        #endregion
    }

    public class RMSiteQueryParamsARM
    {
        #region Properties

        public bool SkipAddToFavorites { get; set; } = true;

        #endregion
    }
}
