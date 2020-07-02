namespace ISFG.Alfresco.Api.Models.CoreApi.AuthApi
{
    public partial class TicketBody
    {
        #region Constructors

        public TicketBody()
        {
        }

        public TicketBody(string username, string password)
        {
            UserId = username;
            Password = password;
        }

        #endregion
    }
}