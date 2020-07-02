using System;

namespace ISFG.Data.Models
{
    public class TransactionHistory
    {
        #region Properties

        public long Id { get; set; }
        public string NodeId { get; set; }
        public string SslNodeType { get; set; }
        public string Pid { get; set; }
        public string FkNodeTypeCode { get; set; }
        public DateTime OccuredAt { get; set; }
        public string UserId { get; set; }
        public string UserGroupId { get; set; }
        public string FkEventTypeCode { get; set; }
        public string EventParameters { get; set; }
        public string EventSource { get; set; }
        public string RowHash { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string ProcessedBy { get; set; }

        public virtual EventType FkEventTypeCodeNavigation { get; set; }
        public virtual NodeType FkNodeTypeCodeNavigation { get; set; }

        #endregion
    }
}
