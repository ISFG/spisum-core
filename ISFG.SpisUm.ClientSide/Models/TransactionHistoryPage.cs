using System.Collections.Generic;

namespace ISFG.SpisUm.ClientSide.Models
{
    public class TransactionHistoryPage<T>
    {
        #region Properties

        public IEnumerable<T> Results { get; set; }

        public int CurrentPage { get; set; }

        public int PageCount { get; set; }

        public int PageSize { get; set; }

        public int RecordCount { get; set; }

        #endregion
    }
}
