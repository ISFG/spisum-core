namespace ISFG.Emails.Models
{
    public class Attachment
    {
        #region Constructors

        public Attachment()
        {
        }

        public Attachment(string fileName, byte[] file)
        {
            FileName = fileName;
            File = file;
        }

        #endregion

        #region Properties

        public string FileName { get; set; }
        public byte[] File { get; set; }

        #endregion
    }
}
