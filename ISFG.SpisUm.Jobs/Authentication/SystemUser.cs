using ISFG.Common.Interfaces;

namespace ISFG.SpisUm.Jobs.Authentication
{
    public class SystemUser : IIdentityUser
    {
        #region Implementation of IIdentityUser

        public string FirstName => "SpisUm System";
        public string Group { get; }
        public string Id => "system";
        public bool IsAdmin => true;
        public string Job { get; set; }
        public string LastName => "";
        public string OrganizationAddress { get; set; }
        public string OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string OrganizationUnit { get; set; }
        public string OrganizationUserId { get; set; }
        public string RequestGroup => "";
        public string Token { get; set; }

        #endregion
    }
}