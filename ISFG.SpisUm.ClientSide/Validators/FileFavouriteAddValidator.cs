using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Models.File;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class FileFavouriteAddValidator : AbstractValidator<FileFavouriteAdd>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;

        #endregion

        #region Constructors

        public FileFavouriteAddValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
             .Cascade(CascadeMode.StopOnFirstFailure)
             .MustAsync(async (context, cancellationToken) =>
             {
                 _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

                 return _groupPaging != null;
             })
             .WithName("File")
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
