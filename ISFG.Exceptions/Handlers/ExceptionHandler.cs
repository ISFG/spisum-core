using System;
using System.Net;

namespace ISFG.Exceptions.Handlers
{
    internal class ExceptionHandler : BaseExceptionHandler<Exception>
    {
        #region Override of BaseExceptionHandler<Exception>

        public override HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;

        #endregion
    }
}