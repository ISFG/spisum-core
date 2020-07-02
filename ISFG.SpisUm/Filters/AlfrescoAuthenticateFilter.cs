using System.Net;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ISFG.SpisUm.Filters
{
    public class AlfrescoAuthenticateFilter : IActionFilter
    {
        #region Implementation of IActionFilter

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User?.Identity?.IsAuthenticated == true && 
                context.HttpContext.User?.Identity?.AuthenticationType == AlfrescoIdentityTypes.AlfrescoIdentity)
                return;
            if (context.HttpContext.User?.Identity?.IsAuthenticated == true)
                throw new ForbiddenException(HttpStatusCode.Forbidden.ToString(), "Forbidden action for this user");
            
            throw new NotAuthenticatedException(HttpStatusCode.Unauthorized.ToString(), "Bad authentication token");
        }

        #endregion
    }
}