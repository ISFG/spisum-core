using FluentValidation.Results;

namespace ISFG.Exceptions.Models
{
    internal class ValidationExceptionModel : ExceptionModel
    {
        #region Properties

        public ValidationResult Result { get; set; }

        #endregion
    }
}