namespace ISFG.SpisUm.ClientSide.Models
{
    public class Authorization
    {
        #region Properties

        public string User { get; set; }
        public string Token { get; set; }
        public string AuthorizationToken { get; set; }
        public int Expire { get; set; }
        public bool IsAdmin { get; set; }
        public bool Signer { get; set; }

        #endregion
    }
}