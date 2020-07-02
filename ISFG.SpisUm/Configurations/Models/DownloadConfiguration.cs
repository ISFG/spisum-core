namespace ISFG.SpisUm.Configurations.Models
{
    public class DownloadConfiguration
    {
        #region Properties

        public int CheckStatusDelayInSeconds { get; set; }
        public int StopDownloadAfterNumberOfRequest { get; set; }

        #endregion
    }
}
