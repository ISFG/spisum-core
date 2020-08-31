using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class ConceptCancelValidator : AbstractValidator<ConceptCancel>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public ConceptCancelValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
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
                        .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                        .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.Concept}.");

                    RuleFor(x => x)
                         .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == true)
                         .OnAnyFailure(x => throw new BadRequestException("Concept must be in evidence site."));

                    RuleFor(x => x)
                        .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceConcepts(identityUser.RequestGroup), StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException("Concept can't be cancelled"));

                    RuleFor(x => x.NodeId)
                        .Must(IsNodePathAllowed);
            
                    RuleFor(x => x)
                        .Must(CheckReasonLength);
                });
        }

        #endregion

        #region Private Methods

        private bool CheckReasonLength(ConceptCancel node)
        {
            if (_nodeEntry?.Entry?.Path?.Name?.Equals(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomUnfinished) == true)
                return true;
            
            if (node?.Body?.Reason == null || node.Body.Reason.Length < 4)
                throw new BadRequestException(ErrorCodes.V_MIN_TEXT);

            if (node.Body.Reason.Length > 30)
                node.Body.Reason = node.Body.Reason.Substring(0, 30);

            return true;
        }

        private bool IsNodePathAllowed(string nodeId)
        {
            var forbidenPaths = new List<string> { $"{SpisumNames.Paths.RMDocumentLibrary}/" };
            var pathRegex = new Regex($"({string.Join("|", forbidenPaths.Select(x => $"{x}(.*)"))})$", RegexOptions.IgnoreCase);

            if (pathRegex.IsMatch(_nodeEntry?.Entry?.Path?.Name))
                throw new BadRequestException(ErrorCodes.V_FORBIDDEN_PATH);

            return true;
        }

        #endregion
    }
}