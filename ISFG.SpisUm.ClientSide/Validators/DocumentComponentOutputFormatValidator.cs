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
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentComponentOutputFormatValidator : AbstractValidator<DocumentComponentOutputFormat>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public DocumentComponentOutputFormatValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.ComponentId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

                    return _nodeEntry?.Entry?.Id != null && _groupPaging != null;
                })
                .WithName(x => nameof(x.ComponentId))
                .WithMessage("Something went wrong with alfresco server.")
                .DependentRules(() =>
                {
                    RuleFor(x => x)
                        .Must(y => _groupPaging?.List?.Entries?.Any(q => q.Entry.Id == identityUser.RequestGroup) ?? false)
                        .WithName(x => "Group")
                        .WithMessage($"User isn't member of group {identityUser.RequestGroup}.");

                    RuleFor(x => x.ComponentId)
                        .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Component || 
                                   _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Email || 
                                   _nodeEntry?.Entry?.NodeType == AlfrescoNames.ContentModel.Content ||
                                   _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.EmailComponent ||
                                   _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.DataBoxComponent)
                        .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.Component}.");
                    
                    RuleFor(x => x)
                        .Must(x =>
                        {
                            var properties = _nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
                            var fileIsInOutputFormat = properties.GetNestedValueOrDefault(SpisumNames.Properties.FileIsInOutputFormat)?.ToString();

                            return fileIsInOutputFormat != "yes";
                        })
                        .WithMessage("Component is already in output format.");
                });
            
            RuleFor(x => x.Body.Reason)
                .MinimumLength(4)
                .MaximumLength(30)
                .NotNull()
                .When(x => x.Body != null)
                .WithName(x => nameof(x.Body.Reason));                
                
        }

        #endregion
    }
}