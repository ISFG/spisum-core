using ISFG.Emails.Interface;

namespace ISFG.Emails.Models
{
    public class EmailConfiguration : IEmailConfiguration
    {
        #region Implementation of IEmailConfiguration

        public string DisplayName { get; set; }
        public string Password { get; set; }
        public ServerConfiguration Pop3 { get; set; }
        public ServerConfiguration Stmp { get; set; }
        public string Username { get; set; }

        #endregion
    }
}
