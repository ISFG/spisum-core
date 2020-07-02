using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EntityFrameworkPaginateCore;
using ISFG.Data.Models;

namespace ISFG.Data.Interfaces
{
    public interface ITransactionHistoryRepository
    {
        #region Public Methods

        Task<TransactionHistory> CreateEvent(string nodeId, string nodeType, string pid, string nodeTypeNode, string eventType, string userId, string userGroup, string software, string eventParameters);
        Task<EventType> GetEventType(string code);
        Task<NodeType> GetNodeType(string code);
        Task<Page<TransactionHistory>> GetTransactionHistory(int pageNumber, int pageSize, Sorts<TransactionHistory> sorts, Filters<TransactionHistory> filters);
        Task<List<TransactionHistory>> GetTransactionHistoryByDate(DateTime from, DateTime to);

        #endregion
    }
}