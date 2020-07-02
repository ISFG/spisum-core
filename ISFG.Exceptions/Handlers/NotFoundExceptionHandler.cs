using System.Net;
using ISFG.Exceptions.Exceptions;
using ISFG.Exceptions.Models;
using Serilog;

namespace ISFG.Exceptions.Handlers
{
    internal class NotFoundExceptionHandler : BaseExceptionHandler<NotFoundException>
    {
        #region Override of BaseExceptionHandler<NotFoundException>

        public override ExceptionModel HandleModel(NotFoundException exception) => new ExceptionModel
        {
            Code = exception.Key.ToString(),
            Message = exception.Message
        };

        public override void LogException(NotFoundException exception) => 
            Log.Debug($"Key: {exception?.Key}, Entity: {exception?.EntityType}, StackTrace:{exception?.StackTrace}");

        public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;

        #endregion
    }
}