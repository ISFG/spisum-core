using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Document;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentReturnForReworkValidator : AbstractValidator<DocumentReturnForRework>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public DocumentReturnForReworkValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path}", ParameterType.QueryString)));

                    return _groupPaging != null && _nodeEntry?.Entry?.Id != null;
                })
                .WithName(x => nameof(x.NodeId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x)
                        .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ??
                                   false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                    RuleFor(x => x.NodeId)
                        .Must(x => {
                            var group = _nodeEntry?.Entry?.Properties?.TryGetValueFromProperties<string>(SpisumNames.Properties.Group);
                            return group != null && (_nodeEntry.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceDocumentsForProcessingForSignature(group)) ||
                                      _nodeEntry.Entry.Path.Name.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceFilesDocumentsForProcessingForSignature(group)));
                        })
                        .WithMessage($"NodeId must be in path {SpisumNames.Paths.EvidenceDocumentsForProcessing(identityUser.RequestGroup)} or {SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(identityUser.RequestGroup)}.");

                });

            RuleFor(x => x.Body.Reason)
                .MinimumLength(4)
                .MaximumLength(30)
                .When(x => x.Body != null)
                .WithMessage("Reason must be at least 4 characters long but maximum 30 characters long");
        }

        #endregion
    }
}