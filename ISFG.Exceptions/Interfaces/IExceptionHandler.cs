using System;
using System.Net;
using ISFG.Exceptions.Models;

namespace ISFG.Exceptions.Interfaces
{
    internal interface IExceptionHandler
    {
        #region Properties

        HttpStatusCode StatusCode { get; }

        #endregion

        #region Public Methods

        ExceptionModel HandleModel(Exception exception);

        void LogException(Exception exception);

        #endregion
    }

    internal interface IExceptionHandler<in T> : IExceptionHandler where T : Exception
    {
        #region Properties

        HttpStatusCode StatusCode { get; }

        #endregion

        #region Public Methods

        ExceptionModel HandleModel(T exception);

        void LogException(T exception);

        #endregion
    }
}