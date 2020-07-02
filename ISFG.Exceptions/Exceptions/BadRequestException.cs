using System;
using FluentValidation.Results;

namespace ISFG.Exceptions.Exceptions
{
    public class BadRequestException : Exception
    {
        #region Constructors

        public BadRequestException(ValidationResult result, string code = null, string message = null)
        {
            Result = result;
            Message = message;
            Code = code;
        }

        public BadRequestException(string code, string message = null)
        {
            Code = code;
            Message = message;
        }

        #endregion

        #region Properties

        public string Code { get; }
        public string Message { get; }
        public ValidationResult Result { get; }

        #endregion
    }
}