using System;
using System.Net;

namespace ISFG.Exceptions.Handlers
{
    internal class NotSupportedExceptionHandler : BaseExceptionHandler<NotSupportedException>
    {
        #region Override of BaseExceptionHandler<NotSupportedException>

        public override HttpStatusCode StatusCode => HttpStatusCode.NotImplemented;

        #endregion
    }
}