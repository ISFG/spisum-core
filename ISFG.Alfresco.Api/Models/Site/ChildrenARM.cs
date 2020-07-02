using System.Collections.Generic;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.Sites;

namespace ISFG.Alfresco.Api.Models.Site
{
    public class ChildrenARM<T, U> : AlfrescoRequest<ChildrenQueryParamsARM>
    {
        #region Properties

        public T Body { get; set; }
        public List<ChildrenARM<U, U>> Childs { get; set; }
        public List<Permission> Permissions { get; set; }
        public bool? ReturnInNodesInfo { get; set; }

        #endregion
    }

    public class ChildrenQueryParamsARM
    {
        #region Properties

        public bool AutoRename { get; set; }
        public List<string> Include { get; set; }
        public List<string> Fields { get; set; }

        #endregion
    }
}
