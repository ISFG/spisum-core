using ISFG.Common.Interfaces;
using ISFG.SpisUm.Jobs.Authentication;

namespace ISFG.SpisUm.Jobs.Services
{
    public class SystemUserContextService : IHttpUserContextService
    {
        #region Implementation of IHttpUserContextService

        public IIdentityUser Current { get; } = new SystemUser();

        #endregion
    }
}