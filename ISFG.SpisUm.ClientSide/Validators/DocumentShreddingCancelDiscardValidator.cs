using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Document;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentShreddingCancelDiscardValidator : AbstractValidator<DocumentShreddingCancelDiscard>
    {
        #region Fields

        private string _cutOffDate;
        private string _discardTo;

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;
        private NodeAssociationPaging _parentRM;

        #endregion

        #region Constructors

        public DocumentShreddingCancelDiscardValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties}, {AlfrescoNames.Includes.Path}",
                            ParameterType.QueryString)));
                    _parentRM = await alfrescoHttpClient.GetNodeParents(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.DocumentInRepository}')", ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.MaxItems, "1", ParameterType.QueryString)));

                    

                    if (_parentRM?.List?.Entries?.Count == 1)
                    {
                        var documentInfoRM = await alfrescoHttpClient.GetNodeInfo(_parentRM?.List?.Entries?.FirstOrDefault()?.Entry?.Id);
                        var rmdocumentProperties = documentInfoRM?.Entry?.Properties?.As<JObject>().ToDictionary();
                        _cutOffDate = rmdocumentProperties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.CutOffDate)?.ToString();
                    }

                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

                    return _nodeEntry != null && _groupPaging != null && _parentRM != null;
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
                        .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                        .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.Document}.");

                    RuleFor(x => x.NodeId)
                        .Must(x => _parentRM?.List?.Entries.Count == 1)
                        .WithMessage(x => "Document is not in record management");

                    RuleFor(x => x.NodeId)
                        .Must(x => !string.IsNullOrWhiteSpace(_cutOffDate))
                        .WithMessage(x => "Cut off date is not set yet");

                    RuleFor(x => x.NodeId)
                        .Must(x =>
                        {
                            var documentProperties = _nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
                            _discardTo = documentProperties.GetNestedValueOrDefault(SpisumNames.Properties.DiscardTo)?.ToString();

                            return !string.IsNullOrWhiteSpace(_discardTo);
                        })
                        .WithMessage(x => $"{SpisumNames.Properties.DiscardTo} property is not set");
                });
        }

        #endregion
    }
}