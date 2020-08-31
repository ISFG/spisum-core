using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.SpisUm.ClientSide.Models;
using Newtonsoft.Json.Linq;

namespace ISFG.SpisUm.ClientSide.Extensions
{
    public static class NodeEntryExt
    {
        #region Static Methods

        public static string GetPid(this NodeEntry nodeEntry)
        {
            try
            {
                var properties = nodeEntry.Entry.Properties.As<JObject>().ToDictionary();
                return properties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion
    }
}
