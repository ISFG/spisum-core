using System;
using System.Net;
using ISFG.Exceptions.Interfaces;
using ISFG.Exceptions.Models;
using Serilog;

namespace ISFG.Exceptions.Handlers
{
    internal abstract class BaseExceptionHandler<T> : IExceptionHandler<T> where T : Exception
    {
        #region Implementation of IExceptionHandler

        public ExceptionModel HandleModel(Exception exception) => HandleModel((T) exception);

        public void LogException(Exception exception) => LogException((T) exception);

        #endregion

        #region Implementation of IExceptionHandler<T>

        public virtual ExceptionModel HandleModel(T exception) =>
            new ExceptionModel
            {
                Message = exception.Message,
                Type = exception.GetType()?.FullName,
                Stack = exception.StackTrace
            };

        public virtual void LogException(T exception) =>
            Log.Error($"Type:{exception.GetType()?.FullName}, Message:{exception.Message}, StackTrace:{exception.StackTrace}, Inner:[{exception.InnerException?.Message}]");

        public virtual HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;

        #endregion
    }
}