namespace ISFG.Pdf.Models.Clause
{
    public class ClauseModel
    {
        #region Constructors

        public ClauseModel(params string[] paragraphs) => Paragraphs = paragraphs;

        #endregion

        #region Properties

        public string[] Paragraphs { get; set; }
        public string Seal { get; set; }
        public string SealTimestamp { get; set; }
        public string Sign { get; set; }
        public string SignTimestamp { get; set; }
        public string Timestamp { get; set; }
        public string OriginalFileFormat { get; set; }
        public string OriginalFileFormatValue { get; set; }
        public string FilePrint { get; set; }
        public string FilePrintValue { get; set; }
        public string UsedAlgorithm { get; set; }
        public string UsedAlgorithmValue { get; set; }
        public string Organizer { get; set; }
        public string OrganizerValue { get; set; }
        public string NameLastName { get; set; }
        public string NameLastNameValue { get; set; }
        public string DateOfIssue { get; set; }
        public string DateOfIssueValue { get; set; }

        #endregion
    }
}