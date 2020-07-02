using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
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
    public class DocumentPropertiesValidator : AbstractValidator<DocumentProperties>
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

        private readonly List<string> _missingProperties = new List<string>();

        private NodeEntry _nodeEntry;
        private readonly List<string> componentsId = new List<string>();
        private readonly List<string> componentsIdNotInOutputFormat = new List<string>();

        #endregion

        #region Constructors

        public DocumentPropertiesValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser, INodesService nodesService)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.Properties}", ParameterType.QueryString)));

                    _components = await nodesService.GetSecondaryChildren(context.NodeId, SpisumNames.Associations.Components, false, true);

                    return _nodeEntry?.Entry?.Id != null;
                })
                .WithName("Document")
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x)
                        .Must(x => _allowedPaths(identityUser.RequestGroup).Any(path => _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + path, StringComparison.OrdinalIgnoreCase) == true))
                        .OnAnyFailure(x => throw new BadRequestException($"Document must be in {string.Join(" or ", _allowedPaths(identityUser.RequestGroup))}."));
                    
                    RuleFor(x => x.NodeId)
                        .Must(x =>
                        {
                            var nodeProperties = _nodeEntry.Entry.Properties.As<JObject>().ToDictionary();

                            _missingProperties.AddRange(_notNullProperties.Where(property => nodeProperties.GetNestedValueOrDefault(property)?.ToString() == null));

                            if (nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.Form)?.ToString() == "analog" && 
                                nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.ListCount)?.ToString() == null)
                            {
                                _missingProperties.Add(SpisumNames.Properties.ListCount);
                                return false;
                            }                                

                            return _notNullProperties.All(property => nodeProperties.GetNestedValueOrDefault(property)?.ToString() != null);
                        })
                        .OnAnyFailure(x => throw new BadRequestException($"Document {x.NodeId} is missing properties in metadata {string.Join(", ", _missingProperties)}"));

                    RuleFor(x => x.NodeId)
                       .Must(x =>
                       {
                           _components.ForEach(component =>
                           {
                               var nodeProperties = component?.Entry.Properties.As<JObject>().ToDictionary();

                               var fileIsReadable = nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.FileIsReadable)?.ToString();
                               var componentType = nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.ComponentType)?.ToString();

                               if (string.IsNullOrWhiteSpace(fileIsReadable) || string.IsNullOrWhiteSpace(componentType))
                                   componentsId.Add(component?.Entry?.Id);
                           });

                           return componentsId.Count == 0;
                       })
                       .OnAnyFailure(x => throw new BadRequestException($"Components {string.Join(",", componentsId)} must have filled one of these properties: {SpisumNames.Properties.FileIsReadable}, " +
                                                                        $"{SpisumNames.Properties.ComponentType}"));

                    RuleFor(x => x.NodeId)
                        .Must(x =>
                        {
                            _components.ForEach(component =>
                            {
                                var nodeProperties = component?.Entry.Properties.As<JObject>().ToDictionary();

                                var fileIsInOutputFormat = nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.FileIsInOutputFormat)?.ToString();

                                if (fileIsInOutputFormat == null || fileIsInOutputFormat == "no")
                                    componentsIdNotInOutputFormat.Add(component?.Entry?.Id);
                            });

                            return componentsIdNotInOutputFormat.Count == 0;

                        })
                        .OnAnyFailure(x => throw new BadRequestException($"Components {string.Join(",", componentsIdNotInOutputFormat)} property {SpisumNames.Properties.FileIsInOutputFormat} must be true"));
                });
        }

        #endregion

        #region Private Methods

        private List<string> _allowedPaths(string group)
        {
            return new List<string>
            {
                SpisumNames.Paths.EvidenceFilesDocumentsProcessed(group),
                SpisumNames.Paths.EvidenceFilesDocumentsForProcessing(group)
            };
        }

        #endregion
    }
}