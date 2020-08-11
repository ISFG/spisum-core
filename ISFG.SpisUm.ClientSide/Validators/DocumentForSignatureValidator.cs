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
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Immutable;
using System.Linq;
using ISFG.SpisUm.ClientSide.Models.Document;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentForSignatureValidator : AbstractValidator<DocumentForSignature>
    {
        #region Fields

        private GroupPagingFixed _groupPagingCurrentUser;
        private GroupPagingFixed _groupPagingNextOwner;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public DocumentForSignatureValidator(
            IAlfrescoHttpClient alfrescoHttpClient,
            IIdentityUser identityUser,
            IAlfrescoConfiguration alfrescoConfiguration,
            ISimpleMemoryCache simpleMemoryCache,
            ISystemLoginService systemLoginService)
        {
            var adminHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));

            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path}", ParameterType.QueryString)));
                    _groupPagingCurrentUser = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    _groupPagingNextOwner = await adminHttpClient.GetPersonGroups(context.Body.User);

                    return _nodeEntry?.Entry?.Id != null && _groupPagingCurrentUser != null && _groupPagingNextOwner != null;
                })
                .WithName(x => nameof(x.NodeId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x)
                        .Must(y => _groupPagingCurrentUser?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                    RuleFor(x => x)
                        .Must(y => _groupPagingNextOwner?.List?.Entries?.Any(q => q.Entry.Id == $"{y.Body.Group}_Sign") ?? false)
                        .WithName(x => "Group")
                        .WithMessage("User for signing isn't member of group with postfix _Sign.");

                    RuleFor(x => x.NodeId)
                        .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                        .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.Document}.");

                    RuleFor(x => x)
                        .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException("Document must be in evidence site."));

                    RuleFor(x => x.NodeId)
                        .Must(x => _nodeEntry.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsForProcessing(identityUser.RequestGroup)) ||
                                   _nodeEntry.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(identityUser.RequestGroup)))
                        .WithMessage($"NodeId must be in path {SpisumNames.Paths.EvidenceDocumentsForProcessing(identityUser.RequestGroup)} or {SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(identityUser.RequestGroup)}.");

                    RuleFor(x => x.NodeId)
                      .Must(x =>
                      {
                          var nodeProperties = _nodeEntry?.Entry.Properties.As<JObject>().ToDictionary();

                          return nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.SenderType)?.ToString() == SpisumNames.SenderType.Own;
                      })
                      .OnAnyFailure(x => throw new BadRequestException($"Document's property {SpisumNames.Properties.SenderType} is not equal to {SpisumNames.SenderType.Own}"));
                });

            RuleFor(x => x)
                .Must(x => !string.IsNullOrEmpty(x?.Body?.User))
                .When(x => x?.Body != null)
                .WithName(x => nameof(x.Body.User))
                .WithMessage("You have to fill user.");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrEmpty(x?.Body?.Group))
                .When(x => x?.Body != null)
                .WithName(x => nameof(x.Body.Group))
                .WithMessage("You have to fill group.");
        }

        #endregion
    }
}