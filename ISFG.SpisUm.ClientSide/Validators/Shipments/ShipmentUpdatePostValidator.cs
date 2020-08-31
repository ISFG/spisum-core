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

namespace ISFG.SpisUm.ClientSide.Validators.Shipments
{
    public class ShipmentUpdatePostValidator : AbstractValidator<ShipmentUpdatePost>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private GroupMemberPaging _groupToDispatch;
        private NodeEntry _nodeEntry;
        private List<NodeAssociationEntry> _nodeParents;

        #endregion

        #region Constructors

        public ShipmentUpdatePostValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser, INodesService nodesService)
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
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.ShipmentPost)
                        .WithMessage(x => $"Provided nodeId must be NodeType {SpisumNames.NodeTypes.ShipmentPost}");

                    RuleFor(x => x)
                        .Must(y => _nodeParents?.Any(q => q.Entry.NodeType == SpisumNames.NodeTypes.Document || q.Entry.NodeType == SpisumNames.NodeTypes.File) ?? false)
                        .WithName(x => "Parent")
                        .WithMessage($"Provided shipment does not have a parent type of {SpisumNames.NodeTypes.Document} or {SpisumNames.NodeTypes.File}");

                    RuleFor(x => x)
                        .Must(y => CheckDispatchGroup(identityUser.RequestGroup))
                        .WithName(x => "Dispatch group")
                        .WithMessage($"Requested group in not part of {SpisumNames.Groups.DispatchGroup}");
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

            RuleFor(x => x.Body.PostType)
               .Must(x => x.All(x => x.Length < 100))
               .When(x => x.Body != null)
               .WithMessage("PostType is too long");

            RuleFor(x => x.Body.PostTypeOther)
               .Must(x => CheckLength(x, 100))
               .When(x => x.Body != null)
               .WithMessage("PostTypeOther is too long");

            RuleFor(x => x.Body.PostItemType)
               .Must(x => CheckLength(x, 100))
               .When(x => x.Body != null)
               .WithMessage("PostItemType is too long");

            RuleFor(x => x.Body.PostItemTypeOther)
               .Must(x => CheckLength(x, 100))
               .When(x => x.Body != null)
               .WithMessage("PostItemTypeOther is too long");

            RuleFor(x => x.Body.PostItemWeight)
              .Must(x => CheckNegativeNumber(x))
              .When(x => x.Body != null)
              .WithMessage("PostItemWeight can't be negative number");

            RuleFor(x => x.Body.PostItemPrice)
              .Must(x => CheckNegativeNumber(x))
              .When(x => x.Body != null)
              .WithMessage("PostItemWeight can't be negative number");

            RuleFor(x => x.Body.PostItemCashOnDelivery)
              .NotNull()
              .GreaterThanOrEqualTo(0)
              .When(x => x.Body != null && x.Body.PostType.Any(x => x == "Dobirka"))
              .WithMessage("PostItemCashOnDelivery is mandatory and cannot be negative number");

            RuleFor(x => x.Body.PostItemStatedPrice)
              .NotNull()
              .GreaterThanOrEqualTo(0)
              .When(x => x.Body != null && x.Body.PostType.Any(x => x == "UdanaCena"))
              .WithMessage("PostItemStatedPrice is mandatory and cannot be negative number");
        }

        #endregion

        #region Private Methods

        private bool CheckDispatchGroup(string requestGroup)
        {
            if (_nodeParents?.Any(x => x?.Entry?.Association?.AssocType == SpisumNames.Associations.ShipmentsToDispatch) ?? false)
                return _groupToDispatch?.List?.Entries?.Any(x => x.Entry.Id == requestGroup) ?? false;
            return true;
        }

        private bool CheckLength(string text, int maximumLength)
        {
            return string.IsNullOrWhiteSpace(text) ? true : text.Length <= maximumLength;
        }

        private bool CheckNegativeNumber(double? weight)
        {
            if (weight != null && weight < 0)
                return false;
            return true;
        }

        #endregion
    }
}
