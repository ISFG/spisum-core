using System;

namespace ISFG.Exceptions.Exceptions
{
    public class NotAuthenticatedException : Exception
    {
        #region Constructors

        public NotAuthenticatedException(string code, string message)
        {
            Code = code;
            Message = message;
        }

        #endregion

        #region Properties

        public string Code { get; }
        public string Message { get; }

        #endregion
    }
}