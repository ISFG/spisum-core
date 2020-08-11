using System;
using System.Threading.Tasks;
using ISFG.Pdf.Models;
using ISFG.Pdf.Models.TransactionHistory;

namespace ISFG.SpisUm.Jobs.Interfaces
{
    public interface ITransformTransactionHistory
    {
        #region Public Methods

        Task<TransactionHistoryModel> ToPdfModel(DateTime currentDate);

        #endregion
    }
}