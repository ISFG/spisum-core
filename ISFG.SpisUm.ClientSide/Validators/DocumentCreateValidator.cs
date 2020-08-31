using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentCreateValidator : AbstractValidator<DocumentCreate>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;

        #endregion

        #region Constructors

        public DocumentCreateValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    
                    return _groupPaging != null;
                })
                .WithName(x => nameof(x.NodeId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() => 
                { 
                    RuleFor(x => x)
                        .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                    RuleFor(x => x)
                       .Must(y => identityUser.RequestGroup == SpisumNames.Groups.MailroomGroup)
                       .WithName(x => "Group")
                       .WithMessage($"User is not member of {SpisumNames.Groups.MailroomGroup} group.");
                });
            
            RuleFor(x => x.DocumentType)
                .Must(x => x == null || CodeLists.DocumentTypes.List.TryGetValue(x, out string _))
                .WithMessage($"DocumentType must be one of [{string.Join(",", CodeLists.DocumentTypes.List.Select(x => x.Key).ToArray())}]");
        }

        #endregion
    }
}