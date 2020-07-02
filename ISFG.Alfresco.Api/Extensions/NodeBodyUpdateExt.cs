using System.Collections.Generic;
using System.Linq;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;

namespace ISFG.Alfresco.Api.Extensions
{
    public static class NodeBodyUpdateExt
    {
        #region Static Methods

        public static NodeBodyUpdate AddPermission(
            this NodeBodyUpdate nodeBodyUpdate, 
            string authority, 
            string name,
            PermissionElementAccessStatus permissionStatus = PermissionElementAccessStatus.ALLOWED)
        {
            if (nodeBodyUpdate.Permissions == null)
                nodeBodyUpdate.Permissions = new PermissionsBody { IsInheritanceEnabled = false };
            
            if (nodeBodyUpdate.Permissions.LocallySet == null)
                nodeBodyUpdate.Permissions.LocallySet = new List<PermissionElement>();

            var locallySet = nodeBodyUpdate.Permissions.LocallySet.ToList();
            var existingPermission = locallySet.Find(x => x.AuthorityId == authority && x.Name == name);

            if (existingPermission != null)
                existingPermission.AccessStatus = permissionStatus;
            else
                locallySet.Add(new PermissionElement
                {
                    AuthorityId = authority,
                    Name = name,
                    AccessStatus = permissionStatus
                });

            nodeBodyUpdate.Permissions.LocallySet = locallySet;

            return nodeBodyUpdate;
        }

        public static NodeBodyUpdate RemovePermission(this NodeBodyUpdate nodeBodyUpdate, string authority)
        {
            if (nodeBodyUpdate.Permissions == null)
                nodeBodyUpdate.Permissions = new PermissionsBody();
            
            if (nodeBodyUpdate.Permissions.LocallySet == null)
                nodeBodyUpdate.Permissions.LocallySet = new List<PermissionElement>();
            
            if (nodeBodyUpdate.Permissions.LocallySet is List<PermissionElement> permissions)
                permissions.RemoveAll(x => x.AuthorityId == authority);

            return nodeBodyUpdate;
        }

        public static NodeBodyUpdate AddProperty(this NodeBodyUpdate nodeBodyUpdate, string key, object value)
        {
            if (nodeBodyUpdate.Properties == null)
                nodeBodyUpdate.Properties = new Dictionary<string, object>();
            
            nodeBodyUpdate.Properties.As<Dictionary<string, object>>()
                .Add(key, value);
                
            return nodeBodyUpdate;
        }

        public static NodeBodyUpdate AddPropertyIfNotNull(this NodeBodyUpdate nodeBodyUpdate, string key, object value)
        {
            return value == null ? nodeBodyUpdate : nodeBodyUpdate.AddProperty(key, value);
        }

        public static NodeBodyUpdate SetPermissionInheritance(this NodeBodyUpdate nodeBodyUpdate, bool isInheritanceEnabled = false)
        {
            if (nodeBodyUpdate.Permissions == null)
                nodeBodyUpdate.Permissions = new PermissionsBody();

            nodeBodyUpdate.Permissions.IsInheritanceEnabled = isInheritanceEnabled;
            
            return nodeBodyUpdate;
        }

        #endregion
    }
}