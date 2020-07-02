using System.Net;
using ISFG.Exceptions.Exceptions;
using ISFG.Exceptions.Models;
using Serilog;

namespace ISFG.Exceptions.Handlers
{
    internal class ForbiddenExceptionHandler : BaseExceptionHandler<ForbiddenException>
    {
        #region Override of BaseExceptionHandler<ForbiddenException>

        public override ExceptionModel HandleModel(ForbiddenException exception) => new ExceptionModel
        {
            Code = exception.Code,
            Message = exception.Message
        };

        public override void LogException(ForbiddenException exception) => 
            Log.Warning($"Code: {exception?.Code}, Message: {exception?.Message}");

        public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;

        #endregion
    }
}