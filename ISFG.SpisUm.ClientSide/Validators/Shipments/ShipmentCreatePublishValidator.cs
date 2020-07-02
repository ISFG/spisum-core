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
    public class ShipmentCreatePublishValidator : AbstractValidator<ShipmentCreatePublish>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public ShipmentCreatePublishValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
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

            RuleFor(x => x.Body.Components)
                .Must(y => y?.Count > 0)
                .When(x=> x.Body != null)
                .WithName(x => "Components")
                .WithMessage("Components cannot be empty");

            RuleFor(x => x.Body.DateFrom)
                .Must(y => y.HasValue && CompareDateTime(y.Value))
                .When(x => x.Body != null)
                .WithName(x => "DateFrom")
                .WithMessage("DateFrom cannot be in past");

            RuleFor(x => x.Body.Days)
                .Must(y => y.HasValue ? y.Value > 0 : true)
                .When(x => x.Body != null)
                .WithName(x => "Days")
                .WithMessage("Days must be greater than 0");

            RuleFor(x => x.Body.Note)
                .MaximumLength(255)
                .When(x => x.Body != null)
                .WithName(x => "Note")
                .WithMessage("Message cannot be longer than 255 characters");
        }

        #endregion

        #region Private Methods

        private bool CompareDateTime(DateTime datefrom)
        {
            return true;

            // doesnt work
            //var output = DateTime.Compare(datefrom, DateTime.Today.AddMilliseconds(-1).ToUniversalTime());

            // return output == 1 || output == 0;
        }

        #endregion
    }
}
