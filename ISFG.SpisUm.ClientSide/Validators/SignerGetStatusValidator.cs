using System.Linq;
using FluentValidation;
using ISFG.Exceptions.Exceptions;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class SignerGetStatusValidator  : AbstractValidator<SignerGetStatus>
    {
        #region Constructors

        public SignerGetStatusValidator()
        {
            RuleFor(x => x)
                .Must(x => x.ComponentId != null && x.ComponentId.All(x => !string.IsNullOrEmpty(x)))
                .When(x => x != null)
                .OnAnyFailure(x => throw new BadRequestException("Component id's can't be null."));
        }

        #endregion
    }
}