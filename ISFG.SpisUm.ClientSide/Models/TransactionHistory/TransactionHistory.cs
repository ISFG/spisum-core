using System;

namespace ISFG.SpisUm.ClientSide.Models.TransactionHistory
{
    public class TransactionHistory
    {
        #region Properties

        public string Description { get; set; }
        public TransactionHistoryParameters EventParameters { get; set; }
        public string EventType { get; set; }
        public string EventSource { get; set; }
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string NodeType { get; set; }
        public string SslNodeType { get; set; }
        public DateTime OccuredAt { get; set; }
        public string Pid { get; set; }
        public string UserId { get; set; }
        public string UserGroupId { get; set; }

        #endregion
    }
}