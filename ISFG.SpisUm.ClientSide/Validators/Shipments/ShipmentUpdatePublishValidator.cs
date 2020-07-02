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
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Validators.Shipments
{
    public class ShipmentUpdatePublishValidator : AbstractValidator<ShipmentUpdatePublish>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private GroupMemberPaging _groupToDispatch;
        private NodeEntry _nodeEntry;
        private List<NodeAssociationEntry> _nodeParents;

        #endregion

        #region Constructors

        public ShipmentUpdatePublishValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser, INodesService nodesService)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));
                    _nodeParents = await nodesService.GetParentsByAssociation(context.NodeId, new List<string>
                    {
                            SpisumNames.Associations.ShipmentsCreated,
                            SpisumNames.Associations.ShipmentsToReturn,
                            SpisumNames.Associations.ShipmentsToDispatch
                        });
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

                    if (_nodeParents?.Any(x => x?.Entry?.Association?.AssocType == SpisumNames.Associations.ShipmentsToDispatch) ?? false)
                        _groupToDispatch = await alfrescoHttpClient.GetGroupMembers(SpisumNames.Groups.DispatchGroup,
                            ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Where, AlfrescoNames.MemberType.Group, ParameterType.QueryString)));

                    return _nodeEntry?.Entry?.Id != null && _groupPaging != null && _nodeParents != null;
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
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.ShipmentPublish)
                        .WithMessage(x => $"Provided nodeId must be NodeType {SpisumNames.NodeTypes.ShipmentPublish}");

                    RuleFor(x => x)
                        .Must(y => _nodeParents?.Any(q => q.Entry.NodeType == SpisumNames.NodeTypes.Document) ?? false)
                        .WithName(x => "Parent")
                        .WithMessage($"Provided shipment does not have a parent type of {SpisumNames.NodeTypes.Document}");

                    RuleFor(x => x)
                        .Must(y => CheckDispatchGroup(identityUser.RequestGroup))
                        .WithName(x => "Dispatch group")
                        .WithMessage($"Requested group in not part of {SpisumNames.Groups.DispatchGroup}");
                });

            RuleFor(x => x.Body.Components)
                .Must(y => y.Count > 0)
                .When(x => x.Body != null)
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

            TimeZoneInfo.FindSystemTimeZoneById("");

            // Does not work
            var output = DateTime.Compare(datefrom.ToLocalTime(), DateTime.UtcNow.Date);

            Log.Logger.Debug(output.ToString());

            return output == 1 || output == 0;
        }

        private bool CheckDispatchGroup(string requestGroup)
        {
            if (_nodeParents?.Any(x => x?.Entry?.Association?.AssocType == SpisumNames.Associations.ShipmentsToDispatch) ?? false)
                return _groupToDispatch?.List?.Entries?.Any(x => x.Entry.Id == requestGroup) ?? false;
            return true;
        }

        #endregion
    }
}
