using System;
using System.Linq;
using FluentValidation;

namespace ISFG.SpisUm.ClientSide.Extensions
{
    public static class ValidatorExt
    {
        #region Static Methods

        public static IRuleBuilderOptions<T, TProperty> Contains<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, params TProperty[] validOptions)
        {
            string formatted;
            if (validOptions == null || validOptions.Length == 0)
                throw new ArgumentException("At least one valid option is expected", nameof(validOptions));

            formatted = validOptions.Length == 1 ? 
                validOptions[0].ToString() : 
                $"{string.Join(", ", validOptions.Select(vo => vo.ToString()).ToArray(), 0, validOptions.Length - 1)} or {validOptions.Last()}";

            return ruleBuilder
                .Must(validOptions.Contains)
                .WithMessage($"{{PropertyName}} must be one of these values: {formatted}");
        }

        #endregion
    }
}