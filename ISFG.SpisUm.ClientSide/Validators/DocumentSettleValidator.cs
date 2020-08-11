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
    public class DocumentSettleValidator : AbstractValidator<DocumentSettle>
    {
        #region Fields

        private readonly List<string> _notNullProperties = new List<string>
        {
            SpisumNames.Properties.Form,
            SpisumNames.Properties.Pid,
            SpisumNames.Properties.Ssid,
            SpisumNames.Properties.Subject,
            SpisumNames.Properties.Group,
            AlfrescoNames.ContentModel.Owner,
            SpisumNames.Properties.FilePlan,
            SpisumNames.Properties.FileMark,
            /*SpisumNames.Properties.RetentionMark,*/
            SpisumNames.Properties.RetentionMode
        };

        private List<NodeChildAssociationEntry> _components;

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;
        private readonly List<string> componentsId = new List<string>();
        private readonly List<string> componentsIdNotInOutputFormat = new List<string>();

        #endregion

        #region Constructors

        public DocumentSettleValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser, INodesService nodesService)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.Properties}", ParameterType.QueryString)));

                    _components = await nodesService.GetSecondaryChildren(context.NodeId, SpisumNames.Associations.Components, false, true);

                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

                    return _groupPaging != null && _nodeEntry?.Entry?.Id != null && _components != null;
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
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.Document)
                        .WithMessage(x => $"Provided nodeId must be NodeType {SpisumNames.NodeTypes.Document}");
                    
                    RuleFor(x => x)
                        .Must(x => _allowedPaths(identityUser.RequestGroup).Any(path => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + path, StringComparison.OrdinalIgnoreCase) == true))
                        .OnAnyFailure(x => throw new BadRequestException($"Document must be in {string.Join(" or ", _allowedPaths(identityUser.RequestGroup))}."));
                    
                    RuleFor(x => x.NodeId)
                        .Must(x =>
                        {
                            var nodeProperties = _nodeEntry.Entry.Properties.As<JObject>().ToDictionary();
                            if (nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.Form)?.ToString() == SpisumNames.Form.Analog && 
                                nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.ListCount)?.ToString() == null)
                                return false;


                            if (string.IsNullOrWhiteSpace(nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_OrgName)?.ToString()) &&
                                string.IsNullOrWhiteSpace(nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_Contact)?.ToString()) &&
                                string.IsNullOrWhiteSpace(nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.Sender_Name)?.ToString())
                                )
                                return false;

                            return _notNullProperties.All(property => nodeProperties.GetNestedValueOrDefault(property)?.ToString() != null);
                        })
                        .WithMessage(x => $"Missing properties in metadata {string.Join(", ", _notNullProperties)}, {SpisumNames.Properties.Sender_OrgName}," +
                        $" {SpisumNames.Properties.Sender_Contact}, {SpisumNames.Properties.Sender_Name}");

                    RuleFor(x => x.NodeId)
                       .Must(x =>
                       {
                           _components.ForEach(component =>
                           {
                               var nodeProperties = component?.Entry.Properties.As<JObject>().ToDictionary();

                               var fileIsInOutputFormat = nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.FileIsInOutputFormat)?.ToString();
                               var fileIsReadable = nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.FileIsReadable)?.ToString();
                               var componentType = nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.ComponentType)?.ToString();

                               // V_SETTLE_OUTPUTFORMAT
                               if (fileIsInOutputFormat == null || fileIsInOutputFormat == SpisumNames.Global.No)
                                   componentsIdNotInOutputFormat.Add(component?.Entry?.Id);

                               // V_SETTLE_READABLE_TYPE
                               if (string.IsNullOrWhiteSpace(fileIsReadable) || string.IsNullOrWhiteSpace(componentType))
                                   componentsId.Add(component?.Entry?.Id);
                           });

                           if (componentsId.Count != 0 && componentsIdNotInOutputFormat.Count != 0)
                               throw new BadRequestException(ErrorCodes.V_SETTLE_OUTPUTFORMAT_READABLE_TYPE);

                           if (componentsId.Count != 0)
                               throw new BadRequestException(ErrorCodes.V_SETTLE_READABLE_TYPE);

                           if (componentsIdNotInOutputFormat.Count != 0)
                               throw new BadRequestException(ErrorCodes.V_SETTLE_OUTPUTFORMAT);

                           return true;
                       });
                });
            
            RuleFor(x => x)
                .Must(x =>
                {
                    if (x?.Body?.SettleMethod != "jinyZpusob") 
                        return true;
                    
                    if (x?.Body?.CustomSettleMethod == null || x?.Body?.SettleReason == null)
                        return false;
                        
                    if (x.Body.SettleReason.Length < 4 || x.Body.SettleReason.Length < 4)
                        throw new BadRequestException(ErrorCodes.V_MIN_TEXT);

                    x.Body.SettleReason = x.Body.SettleReason.CutLength(30);
                    
                    return true;
                })
                .WithName(x => nameof(x.Body.SettleMethod))
                .WithMessage("You have to fill CustomSettleMethod and SettleReason.");
        }

        #endregion

        #region Private Methods

        private List<string> _allowedPaths(string group)
        {
            return new List<string>
            {
                SpisumNames.Paths.EvidenceDocumentsForProcessing(group),
                SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(group)
            };
        }

        #endregion
    }
}

