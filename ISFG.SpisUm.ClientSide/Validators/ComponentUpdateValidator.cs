using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
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
    public class ComponentUpdateValidator : AbstractValidator<ComponentUpdate>
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IDocumentService _documentService;

        private readonly string[] _ignoreList = 
        {
            SpisumNames.Properties.Pid,
            SpisumNames.Properties.DocumentType,
            AlfrescoNames.ContentModel.Owner
        };

        private readonly INodesService _nodesService;
        private GroupPagingFixed _groupPaging;

        #endregion

        #region Constructors

        public ComponentUpdateValidator(IAlfrescoHttpClient alfrescoHttpClient, INodesService nodesService, IIdentityUser identityUser, IDocumentService documentService)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _nodesService = nodesService;
            _documentService = documentService;

            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    
                    return _groupPaging != null;
                })
                .WithName(x => nameof(x.NodeId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() => 
                { 
                    RuleFor(x => x)
                        .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");
                });

            RuleFor(x => x.Body.Properties ?? new Dictionary<string, object>())
                .Must(x => !_ignoreList.Any(x.ContainsKey))
                .WithMessage($"You are not allowed to pass properties {string.Join(", ", _ignoreList)}.");
            
            RuleFor(x => x)
                .MustAsync(CheckMainComponent);
        }

        #endregion

        #region Private Methods

        private async Task<bool> CheckMainComponent(NodeUpdate body, CancellationToken cancellation)
        {
            if (body.Body.Properties == null)
                return true;

            if (body.Body.Properties.ContainsKey(SpisumNames.Properties.ComponentType))
            {
                var inputValue = body.Body.Properties.GetNestedValueOrDefault(SpisumNames.Properties.ComponentType)?.ToString();
                if (!inputValue.Equals("main"))
                    return true;

                var response = await _alfrescoHttpClient.GetNodeParents(body.NodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.Components}')", ParameterType.QueryString)));

                foreach (var parent in response.List.Entries)
                {
                    var childrens =  await _documentService.GetComponents(parent.Entry.Id, false, true );

                    foreach (var children in childrens)
                    {
                        if (children.Entry.Id == body.NodeId)
                            continue;

                        var properties = children.Entry.Properties?.As<JObject>().ToDictionary();
                        var fileMetaType = properties.GetNestedValueOrDefault(SpisumNames.Properties.ComponentType)?.ToString();

                        if (fileMetaType != null && fileMetaType == SpisumNames.Component.Main)
                            throw new BadRequestException(ErrorCodes.V_COMPONENT_TYPE);
                    }
                }
            }

            return true;
        }

        #endregion
    }
}