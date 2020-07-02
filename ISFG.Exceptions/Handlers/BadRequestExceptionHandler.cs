using System.Net;
using ISFG.Exceptions.Exceptions;
using ISFG.Exceptions.Models;
using Newtonsoft.Json;
using Serilog;

namespace ISFG.Exceptions.Handlers
{
    internal class BadRequestExceptionHandler : BaseExceptionHandler<BadRequestException>
    {
        #region Override of BaseExceptionHandler<BadRequestException>

        public override ExceptionModel HandleModel(BadRequestException exception) => new ValidationExceptionModel
        {
            Code = exception.Code,
            Message = exception.Message,
            Result = exception.Result
        };

        public override void LogException(BadRequestException exception)
        {
            Log.Debug(exception.Result != null
                ? $"{JsonConvert.SerializeObject(exception.Result)}"
                : $"Code: {exception?.Code}, Message: {exception?.Message}, StackTrace:{exception?.StackTrace}");
        }

        public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

        #endregion
    }
}