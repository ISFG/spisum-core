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
using ISFG.SpisUm.ClientSide.Models.Document;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators.Concept
{
    public class DocumentComponentUpdateContentValidator : AbstractValidator<DocumentComponentUpdateContent>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;
        private NodeAssociationPaging _nodeParents;

        #endregion

        #region Constructors

        public DocumentComponentUpdateContentValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    _nodeParents = await alfrescoHttpClient.GetNodeParents(context.ComponentId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                        .Add(new Parameter(AlfrescoNames.Headers.Where,
                            $"(assocType='{SpisumNames.Associations.Components}')"
                            , ParameterType.QueryString)));

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
                        .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                        .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.Document}.");

                    RuleFor(x => x)
                        .Must(c => _nodeParents?.List?.Entries?.Any(y => y?.Entry?.Id == c.NodeId) ?? false)
                        .WithMessage("Provided component is not associated with nodeId or cannot be canceled.");

                    RuleFor(x => x)
                        .Must(x =>
                        {
                            if (_nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.Concept)
                                return true;

                            if (_nodeEntry?.Entry?.IsLocked == true)
                                return false;

                            var properties = _nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
                            var form = properties.GetNestedValueOrDefault(SpisumNames.Properties.Form)?.ToString();
                            var senderType = properties.GetNestedValueOrDefault(SpisumNames.Properties.SenderType)?.ToString();
                            var documentType = properties.GetNestedValueOrDefault(SpisumNames.Properties.DocumentType)?.ToString();

                            if (form == SpisumNames.Form.Analog || documentType == "technicalDataCarries" || form == SpisumNames.Form.Digital && senderType == SpisumNames.SenderType.Own)
                                return true;

                            return false;
                        })
                        .OnAnyFailure(x => throw new BadRequestException("Adding component is not allowed."));
                });
        }

        #endregion
    }
}
