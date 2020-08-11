using System;
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
using ISFG.SpisUm.ClientSide.Models.File;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class FileShreddingAValidator : AbstractValidator<FileShreddingA>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public FileShreddingAValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Properties}, {AlfrescoNames.Includes.Path}", ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    
                    return _nodeEntry != null && _groupPaging != null;
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
                        .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                        .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.File}.");
                    
                    RuleFor(x => x)
                        .Must(x => 
                            _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.RepositoryStored, StringComparison.OrdinalIgnoreCase) == true || 
                            _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.RepositoryRented, StringComparison.OrdinalIgnoreCase) == true)
                        .OnAnyFailure(x => throw new BadRequestException($"File must be in {SpisumNames.Paths.RepositoryStored} or {SpisumNames.Paths.RepositoryRented} path."));      
                    
                    RuleFor(x => x.NodeId)
                        .Must(x =>
                        {
                            var nodeProperties = _nodeEntry.Entry.Properties.As<JObject>().ToDictionary();
                            var retentionMark = nodeProperties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionMark)?.ToString();
                            
                            return retentionMark == "S" || retentionMark == "V";
                        })
                        .WithMessage(x => "RetentionMark isn't S or V.");                 
                });
        }

        #endregion
    }    
}