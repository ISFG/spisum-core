using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.Email.Api.Interfaces;
using ISFG.Email.Api.Models;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators.Shipments
{
    public class ShipmentUpdateEmailValidator : AbstractValidator<ShipmentUpdateEmail>
    {
        #region Fields

        private List<EmailAccount> _accounts;

        private GroupPagingFixed _groupPaging;
        private GroupMemberPaging _groupToDispatch;
        private NodeEntry _nodeEntry;
        private List<NodeAssociationEntry> _nodeParents;

        #endregion

        #region Constructors

        public ShipmentUpdateEmailValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser, INodesService nodesService, IEmailHttpClient emailHttpClient)
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

                    try { _accounts = await emailHttpClient.Accounts(); } catch { }


                    if (_nodeParents?.Any(x => x?.Entry?.Association?.AssocType == SpisumNames.Associations.ShipmentsToDispatch) ?? false)
                        _groupToDispatch = await alfrescoHttpClient.GetGroupMembers(SpisumNames.Groups.DispatchGroup,
                            ImmutableList<Parameter>.Empty.Add(new Parameter(AlfrescoNames.Headers.Where, AlfrescoNames.MemberType.Group, ParameterType.QueryString)));

                    return _nodeEntry?.Entry?.Id != null && _groupPaging != null && _nodeParents != null && _accounts != null;
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
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.ShipmentEmail)
                        .WithMessage(x => $"Provided nodeId must be NodeType {SpisumNames.NodeTypes.ShipmentEmail}");

                    RuleFor(x => x)
                        .Must(y => _nodeParents?.Any(q => q.Entry.NodeType == SpisumNames.NodeTypes.Document) ?? false)
                        .WithName(x => "Parent")
                        .WithMessage($"Provided shipment does not have a parent type of {SpisumNames.NodeTypes.Document}");

                    RuleFor(x => x)
                        .Must(y => CheckDispatchGroup(identityUser.RequestGroup))
                        .WithMessage(x => "Dispatch group")
                        .WithMessage($"Requested group in not part of {SpisumNames.Groups.DispatchGroup}");

                    RuleFor(x => x)
                        .Must(x => _accounts.Count > 0)
                        .WithMessage(x => "No email configuration on email server");

                    RuleFor(x => x.Body.Sender)
                        .Must(y => _accounts?.Any(x => x.Username == y) ?? false)
                        .WithMessage(x => "Sender was not found in email configuration on email server");
                });

            RuleFor(x => x.Body.Recipient)
                        .Must(x => EmailUtils.IsValidEmail(x))
                        .When(x => x.Body != null)
                        .WithName(x => "Recipient")
                        .WithMessage("Recipient is not a valid email address");

            RuleFor(x => x.Body.Subject)
                        .Must(x => CheckLength(x, 255))
                        .When(x => x.Body != null)
                        .WithName(x => "Subject")
                        .WithMessage("Subject is too long");

            RuleFor(x => x.Body.Recipient)
                        .Must(x => CheckLength(x, 254))
                        .When(x => x.Body != null)
                        .WithName(x => "Recipient")
                        .WithMessage("Recipient is too long");

            RuleFor(x => x.Body.Components)
                .Must(y => y.Count > 0)
                .WithName(x => "Components")
                .WithMessage("Components cannot be empty");
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

        #endregion
    }
}
