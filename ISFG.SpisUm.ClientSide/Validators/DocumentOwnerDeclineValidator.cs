using System;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentOwnerDeclineValidator : AbstractValidator<DocumentOwnerDecline>
    {
        #region Fields

        private readonly IIdentityUser _identityUser;
        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public DocumentOwnerDeclineValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            _identityUser = identityUser;

            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties},{AlfrescoNames.Includes.Permissions},{AlfrescoNames.Includes.Path}", ParameterType.QueryString)));
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
                        .Must(HasAction)
                        .WithMessage("NodeIs has no decline action.");
            
                    RuleFor(x => x.NodeId)
                        .Must(CanUserMakeAction)
                        .WithMessage("User has no access to this action.");

                    RuleFor(x => x)
                         .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == true
                            || _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomWaitingForTakeOver, StringComparison.OrdinalIgnoreCase) == true)
                         .OnAnyFailure(x => throw new BadRequestException("Document must be in evidence/mailroom site."));
                });
        }

        #endregion

        #region Private Methods

        private bool CanUserMakeAction(string nodeId)
        {
            var properties = _nodeEntry.Entry.Properties.As<JObject>().ToDictionary();
            var nextGroup = properties?.GetNestedValueOrDefault(SpisumNames.Properties.NextGroup)?.ToString();
            
            string requestGroup = _identityUser.RequestGroup;
            if (string.IsNullOrEmpty(requestGroup) || nextGroup == null)
                return false;

            return requestGroup.Contains(nextGroup);
        }

        private bool HasAction(string nodeId)
        {
            var properties = _nodeEntry.Entry.Properties.As<JObject>().ToDictionary();
            var nextOwner = properties?.GetNestedValueOrDefault(SpisumNames.Properties.NextOwner)?.ToString();
            var nextGroup = properties?.GetNestedValueOrDefault(SpisumNames.Properties.NextGroup)?.ToString();

            return nextOwner != null || nextGroup != null;
        }

        #endregion
    }
}