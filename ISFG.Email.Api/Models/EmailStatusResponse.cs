namespace ISFG.Email.Api.Models
{
    public class EmailAccount
    {
        #region Properties

        public string Name { get; set; }
        public string Username { get; set; }

        #endregion
    }

    public class EmailStatusResponse
    {
        #region Properties

        public bool Running { get; set; }
        public int NewMessageCount { get; set; }

        #endregion
    }
}