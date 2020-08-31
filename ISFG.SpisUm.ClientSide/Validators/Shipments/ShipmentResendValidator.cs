using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Models.Shipments;

namespace ISFG.SpisUm.ClientSide.Validators.Shipments
{
    public class ShipmentResendValidator : AbstractValidator<ShipmentResend>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;

        #endregion

        #region Constructors

        public ShipmentResendValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
             .Cascade(CascadeMode.StopOnFirstFailure)
             .MustAsync(async (context, cancellationToken) =>
             {
                 _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

                 return _groupPaging != null;
             })
             .WithName("Document")
             .WithMessage("Something went wrong with alfresco server.")
             .DependentRules(() =>
             {
                 RuleFor(x => x)
                       .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                       .WithName(x => "Group")
                       .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");
             });
        }

        #endregion
    }
}
