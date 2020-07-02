using FluentValidation;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models.Shredding;

namespace ISFG.SpisUm.ClientSide.Validators.Shredding
{
    public class ShreddingProposalCreateValidator : AbstractValidator<ShreddingProposalCreate>
    {
        #region Constructors

        public ShreddingProposalCreateValidator()
        {
            RuleFor(x => x.Name)
                .MinimumLength(4)
                .MaximumLength(50);

            RuleFor(x => x.Ids)
                .Must(y => y.Count > 0)
                .OnAnyFailure(x => throw new BadRequestException("", "At least one id must be provided"));
        }

        #endregion
    }
}