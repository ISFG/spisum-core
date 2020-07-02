using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISFG.SpisUm.ClientSide.Interfaces;

namespace ISFG.SpisUm.ClientSide.Extensions
{
    public static class NodeServiceExt
    {
        #region Static Methods

        public static async Task<List<string>> GetChildrenIds(this INodesService nodeService, string nodeId) =>
            (await nodeService.GetChildren(nodeId)).Select(x => x.Entry.Id).ToList();

        #endregion
    }
}