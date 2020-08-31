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
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators
{
    public class DocumentComponentCreateValidator : AbstractValidator<DocumentComponentCreate>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public DocumentComponentCreateValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.IsLocked}" , ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);
                    
                    return _groupPaging != null && _nodeEntry?.Entry?.Id != null;
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
                        .Must(x =>
                        {
                            if (_nodeEntry.Entry.NodeType == SpisumNames.NodeTypes.Concept)
                                return true;

                            var properties = _nodeEntry?.Entry?.Properties?.As<JObject>().ToDictionary();
                            var senderType = properties.GetNestedValueOrDefault(SpisumNames.Properties.SenderType)?.ToString();
                            var isPathMailroom = _nodeEntry?.Entry?.Path?.Name?.StartsWith(AlfrescoNames.Prefixes.Path + SpisumNames.Paths.MailRoomUnfinished,
                                StringComparison.OrdinalIgnoreCase);

                            if (isPathMailroom != true && senderType != SpisumNames.SenderType.Own)
                                return false;

                            var form = properties.GetNestedValueOrDefault(SpisumNames.Properties.Form)?.ToString();
                            var documentType = properties.GetNestedValueOrDefault(SpisumNames.Properties.DocumentType)?.ToString();
                            
                            if (_nodeEntry?.Entry?.IsLocked == true)
                                return false;

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