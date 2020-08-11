using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Pdf.Models.Clause;
using ISFG.Pdf.Models.ShreddingPlan;

namespace ISFG.Pdf.Interfaces
{
    public interface ITransformService
    {
        #region Public Methods

        Task<ClauseModel> Clause(NodeEntry nodeEntry, byte[] pdf);
        Task<ShreddingPlan> ShreddingPlan(string id);

        #endregion
    }
}