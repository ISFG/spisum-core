namespace ISFG.Common.Interfaces
{
    public interface IHttpUserContextService
    {
        #region Properties

        IIdentityUser Current { get; }

        #endregion
    }
}