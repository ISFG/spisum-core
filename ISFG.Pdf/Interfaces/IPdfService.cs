using System.IO;
using System.Threading.Tasks;
using ISFG.Pdf.Models;

namespace ISFG.Pdf.Interfaces
{
    public interface IPdfService
    {
        #region Public Methods

        Task<byte[]> ConvertToPdfA2B(MemoryStream input);
        Task<byte[]> GenerateTransactionHistory(TransactionHistoryPdf transactionHistoryPdf);
        Task<bool> IsPdfA2B(MemoryStream input);

        #endregion
    }
}