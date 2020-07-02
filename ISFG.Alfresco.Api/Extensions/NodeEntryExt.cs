using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using Newtonsoft.Json.Linq;

namespace ISFG.Alfresco.Api.Extensions
{
    public static class NodeEntryExt
    {
        #region Static Methods

        public static bool IsDocument(this NodeEntry nodeEntry)
        {
            return nodeEntry.Entry.NodeType == "ssl:document";
        }

        public static bool IsFile(this NodeEntry nodeEntry)
        {
            return nodeEntry.Entry.NodeType == "ssl:file";
        }

        public static bool IsDocumentOrFile(this NodeEntry nodeEntry)
        {
            return nodeEntry.Entry.NodeType == "ssl:file" || nodeEntry.Entry.NodeType == "ssl:document";
        }

        public static bool IsUserOwner(this NodeEntry nodeEntry, string currentUser)
        {
            var properties = nodeEntry.Entry.Properties.As<JObject>().ToDictionary();
            var contentOwner = properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString();
            
            return  contentOwner == currentUser || nodeEntry.Entry.CreatedByUser.Id == currentUser;
        }

        #endregion
    }
}