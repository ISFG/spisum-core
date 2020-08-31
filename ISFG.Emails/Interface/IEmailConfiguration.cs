using ISFG.Emails.Models;

namespace ISFG.Emails.Interface
{
    public interface IEmailConfiguration
    {
        #region Properties

        public ServerConfiguration Pop3 { get; set; }
        public ServerConfiguration Stmp { get; set; }
        public string DisplayName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        #endregion
    }
}
