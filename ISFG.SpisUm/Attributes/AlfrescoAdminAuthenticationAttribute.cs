using ISFG.SpisUm.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Attributes
{
    public class AlfrescoAdminAuthenticationAttribute : ServiceFilterAttribute
    {
        #region Constructors

        public AlfrescoAdminAuthenticationAttribute() : base(typeof(AlfrescoAdminAuthenticateFilter))
        {
        }

        #endregion
    }
}