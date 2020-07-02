using System;

namespace ISFG.Exceptions.Exceptions
{
    public class ForbiddenException : Exception
    {
        #region Constructors

        public ForbiddenException(string code, string message)
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