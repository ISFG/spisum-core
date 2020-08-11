using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.DataBox.Api.Interfaces;
using ISFG.DataBox.Api.Models;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators.Shipments
{
    public class ShipmentCreateDataBoxValidator : AbstractValidator<ShipmentCreateDataBox>
    {
        #region Fields

        private List<DataboxAccount> _accounts;

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public ShipmentCreateDataBoxValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser, IDataBoxHttpClient dataBoxHttpClient)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    try { _accounts = await dataBoxHttpClient.Accounts(); } catch { }

                    return _nodeEntry?.Entry?.Id != null && _groupPaging != null && _accounts != null;
                })
                .WithName(x => nameof(x.NodeId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x)
                        .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                    RuleFor(x => x.NodeId)
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.Document)
                        .WithMessage(x => $"Provided nodeId must be NodeType {SpisumNames.NodeTypes.Document}");

                    RuleFor(x => x)
                        .Must(x => _accounts.Count > 0)
                        .WithMessage(x => "No databox configuration on databox server");

                    RuleFor(x => x)
                        .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException("Document must be in evidence site."));

                    RuleFor(x => x.Body.Sender)
                        .Must(y => _accounts?.Any(x => x.Id == y) ?? false)
                        .WithMessage(x => "Sender was not found in databox configuration on databox server");
                });

            RuleFor(x => x.Body.LegalTitleLaw)
                        .Must(x => CheckLength(x, 4))
                        .When(x => x.Body != null)
                        .WithMessage("LegalTitleLaw is too long");

            RuleFor(x => x.Body.LegalTitleYear)
                        .Must(x => CheckLength(x, 4))
                        .When(x => x.Body != null)
                        .WithMessage("LegalTitleYear is too long");

            RuleFor(x => x.Body.LegalTitleSect)
                        .Must(x => CheckLength(x, 4))
                        .When(x => x.Body != null)
                        .WithMessage("LegalTitleSect is too long");

            RuleFor(x => x.Body.LegalTitlePar)
                        .Must(x => CheckLength(x, 2))
                        .When(x => x.Body != null)
                        .WithMessage("LegalTitlePar is too long");

            RuleFor(x => x.Body.LegalTitlePoint)
                        .Must(x => CheckLength(x, 2))
                        .When(x => x.Body != null)
                        .WithMessage("LegalTitlePoint is too long");

            RuleFor(x => x.Body.Recipient)
                        .Must(x => CheckLength(x, 7))
                        .When(x => x.Body != null)
                        .WithMessage("Recipient is too long");

            RuleFor(x => x.Body.Subject)
                        .Must(x => CheckLength(x, 255))
                        .When(x => x.Body != null)
                        .WithMessage("Subject is too long");

            RuleFor(x => x.Body.ToHands)
                        .Must(x => CheckLength(x, 30))
                        .When(x => x.Body != null)
                        .WithMessage("ToHands is too long");

            RuleFor(x => x.Body.Components)
                .Must(y => y.Count > 0)
                .WithName(x => "Components")
                .WithMessage("Components cannot be empty");
        }

        #endregion

        #region Private Methods

        private bool CheckLength(string text, int maximumLength)
        {
            return string.IsNullOrWhiteSpace(text) ? true : text.Length <= maximumLength;
        }

        #endregion
    }
}
