using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IShreddingService
    {
        #region Public Methods

        Task<NodeEntry> ShreddingProposalCreate(string name, List<string> ids);

        #endregion
    }
}
