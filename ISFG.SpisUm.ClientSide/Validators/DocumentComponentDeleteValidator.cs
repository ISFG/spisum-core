using System;
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
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentComponentDeleteValidator : AbstractValidator<DocumentComponentDelete>
    {
        #region Fields

        private readonly List<ParentsInfo> _nodeParents = new List<ParentsInfo>();

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public DocumentComponentDeleteValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser, INodesService nodesService)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));
                    await context.ComponentsId.ForEachAsync(async x =>
                    {
                        _nodeParents.Add(new ParentsInfo
                        {
                            Parents = await nodesService.GetParentsByAssociation(x, new List<string> {SpisumNames.Associations.DeletedComponents}),
                            ComponentId = x
                        });
                    });


                    return _groupPaging != null && _nodeEntry != null && _nodeParents.Count != 0;
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
                        .Must(y => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                        .WithMessage($"Provided nodeId must be type of {SpisumNames.NodeTypes.Document}");

                    RuleFor(x => x.ComponentsId)
                        .Must(y => _nodeParents.All(x => x.Parents.Count == 0))
                        .WithMessage("One or more components were already canceleted/deleted");

                    RuleFor(x => x)
                        .Must(x =>
                        {
                            var isPathMailroom = _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomUnfinished,
                                StringComparison.OrdinalIgnoreCase);

                            if (isPathMailroom == null)
                                throw new BadRequestException("", "Something went wrong with document path");

                            var properties = _nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
                            var form = properties.GetNestedValueOrDefault(SpisumNames.Properties.Form)?.ToString();
                            var documentType = properties.GetNestedValueOrDefault(SpisumNames.Properties.DocumentType)?.ToString();
                            var senderType = properties.GetNestedValueOrDefault(SpisumNames.Properties.SenderType)?.ToString();
                            var isLocked = _nodeEntry?.Entry?.IsLocked;

                            if (isPathMailroom.Value)
                                return form == "analog" || documentType == "technicalDataCarries";

                            if (isLocked != null && !isLocked.Value && senderType == SpisumNames.SenderType.Own)
                                return true;

                            return false;
                        })
                        .OnAnyFailure(x => throw new BadRequestException("Adding component is not allowed."));
                });
        }

        #endregion

        #region Nested Types, Enums, Delegates

        private class ParentsInfo
        {
            #region Properties

            public string ComponentId { get; set; }
            public List<NodeAssociationEntry> Parents { get; set; }

            #endregion
        }

        #endregion
    }
}