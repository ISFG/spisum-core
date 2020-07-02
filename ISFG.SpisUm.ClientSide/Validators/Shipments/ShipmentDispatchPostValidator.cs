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
    public class ShipmentDispatchPostValidator : AbstractValidator<ShipmentDispatchPost>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;
        private List<NodeAssociationEntry> _nodeParents;

        #endregion

        #region Constructors

        public ShipmentDispatchPostValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser, INodesService nodesService)
        {
            RuleFor(o => o)
             .Cascade(CascadeMode.StopOnFirstFailure)
             .MustAsync(async (context, cancellationToken) =>
             {
                 _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                 _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                          .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));
                 _nodeParents = await nodesService.GetParentsByAssociation(context.NodeId, new List<string> { SpisumNames.Associations.ShipmentsToDispatch });

                 return _groupPaging != null && _nodeEntry != null && _nodeParents != null;
             })
             .WithName("Document")
             .WithMessage("Something went wrong with alfresco server.")
             .DependentRules(() =>
             {
                 RuleFor(x => x)
                       .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                       .WithName(x => "Group")
                       .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                 RuleFor(x => x.NodeId)
                    .Must(q => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.ShipmentPost)
                    .WithName("NodeId")
                    .WithMessage($"Provided node must be type of {SpisumNames.NodeTypes.ShipmentPost}.");

                 RuleFor(x => x.NodeId)
                    .Must(q => _nodeEntry?.Entry?.Path?.Name == AlfrescoNames.Prefixes.Path + SpisumNames.Paths.DispatchToDispatch)
                    .WithName("NodeId")
                    .WithMessage($"Provided node must be located in {AlfrescoNames.Prefixes.Path + SpisumNames.Paths.DispatchToDispatch}.");

                 RuleFor(x => x.NodeId)
                    .Must(q => _nodeParents?.Count > 0)
                    .WithName("NodeId")
                    .WithMessage($"Provided node is not child type of {SpisumNames.Associations.ShipmentsToDispatch}");

             });
        }

        #endregion
    }
}
