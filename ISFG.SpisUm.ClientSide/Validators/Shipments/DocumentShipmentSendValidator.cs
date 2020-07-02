using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators.Shipments
{
    public class DocumentShipmentSendValidator : AbstractValidator<DocumentShipmentSend>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;
        private readonly List<ParentShipmentInfo> _parents = new List<ParentShipmentInfo>();

        #endregion

        #region Constructors

        public DocumentShipmentSendValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser, INodesService nodesService)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));

                    await context.ShipmentsId?.ForEachAsync(async x =>
                    {
                        _parents.Add(new ParentShipmentInfo
                        {
                            ShipmentId = x,
                            Parents = await nodesService.GetParentsByAssociation(x, new List<string>
                            { SpisumNames.Associations.ShipmentsCreated, SpisumNames.Associations.ShipmentsToReturn })
                        });
                    });

                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

                    return _groupPaging != null && _nodeEntry != null && _parents != null;
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
                        .Must(y => _parents.All(p => p.Parents.Any(c => c.Entry.Id == y.NodeId)))
                        .WithName(x => "Shipments")
                        .WithMessage($"Not all shipments are type of {SpisumNames.Associations.ShipmentsCreated} or {SpisumNames.Associations.ShipmentsToReturn} or not all shipments are not associated with nodeId");
                });
        }

        #endregion

        #region Nested Types, Enums, Delegates

        private class ParentShipmentInfo
        {
            #region Properties

            public string ShipmentId { get; set; }
            public List<NodeAssociationEntry> Parents { get; set; }

            #endregion
        }

        #endregion
    }
}