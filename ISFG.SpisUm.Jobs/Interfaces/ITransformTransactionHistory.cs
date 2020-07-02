using System;
using System.Threading.Tasks;
using ISFG.Pdf.Models;

namespace ISFG.SpisUm.Jobs.Interfaces
{
    public interface ITransformTransactionHistory
    {
        #region Public Methods

        Task<TransactionHistoryPdf> ToPdfModel(DateTime currentDate);

        #endregion
    }
}