using System.Net;
using ISFG.Exceptions.Exceptions;
using ISFG.Exceptions.Models;
using Serilog;

namespace ISFG.Exceptions.Handlers
{
    internal class UnauthorizedAccessExceptionHandler : BaseExceptionHandler<NotAuthenticatedException>
    {
        #region Override of BaseExceptionHandler<NotAuthenticatedException>

        public override ExceptionModel HandleModel(NotAuthenticatedException exception) => new ExceptionModel
        {
            Code = exception.Code,
            Message = exception.Message
        };

        public override void LogException(NotAuthenticatedException exception) => 
            Log.Warning($"Code: {exception?.Code}, Message: {exception?.Message}");

        public override HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;

        #endregion
    }
}