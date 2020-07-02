using System;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Alfresco.Api.Services;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Authentification;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentOwnerHandOverValidator : AbstractValidator<DocumentOwnerHandOver>
    {
        #region Fields

        private readonly IIdentityUser _identityUser;
        private GroupPagingFixed _groupPagingCurrentUser;
        private GroupPagingFixed _groupPagingNextOwner;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public DocumentOwnerHandOverValidator(
            IAlfrescoHttpClient alfrescoHttpClient, 
            IIdentityUser identityUser, 
            IAlfrescoConfiguration alfrescoConfiguration,
            ISimpleMemoryCache simpleMemoryCache,
            ISystemLoginService systemLoginService)
        {
            var adminHttpClient = new AlfrescoHttpClient(alfrescoConfiguration, new AdminAuthentification(simpleMemoryCache, alfrescoConfiguration, systemLoginService));
            _identityUser = identityUser;

            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties},{AlfrescoNames.Includes.Permissions},{AlfrescoNames.Includes.Path}", ParameterType.QueryString)));
                    _groupPagingCurrentUser = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    
                    if (context?.Body?.NextOwner != null) 
                        _groupPagingNextOwner = await adminHttpClient.GetPersonGroups(context.Body.NextOwner);
                    
                    return _nodeEntry?.Entry?.Id != null && _groupPagingCurrentUser != null;
                })
                .WithName(x => nameof(x.NodeId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() => 
                {
                    RuleFor(x => x)
                        .Must(y =>
                        {
                            if (y?.Body?.NextOwner == null) 
                                return true;
                            return _groupPagingNextOwner?.List?.Entries?.Any(q => q.Entry.Id == y.Body.NextGroup) ?? false;
                        })
                        .WithName(x => "Group")
                        .WithMessage("NextOwner isn't member of group NextGroup.");

                    RuleFor(x => x)
                        .Must(y => _groupPagingCurrentUser?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                    RuleFor(x => x)
                         .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == true)
                         .OnAnyFailure(x => throw new BadRequestException("Document must be in evidence site."));

                    RuleFor(x => x.NodeId)
                        .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document || _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.File || _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                        .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.Document} or {SpisumNames.NodeTypes.File} or {SpisumNames.NodeTypes.Concept}");
                    
                    RuleFor(x => x.NodeId)
                        .Must(HasAction)
                        .WithName(x => nameof(x.NodeId))
                        .WithMessage("NodeIs is being handled to different owner. You have cancel it first.");
            
                    RuleFor(x => x.NodeId)
                        .Must(CanUserMakeAction)
                        .WithMessage("User has no access to this action.");
                });
            
            RuleFor(x => x)
                .Must(x => !string.IsNullOrEmpty(x?.Body?.NextOwner) || !string.IsNullOrEmpty(x?.Body?.NextGroup))
                .WithName(x => nameof(x.Body.NextOwner))
                .WithMessage("You have to fill nextGroup or nextOwner.");
            
            RuleFor(x => x)
                .Must(x => x?.Body?.NextOwner == null || !string.IsNullOrEmpty(x?.Body?.NextGroup))
                .WithName(x => nameof(x.Body.NextGroup))
                .WithMessage("You have to fill NextGroup.");
        }

        #endregion

        #region Private Methods

        private bool CanUserMakeAction(string nodeId)
        {
            var properties = _nodeEntry.Entry.Properties.As<JObject>().ToDictionary();
            var group = properties?.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

            string requestGroup = _identityUser.RequestGroup;
            if (string.IsNullOrEmpty(requestGroup) || group == null)
                return false;

            return requestGroup.Contains(group);
        }

        private bool HasAction(string nodeId)
        {
            var properties = _nodeEntry.Entry.Properties.As<JObject>().ToDictionary();
            var nextGroup = properties?.GetNestedValueOrDefault(SpisumNames.Properties.NextGroup)?.ToString();

            return nextGroup == null;
        }

        #endregion
    }
}