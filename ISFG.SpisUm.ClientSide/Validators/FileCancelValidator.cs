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
using ISFG.SpisUm.ClientSide.Models.File;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class FileCancelValidator : AbstractValidator<FileCancel>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;
        private NodeChildAssociationPagingFixed _secondaryChildren;

        #endregion

        #region Constructors

        public FileCancelValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));
                    _secondaryChildren = await alfrescoHttpClient.GetNodeSecondaryChildren(context.NodeId, ImmutableList<Parameter>.Empty
                            .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    
                    return _nodeEntry?.Entry?.Id != null && _secondaryChildren != null && _groupPaging != null;
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
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.File)
                        .WithMessage(x => $"Provided nodeId must be NodeType {SpisumNames.NodeTypes.File}");

                    RuleFor(x => x)
                        .Must(x => _secondaryChildren?.List?.Entries?.Count == null || !_secondaryChildren.List.Entries.Any())
                        .OnAnyFailure(x => throw new BadRequestException(ErrorCodes.V_FILE_CANCEL_CHILDREN));

                    RuleFor(x => x)
                        .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException("Document must be in evidence site."));

                    RuleFor(x => x)
                        .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.EvidenceFilesOpen(identityUser.RequestGroup), StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException("File can't be cancelled"));
                    
                    RuleFor(x => x.NodeId) 
                        .Must(x => IsNodePathAllowed(x, identityUser.RequestGroup));
                });
            
            RuleFor(x => x)
                .Must(CheckReasonLength);
        }

        #endregion

        #region Private Methods

        private bool CheckReasonLength(FileCancel node)
        {
            if (node?.Body?.Reason == null || node.Body.Reason.Length < 4)
                throw new BadRequestException(ErrorCodes.V_MIN_TEXT);

            if (node.Body.Reason.Length > 30)
                node.Body.Reason = node.Body.Reason.Substring(0, 30);

            return true;
        }

        private bool IsNodePathAllowed(string nodeId, string group)
        {
            var forbidenPaths = new List<string> { $"{SpisumNames.Paths.RMDocumentLibrary}/", $"{SpisumNames.Paths.EvidenceCancelled(group)}" };
            var pathRegex = new Regex($"({string.Join("|", forbidenPaths.Select(x => $"{x}(.*)"))})$", RegexOptions.IgnoreCase);

            if (pathRegex.IsMatch(_nodeEntry?.Entry?.Path?.Name))
                throw new BadRequestException(ErrorCodes.V_FORBIDDEN_PATH);

            return true;
        }

        #endregion
    }
}
