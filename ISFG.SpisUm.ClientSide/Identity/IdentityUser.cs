using ISFG.Common.Interfaces;

namespace ISFG.SpisUm.ClientSide.Identity
{
    public class IdentityUser : IIdentityUser
    {
        #region Implementation of IIdentityUser

        public string FirstName { get; set; }
        public string Group { get; set; }

        public string Id { get; set; }
        public bool IsAdmin { get; set; }
        public string Job { get; set; }
        public string LastName { get; set; }
        public string OrganizationAddress { get; set; }
        public string OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string OrganizationUnit { get; set; }
        public string OrganizationUserId { get; set; }
        public string RequestGroup { get; set; }
        public string Token { get; set; }

        #endregion
    }
}