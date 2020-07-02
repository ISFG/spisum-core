using System.Collections.Generic;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.Site;

namespace ISFG.Alfresco.Api.Models.Sites
{
    public class SiteARM: AlfrescoRequest<SiteQueryParamsARM>
    {
        #region Properties

        public SiteBodyCreate Body { get; set; }
        public List<ChildrenARM<NodeBodyCreate, NodeBodyCreate>> Childs { get; set; }
        public bool? GroupStructure { get; set; }
        public List<Permission> Permissions { get; set; }

        #endregion
    }

    public class Permission
    {
        #region Properties

        public string Id { get; set; }
        public string Role { get; set; }

        #endregion
    }

    public class SiteQueryParamsARM
    {
        #region Properties

        public List<string> fields { get; set; }
        public bool skipAddToFavorites { get; set; } = false;
        public bool skipConfiguration { get; set; } = false;

        #endregion
    }
}
