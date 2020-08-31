using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Models.Shipments;

namespace ISFG.SpisUm.ClientSide.Validators.Shipments
{

    public class ShipmentCancelValidator : AbstractValidator<ShipmentCancel>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private readonly List<NodeEntry> _nodeEntries = new List<NodeEntry>();

        #endregion

        #region Constructors

        public ShipmentCancelValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
             .Cascade(CascadeMode.StopOnFirstFailure)
             .MustAsync(async (context, cancellationToken) =>
             {
                 _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                 await context.NodeIds.ForEachAsync(async x => { _nodeEntries.Add(await alfrescoHttpClient.GetNodeInfo(x)); });

                 return _groupPaging != null && _nodeEntries?.Count != 0;
             })
             .WithName("Document")
             .WithMessage("Something went wrong with alfresco server.")
             .DependentRules(() =>
             {
                 RuleFor(x => x)
                       .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                       .WithName(x => "Group")
                       .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                 RuleFor(x => x)
                       .Must(y => _nodeEntries?.All(q => q.Entry.NodeType.StartsWith("ssl:shipment")) ?? false)
                       .WithName(x => "NodeIds")
                       .WithMessage("Not all provided nodes are type of shipment.");
             });
        }

        #endregion
    }
}