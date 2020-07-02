using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.SpisUm.ClientSide.Models;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IAuditLogService
    {
        #region Public Methods

        Task<TransactionHistoryPage<TransactionHistory>> GetEvents(TransactionHistoryQuery transactionHistoryQuery);
        Task Record(string nodeId, string nodeType, string pid, string nodeTypeCode, string eventType, string message = null);
        Task Record(string nodeId, string nodeType, string pid, string nodeTypeCode, string eventType, Dictionary<string, object> a, Dictionary<string, object> b, string message = null);

        #endregion
    }
}