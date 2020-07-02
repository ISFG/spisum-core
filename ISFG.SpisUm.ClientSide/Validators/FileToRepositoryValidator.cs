using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.File;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class FileToRepositoryValidator : AbstractValidator<FileToRepository>
    {
        #region Fields

        private GroupMemberPaging _groupMember;
        private GroupPagingFixed _groupPaging;

        #endregion

        #region Constructors

        public FileToRepositoryValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _groupMember = await alfrescoHttpClient.GetGroupMembers(SpisumNames.Groups.RepositoryGroup, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Where, AlfrescoNames.MemberType.Group, ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    
                    return _groupMember != null && _groupPaging != null;
                })
                .WithName(x => nameof(x.Group))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x)
                        .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");
                    
                    RuleFor(x => x)
                        .Must(y => _groupMember?.List?.Entries?.Any(q => q.Entry.Id == y.Group) ?? false)
                        .WithName(x => "Group")
                        .WithMessage(x => $"Group {x.Group} does not exists.");
                });
        }

        #endregion
    }
}