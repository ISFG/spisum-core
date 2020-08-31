using ISFG.Emails.Interface;

namespace ISFG.Emails.Models
{
    public class ServerConfiguration : IServerConfiguration
    {
        #region Implementation of IServerConfiguration

        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSSL { get; set; }

        #endregion
    }
}
