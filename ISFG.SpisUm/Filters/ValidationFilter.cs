using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using ISFG.Exceptions.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ISFG.SpisUm.Filters
{
    public class ValidationFilter : IActionFilter
    {
        #region Implementation of IActionFilter

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ModelState.IsValid) return;
            
            IEnumerable<ValidationFailure> errors = context.ModelState
                .Where(kvp => kvp.Value.ValidationState == ModelValidationState.Invalid)
                .SelectMany(kvp => kvp.Value.Errors.Select(e => new ValidationFailure(kvp.Key, e.ErrorMessage, kvp.Value.AttemptedValue)));
            
            throw new BadRequestException(new ValidationResult(errors));
        }

        #endregion
    }
}