namespace ISFG.Emails.Interface
{
    public interface IServerConfiguration
    {
        #region Properties

        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSSL { get; set; }

        #endregion
    }
}
