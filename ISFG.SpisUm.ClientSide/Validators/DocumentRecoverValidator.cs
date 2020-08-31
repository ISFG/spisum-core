using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Document;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentRecoverValidator : AbstractValidator<DocumentRecover>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;

        #endregion

        #region Constructors

        public DocumentRecoverValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
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
            
            RuleFor(x => x)
                .Must(CheckReasonLength);
        }

        #endregion

        #region Private Methods

        private bool CheckReasonLength(DocumentRecover node)
        {
            if (node?.Reason == null || node.Reason.Length < 4)
                throw new BadRequestException(ErrorCodes.V_MIN_TEXT);

            if (node.Reason.Length > 30)
                node.Reason = node.Reason.Substring(0, 30);

            return true;
        }

        #endregion
    }
}
