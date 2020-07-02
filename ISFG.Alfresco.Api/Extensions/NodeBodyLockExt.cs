using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace ISFG.Alfresco.Api.Extensions
{
    public static class NodeBodyLockExt
    {
        #region Static Methods

        public static NodeBodyLock AddLockType(this NodeBodyLock nodeBodyLock, NodeBodyLockType lockType)
        {
            nodeBodyLock.Type = lockType;
            
            return nodeBodyLock;
        }

        #endregion
    }
}