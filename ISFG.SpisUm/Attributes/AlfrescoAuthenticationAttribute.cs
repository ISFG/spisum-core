using ISFG.SpisUm.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Attributes
{
    public class AlfrescoAuthenticationAttribute : ServiceFilterAttribute
    {
        #region Constructors

        public AlfrescoAuthenticationAttribute() : base(typeof(AlfrescoAuthenticateFilter))
        {
        }

        #endregion
    }
}