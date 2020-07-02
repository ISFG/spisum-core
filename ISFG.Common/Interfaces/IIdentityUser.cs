namespace ISFG.Common.Interfaces
{
    public interface IIdentityUser
    {
        #region Properties

        string Id { get; }
        string FirstName { get; }
        string LastName { get; }
        string Token { get; set; }
        string Group { get; }
        string RequestGroup { get; }
        bool IsAdmin { get; }
        string OrganizationUserId { get; }
        string OrganizationId { get; }
        string OrganizationName { get; }
        string OrganizationUnit { get; }
        string OrganizationAddress { get; }
        string Job { get; }

        #endregion
    }
}