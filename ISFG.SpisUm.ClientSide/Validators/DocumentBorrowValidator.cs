using System;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Document;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentBorrowValidator : AbstractValidator<DocumentBorrow>
    {
        #region Fields

        private readonly IIdentityUser _identityUser;
        private string _borrowedUser;
        private GroupPagingFixed _groupPaging;
        private GroupPagingFixed _groupPagingNextOwner;
        private GroupMemberPaging _groupRepository;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public DocumentBorrowValidator(IAlfrescoHttpClient alfrescoHttpClient,
                                       IIdentityUser identityUser,
                                       IAlfrescoConfiguration alfrescoConfiguration,
                                       ISimpleMemoryCache simpleMemoryCache,
                                       ISystemLoginService systemLoginService)
        {
            var adminHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));
            _identityUser = identityUser;

            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                    var documentProperties = _nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
                    _borrowedUser = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.Borrower)?.ToString();

                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

                    _groupRepository = await alfrescoHttpClient.GetGroupMembers(SpisumNames.Groups.RepositoryGroup,
                        ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Where, AlfrescoNames.MemberType.Group, ParameterType.QueryString)));

                    _groupPagingNextOwner = await adminHttpClient.GetPersonGroups(context.Body.User);

                    return _nodeEntry?.Entry?.Id != null && _groupPaging != null && _groupPagingNextOwner != null && _groupRepository != null;
                })
                .WithName(x => nameof(x.NodeId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x)
                        .Must(y =>
                        {
                            return _groupPagingNextOwner?.List?.Entries?.Any(q => q.Entry.Id == y.Body.Group) ?? false;
                        })
                        .WithName(x => "User")
                        .WithMessage("User isn't member of the Group.");

                    RuleFor(x => x)
                        .Must(y =>
                        {
                            return _groupRepository?.List?.Entries?.Any(x => x.Entry.Id == identityUser.RequestGroup) ?? false;
                        })
                        .WithName(x => "User")
                        .WithMessage("User isn't member of repository group");

                    RuleFor(x => x)
                        .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                    RuleFor(x => x.NodeId)
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.Document)
                        .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.Document}.");

                    RuleFor(x => x)
                        .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.RepositoryDocuments, StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException("Document must be in repository site."));

                    RuleFor(x => x)
                        .Must(x => string.IsNullOrWhiteSpace(_borrowedUser))
                        .WithMessage("Document is already borrowed");
                });
        }

        #endregion
    }
}
