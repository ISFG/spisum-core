using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EntityFrameworkPaginateCore;
using ISFG.Data.Database;
using ISFG.Data.Interfaces;
using ISFG.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ISFG.Data.Repositories
{
    public class TransactionHistoryRepository : ITransactionHistoryRepository
    {
        #region Fields

        private readonly SpisumContext _spisumContext;

        #endregion

        #region Constructors

        public TransactionHistoryRepository(SpisumContext spisumContext) => _spisumContext = spisumContext;

        #endregion

        #region Implementation of ITransactionHistoryRepository

        public async Task<TransactionHistory> CreateEvent(string nodeId, string nodeType, string pid, string nodeTypeCode, string eventType, string userId, string userGroup, string software, string eventParameters)
        {
            var transactionHistoryModel = new TransactionHistory
            {
                NodeId = nodeId,
                SslNodeType = nodeType,
                Pid = pid,
                OccuredAt = DateTime.Now.ToUniversalTime(),
                UserId = userId,
                UserGroupId = userGroup,
                EventSource = software,
                EventParameters = eventParameters,
                FkNodeTypeCode = nodeTypeCode,
                FkEventTypeCode = eventType
            };
            
            _spisumContext.TransactionHistory.Add(transactionHistoryModel);
            await _spisumContext.SaveChangesAsync();

            return transactionHistoryModel;
        }

        public async Task<EventType> GetEventType(string code)
        {
            return await _spisumContext.EventType
                .FirstOrDefaultAsync(x => x.Code == code);
        }

        public async Task<NodeType> GetNodeType(string code)
        {
            return await _spisumContext.NodeType
                .FirstOrDefaultAsync(x => x.Code == code);
        }

        public async Task<Page<TransactionHistory>> GetTransactionHistory(int pageNumber, int pageSize, Sorts<TransactionHistory> sorts, Filters<TransactionHistory> filters)
        {
            return await (from e in _spisumContext.TransactionHistory
                    join nodeType in _spisumContext.NodeType
                        on e.FkNodeTypeCode equals nodeType.Code
                    join eventType in _spisumContext.EventType
                        on e.FkEventTypeCode equals eventType.Code
                    select new TransactionHistory
                    {
                        Id = e.Id,
                        NodeId = e.NodeId,
                        SslNodeType = e.SslNodeType,
                        Pid = e.Pid,
                        FkNodeTypeCodeNavigation = nodeType,
                        OccuredAt = e.OccuredAt,
                        UserId = e.UserId,
                        UserGroupId = e.UserGroupId,
                        FkEventTypeCodeNavigation = eventType,
                        EventParameters = e.EventParameters,
                        EventSource = e.EventSource,
                        RowHash = e.RowHash,
                        ProcessedAt = e.ProcessedAt,
                        ProcessedBy = e.ProcessedBy
                    })
                .AsNoTracking()
                .PaginateAsync(pageNumber, pageSize, sorts, filters);
        }

        public async Task<List<TransactionHistory>> GetTransactionHistoryByDate(DateTime from, DateTime to)
        {
            return await (from e in _spisumContext.TransactionHistory
                    join nodeType in _spisumContext.NodeType
                        on e.FkNodeTypeCode equals nodeType.Code
                    join eventType in _spisumContext.EventType
                        on e.FkEventTypeCode equals eventType.Code
                    select new TransactionHistory
                    {
                        Id = e.Id,
                        NodeId = e.NodeId,
                        SslNodeType = e.SslNodeType,
                        Pid = e.Pid,
                        FkNodeTypeCodeNavigation = nodeType,
                        OccuredAt = e.OccuredAt,
                        UserId = e.UserId,
                        UserGroupId = e.UserGroupId,
                        FkEventTypeCodeNavigation = eventType,
                        EventParameters = e.EventParameters,
                        EventSource = e.EventSource,
                        RowHash = e.RowHash,
                        ProcessedAt = e.ProcessedAt,
                        ProcessedBy = e.ProcessedBy
                    })
                .Where(x => x.OccuredAt.Date >= from && x.OccuredAt.Date <= to)
                .AsNoTracking()
                .ToListAsync();
        }

        #endregion
    }
}

/* // Old Solution
public async Task<List<Event>> GetEvents(EventFilter eventFilter)
{
    IQueryable<Event> query;
    
    if (eventFilter.NodeId != null)
        query = _auditContext.Event.Where(x => x.FkIdentity.NodeId == eventFilter.NodeId);
    else if (eventFilter.NodeIds != null && eventFilter.NodeIds.Any())
        query = _auditContext.Event.Where(x => eventFilter.NodeIds.Contains(x.FkIdentity.NodeId));
    else
        query = _auditContext.Event;

    if (eventFilter.From != null)
        query = query.Where(x => x.OccuredAt > eventFilter.From);
    if (eventFilter.To != null)
        query = query.Where(x => x.OccuredAt < eventFilter.To);
    if (eventFilter.Skip != null)
        query = query.Skip(eventFilter.Skip.Value);
    if (eventFilter.Take != null)
        query = query.Take(eventFilter.Take.Value);

    return await query.Select(x => x).ToListAsync();
}
*/