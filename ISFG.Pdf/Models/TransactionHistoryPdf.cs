using System.Collections.Generic;

namespace ISFG.Pdf.Models
{
    public class TransactionHistoryPdf
    {
        #region Properties

        public string Header { get; set; }
        public string Name { get; set; }
        public string Originator { get; set; }
        public string Address { get; set; }
        public string SerialNumber { get; set; }
        public string NumberOfPages { get; set; }
        public TableRows Rows { get; set; }
        public List<TableColumns> Columns { get; set; }

        #endregion
    }
}