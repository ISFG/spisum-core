using System;
using System.Collections.Generic;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.ClientSide.Models.TransactionHistory;

namespace ISFG.SpisUm.Models.V1
{
    public class HistoryResponseModel
    {
        #region Properties

        public HistoryListModel List { get; set; }

        #endregion
    }
    
    public class HistoryListModel
    {
        #region Properties

        public Pagination Pagination { get; set; }
        public IEnumerable<TransactionHistoryEntry> Entries { get; set; }

        #endregion
    }

    public class TransactionHistoryEntry
    {
        #region Properties

        public TransactionHistory Entry { get; set; }

        #endregion
    }

    public class HistoryEntryModel
    {
        #region Properties

        public HistoryEntryModelEntry Entry { get; set; }

        #endregion
    }

    public class HistoryEntryModelEntry
    {
        #region Properties

        public DateTime CreatedAt { get; set; }
        public Creator CreatedByUser { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }

        #endregion
    }

    public class Creator
    {
        #region Properties

        public string DisplayName { get; set; }
        public string Id { get; set; }

        #endregion
    }
}
