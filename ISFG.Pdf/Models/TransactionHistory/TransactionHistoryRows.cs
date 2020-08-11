namespace ISFG.Pdf.Models.TransactionHistory
{
    public class TransactionHistoryRows
    {
        #region Properties

        public string Pid { get; set; }
        public string TypeOfObject { get; set; }
        public string TypeOfChanges { get; set; }
        public string Descriptions { get; set; }
        public string Author { get; set; }
        public string Date { get; set; }

        #endregion
    }
}