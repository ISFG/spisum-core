using System.IO;
using System.Threading.Tasks;
using ISFG.Pdf.Models.Clause;
using ISFG.Pdf.Models.ShreddingPlan;
using ISFG.Pdf.Models.TransactionHistory;

namespace ISFG.Pdf.Interfaces
{
    public interface IPdfService
    {
        #region Public Methods

        Task<byte[]> AddClause(MemoryStream input, ClauseModel clauseModel);
        Task<byte[]> ConvertToPdfA2B(MemoryStream input);
        Task<byte[]> GenerateShreddingPlan(ShreddingPlan shreddingPlan);
        Task<byte[]> GenerateTransactionHistory(TransactionHistoryModel transactionHistoryModel);
        Task<bool> IsPdfA2B(MemoryStream input);

        #endregion
    }
}