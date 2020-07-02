using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentToRepositoryDocumentValidator : AbstractValidator<DocumentProperties>
    {
        #region Fields

        private readonly List<string> _notNullProperties = new List<string>
        {
            SpisumNames.Properties.Pid,
            SpisumNames.Properties.Ssid,
            SpisumNames.Properties.Subject,
            SpisumNames.Properties.AttachmentsCount,
            SpisumNames.Properties.Group,
            AlfrescoNames.ContentModel.Owner,
            SpisumNames.Properties.FilePlan,
            SpisumNames.Properties.FileMark,
            /*SpisumNames.Properties.RetentionMark,*/
            SpisumNames.Properties.RetentionMode,
            SpisumNames.Properties.Form,
            SpisumNames.Properties.SettleMethod
        };

        private readonly List<string> _oneOfThemNotNull = new List<string>
        {
            SpisumNames.Properties.Sender_OrgName,
            SpisumNames.Properties.Sender_Contact,
            SpisumNames.Properties.Sender_Name
        };

        private NodeEntry _nodeEntry;
        private List<NodeChildAssociationEntry> _secondaryChildren;

        #endregion

        #region Constructors

        public DocumentToRepositoryDocumentValidator(IAlfrescoHttpClient alfrescoHttpClient, INodesService nodesService)
        {
             RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.Properties}", ParameterType.QueryString)));

                    _secondaryChildren = await nodesService.GetSecondaryChildren(context.NodeId, new List<string>
                    {
                        SpisumNames.Associations.ShipmentsCreated,
                        SpisumNames.Associations.ShipmentsToDispatch,
                        SpisumNames.Associations.ShipmentsToReturn
                    });
                    
                    return  _nodeEntry?.Entry?.Id != null && _secondaryChildren != null;
                })
                .WithName("Document")
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.NodeId)
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.Document)
                        .OnAnyFailure(x => throw new Exception($"Provided nodeId must be NodeType of {SpisumNames.NodeTypes.Document}"));

                    RuleFor(x => x.NodeId)
                        .Must(x => _secondaryChildren.Count == 0)
                        .OnAnyFailure(x => throw new Exception($"Not all shipments are dispatched for nodeId {x.NodeId}"));

                    RuleFor(x => x.NodeId)
                        .Must(x =>
                        {
                            var nodeProperties = _nodeEntry.Entry.Properties.As<JObject>().ToDictionary();
                            if (nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.Form)?.ToString() == "analog" && 
                                nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.ListCount)?.ToString() == null)
                                return false;

                            if (_oneOfThemNotNull.All(property => nodeProperties.GetNestedValueOrDefault(property)?.ToString() == null))
                                return false;
                            
                            return _notNullProperties.All(property => nodeProperties.GetNestedValueOrDefault(property)?.ToString() != null);
                        })
                        .OnAnyFailure(x => throw new Exception($"Missing properties in metadata {string.Join(", ", _notNullProperties)}"));
                });
        }

        #endregion
    }
}

