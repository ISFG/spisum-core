using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Concept;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators.Concept
{
    public class ConceptComponentCancelValidator : AbstractValidator<ConceptComponentCancel>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;
        private NodeChildAssociationPagingFixed _nodeChildrens;

        #endregion

        #region Constructors

        public ConceptComponentCancelValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
               .Cascade(CascadeMode.StopOnFirstFailure)
               .MustAsync(async (context, cancellationToken) =>
               {
                   _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                       .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));
                   _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                   _nodeChildrens = await alfrescoHttpClient.GetNodeSecondaryChildren(context.NodeId, ImmutableList<Parameter>.Empty
                       .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                       .Add(new Parameter(AlfrescoNames.Headers.Where,
                           $"(assocType='{SpisumNames.Associations.Components}')"
                           , ParameterType.QueryString)));

                   return _nodeEntry?.Entry?.Id != null && _groupPaging != null && _nodeChildrens != null;
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
                       .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                       .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.Concept}.");

                   RuleFor(x => x)
                        .Must(c => c.ComponentsId.All(x => _nodeChildrens?.List?.Entries?.Any(y => y?.Entry?.Id == x) ?? false))
                        .WithMessage("Not all components are associated with nodeId");
               });
        }

        #endregion
    }
}
