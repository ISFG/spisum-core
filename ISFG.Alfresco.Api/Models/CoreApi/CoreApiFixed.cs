using System.Collections.Generic;

namespace ISFG.Alfresco.Api.Models.CoreApi.CoreApi
{
    public partial class NodeBodyMove 
    {
        #region Constructors

        public NodeBodyMove()
        {
        }

        public NodeBodyMove(string targetParentId) => TargetParentId = targetParentId;

        #endregion
    }

    public partial class NodeBodyUpdate
    {
        #region Constructors

        #endregion
    }
    public class NodeBodyUpdateFixed : NodeBodyUpdate
    {
        #region Constructors

        public NodeBodyUpdateFixed()
        {
            if (Properties == null)
                Properties = new Dictionary<string, object>();
            if (Permissions == null)
                Permissions = new PermissionsBody();
            if (Permissions.LocallySet == null)
                Permissions.LocallySet = new List<PermissionElement>();
        }

        #endregion
    }
}