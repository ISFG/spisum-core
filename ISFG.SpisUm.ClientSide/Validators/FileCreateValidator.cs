using System;
using System.Collections.Immutable;
using System.Linq;
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
    public class FileCreateValidator : AbstractValidator<FileCreate>
    {
        #region Fields

        private NodeAssociationPaging _associationPaging;

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public FileCreateValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.DocumentId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties},{AlfrescoNames.Includes.Permissions},{AlfrescoNames.Includes.Path}", ParameterType.QueryString)));
                    _associationPaging = await alfrescoHttpClient.GetNodeParents(context.DocumentId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Documents}')", ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    
                    return _nodeEntry?.Entry?.Id != null && _associationPaging != null && _groupPaging != null;
                })
                .WithName(x => nameof(x.DocumentId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x)
                        .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");
                    
                    RuleFor(x => x.DocumentId)
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.Document)
                        .WithMessage(x => $"Provided nodeId must be NodeType {SpisumNames.NodeTypes.Document}");

                    RuleFor(x => x)
                        .Must(x => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.Evidence, StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException("Document must be in evidence site."));

                    RuleFor(x => x.DocumentId)
                        .Must(x => _associationPaging.List.Entries.Count == 0)
                        .WithMessage(x => "Provided document is already in file");

                });
        }

        #endregion
    }
}
