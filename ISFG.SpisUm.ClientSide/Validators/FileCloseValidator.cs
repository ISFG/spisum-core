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
using ISFG.SpisUm.ClientSide.Models.Nodes;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class FileCloseValidator : AbstractValidator<FileClose>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public FileCloseValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.Properties}", ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

                    return _groupPaging != null && _nodeEntry?.Entry?.Id != null;
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
                        .Must(x => _nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.File)
                        .WithMessage(x => $"Provided nodeId must be NodeType {SpisumNames.NodeTypes.File}");
                });
            
            RuleFor(x => x)
                .Must(x =>
                {
                    if (x?.Body?.SettleMethod != "jinyZpusob") 
                        return true;
                    
                    if (x?.Body?.CustomSettleMethod == null || x?.Body?.SettleReason == null)
                        return false;
                        
                    if (x.Body.CustomSettleMethod.Length < 4 || x.Body.SettleReason.Length < 4)
                        throw new BadRequestException(ErrorCodes.V_MIN_TEXT);

                    x.Body.CustomSettleMethod = x.Body.CustomSettleMethod.CutLength(30);
                    x.Body.SettleReason = x.Body.SettleReason.CutLength(30);
                    
                    return true;
                })
                .WithName(x => nameof(x.Body.SettleMethod))
                .WithMessage("You have to fill CustomSettleMethod and SettleReason.");
        }

        #endregion
    }
}