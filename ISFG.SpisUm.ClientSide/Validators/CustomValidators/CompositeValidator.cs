using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;

namespace ISFG.SpisUm.ClientSide.Validators.CustomValidators
{
    public abstract class CompositeValidator<T> : AbstractValidator<T> 
    {
        #region Fields

        private readonly List<IValidator> _otherValidators = new List<IValidator>();

        #endregion

        #region Override of AbstractValidator<T>

        public override ValidationResult Validate(ValidationContext<T> context) 
        {
            var mainErrors = base.Validate(context).Errors;
            var errorsFromOtherValidators = _otherValidators.SelectMany(x => x.Validate(context).Errors);
            var combinedErrors = mainErrors.Concat(errorsFromOtherValidators);
 
            return new ValidationResult(combinedErrors);
        }

        #endregion

        #region Protected Methods

        protected void RegisterBaseValidator<TBase>(IValidator<TBase> validator) 
        {
            if(validator.CanValidateInstancesOfType(typeof(T)))
                _otherValidators.Add(validator);
            else
                throw new NotSupportedException($"Type {typeof(TBase).Name} is not a base-class or interface implemented by {typeof(T).Name}.");
        }

        #endregion
    }
}
