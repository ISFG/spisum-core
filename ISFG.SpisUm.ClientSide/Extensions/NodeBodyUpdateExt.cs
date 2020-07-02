using System;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.ClientSide.Models;

namespace ISFG.SpisUm.ClientSide.Extensions
{
    public static class NodeBodyUpdateExt
    {
        #region Static Methods

        public static NodeBodyUpdate AddPermissions(this NodeBodyUpdate bodyUpdate, string prefix = null, string owner = null)
        {
            if (prefix != null && prefix != SpisumNames.Groups.SpisumAdmin) //(bodyUpdate?.Permissions?.LocallySet?.All(x => !x.AuthorityId.StartsWith(prefix)) ?? true)
                foreach (var permission in Enum.GetValues(typeof(GroupPermissionTypes)))
                    bodyUpdate.AddPermission($"{prefix}_{permission}", $"{permission}");

            if (owner != null && owner != SpisumNames.SystemUsers.Spisum && owner != AlfrescoNames.Aliases.Group) //(bodyUpdate?.Permissions?.LocallySet?.All(x => x.AuthorityId != $"{SpisumNames.Prefixes.UserGroup}{owner}") ?? true)
                bodyUpdate.AddPermission($"{SpisumNames.Prefixes.UserGroup}{owner}", $"{GroupPermissionTypes.Coordinator}");

            bodyUpdate.AddPermission(SpisumNames.Groups.SpisumAdmin, AlfrescoNames.Permissions.SiteManager);
            bodyUpdate.AddPermission(SpisumNames.Groups.DispatchGroup, AlfrescoNames.Permissions.Consumer);

            return bodyUpdate;
        }

        public static NodeBodyUpdate AddPermissionsWithoutPostfixes(this NodeBodyUpdate bodyUpdate, string prefix = null, string owner = null)
        {
            if (prefix != null && prefix != SpisumNames.Groups.SpisumAdmin)
                bodyUpdate.AddPermission($"{prefix}", $"{GroupPermissionTypes.Coordinator}");

            if (owner != null && owner != SpisumNames.SystemUsers.Spisum && owner != AlfrescoNames.Aliases.Group)
                bodyUpdate.AddPermission($"{SpisumNames.Prefixes.UserGroup}{owner}", $"{GroupPermissionTypes.Coordinator}");

            bodyUpdate.AddPermission(SpisumNames.Groups.SpisumAdmin, AlfrescoNames.Permissions.SiteManager);
            bodyUpdate.AddPermission(SpisumNames.Groups.DispatchGroup, AlfrescoNames.Permissions.Consumer);

            return bodyUpdate;
        }

        public static NodeBodyUpdate RemovePermissions(this NodeBodyUpdate bodyUpdate, string prefix = null, string owner = null)
        {
            if (prefix != null)
                foreach (var permission in Enum.GetValues(typeof(GroupPermissionTypes)))
                    bodyUpdate.RemovePermission($"{prefix}_{permission}");

            if (owner != null)
                bodyUpdate.RemovePermission($"{SpisumNames.Prefixes.UserGroup}{owner}");

            return bodyUpdate;
        }

        #endregion
    }
}