using System.Collections.Generic;

namespace ISFG.Pdf.Models.TransactionHistory
{
    public class TransactionHistoryModel
    {
        #region Properties

        public string Header { get; set; }
        public string Name { get; set; }
        public string Originator { get; set; }
        public string Address { get; set; }
        public string SerialNumber { get; set; }
        public string NumberOfPages { get; set; }
        public TransactionHistoryRows Rows { get; set; }
        public List<TransactionHistoryColumns> Columns { get; set; }

        #endregion
    }
}