using System;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators.Shipments
{
    public class ShipmentCreatePersonallyValidator : AbstractValidator<ShipmentCreatePersonally>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public ShipmentCreatePersonallyValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

                    return _nodeEntry?.Entry?.Id != null && _groupPaging != null;
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
                        .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException("Document must be in evidence site."));

                });

            RuleFor(x => x.Body.Address1)
                .Must(x => CheckLength(x, 100))
                .When(x => x.Body != null)
                .WithMessage("Address1 is too long");

            RuleFor(x => x.Body.Address2)
                .Must(x => CheckLength(x, 100))
                .When(x => x.Body != null)
                .WithMessage("Address2 is too long");

            RuleFor(x => x.Body.Address3)
                .Must(x => CheckLength(x, 100))
                .When(x => x.Body != null)
                .WithMessage("Address3 is too long");

            RuleFor(x => x.Body.Address4)
                .Must(x => CheckLength(x, 100))
                .When(x => x.Body != null)
                .WithMessage("Address4 is too long");

            RuleFor(x => x.Body.AddressStreet)
                .Must(x => CheckLength(x, 100))
                .When(x => x.Body != null)
                .WithMessage("AddressStreet is too long");

            RuleFor(x => x.Body.AddressCity)
               .Must(x => CheckLength(x, 100))
               .When(x => x.Body != null)
               .WithMessage("AddressCity is too long");

            RuleFor(x => x.Body.AddressZip)
                .MaximumLength(10);

            RuleFor(x => x.Body.AddressState)
               .Must(x => CheckLength(x, 100))
               .When(x => x.Body != null)
               .WithMessage("AddressState is too long");
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
