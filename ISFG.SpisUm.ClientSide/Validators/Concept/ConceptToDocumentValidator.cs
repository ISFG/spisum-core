﻿using System;
using System.Collections.Immutable;
using System.Linq;
using FluentValidation;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Concept;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Validators.Concept
{
    public class ConceptToDocumentValidator : AbstractValidator<ConceptToDocument>
    {
        #region Fields

        private GroupPagingFixed _groupPaging;
        private NodeEntry _nodeEntry;

        #endregion

        #region Constructors

        public ConceptToDocumentValidator(IAlfrescoHttpClient alfrescoHttpClient, IIdentityUser identityUser)
        {
            RuleFor(o => o)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .MustAsync(async (context, cancellationToken) =>
                {
                    _nodeEntry = await alfrescoHttpClient.GetNodeInfo(context.NodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Path, ParameterType.QueryString)));
                    _groupPaging = await alfrescoHttpClient.GetPersonGroups(identityUser.Id);

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
                        .Must(x => _nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                        .WithMessage($"NodeId must be type of {SpisumNames.NodeTypes.Concept}.");
                });

            RuleFor(x => x.Body.SettleTo)
                .Must(x => x.HasValue ? x.Value.Date >= DateTime.UtcNow.Date : true)
                .When(w => w.Body != null)
                .WithMessage("SettleTo can't be in the past");

            RuleFor(x => x.Body.AttachmentsCount)
             .GreaterThanOrEqualTo(0)
             .When(w => w.Body != null)
             .WithMessage("AttachmentCount can't be negative number");

        }

        #endregion
    }
}
