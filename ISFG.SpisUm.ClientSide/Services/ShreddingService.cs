using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class ShreddingService : IShreddingService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IIdentityUser _identityUser;
        private readonly INodesService _nodesService;

        #endregion

        #region Constructors

        public ShreddingService(IAlfrescoHttpClient alfrescoHttpClient, INodesService nodesService, IIdentityUser identityUser)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _identityUser = identityUser;
            _nodesService = nodesService;
        }

        #endregion

        #region Implementation of IShreddingService

        public async Task<NodeEntry> ShreddingProposalCreate(string name, List<string> ids)
        {
            var nodes = new List<NodeEntry>();

            await ids.ForEachAsync(async x =>
            {
                #region Validations

                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(x);
                nodes.Add(nodeInfo);

                var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
                var discardReason = properties.GetNestedValueOrDefault(SpisumNames.Properties.DiscardReason)?.ToString();
                var discardTo = properties.GetNestedValueOrDefault(SpisumNames.Properties.DiscardTo)?.ToString();
                var discardDate = properties.GetNestedValueOrDefault(SpisumNames.Properties.DiscardDate)?.ToString();
                var borrower = properties.GetNestedValueOrDefault(SpisumNames.Properties.Borrower)?.ToString();
                var proposalName = properties.GetNestedValueOrDefault(SpisumNames.Properties.ProposalName)?.ToString();

                if (!string.IsNullOrWhiteSpace(proposalName))
                    throw new BadRequestException("", "One or more of the ids cannot be used because they were used previously.");

                if (!string.IsNullOrWhiteSpace(discardReason) ||
                    !string.IsNullOrWhiteSpace(discardTo) ||
                    !string.IsNullOrWhiteSpace(discardDate) ||
                    !string.IsNullOrWhiteSpace(borrower))
                    throw new BadRequestException("", "One or more of ids is borrowed or removed of shredding plan");

                #endregion
            });

            var shreddingPlanCreate = await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new FormDataParam(new byte[] {01}),
                    ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.NodeType, SpisumNames.NodeTypes.ShreddingProposal, ParameterType.GetOrPost))
                        .Add(new Parameter(AlfrescoNames.Headers.Name, IdGenerator.GenerateId(), ParameterType.GetOrPost))
                        .Add(new Parameter(AlfrescoNames.Headers.RelativePath, SpisumNames.Paths.RepositoryShreddingProposal, ParameterType.GetOrPost))
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.Properties, ParameterType.QueryString))
                        .Add(new Parameter(SpisumNames.Properties.Name, name, ParameterType.GetOrPost))
                        .Add(new Parameter(SpisumNames.Properties.AssociationCount, nodes.Count, ParameterType.GetOrPost))
                        .Add(new Parameter(SpisumNames.Properties.CreatedDate, DateTime.UtcNow.ToAlfrescoDateTimeString(), ParameterType.GetOrPost))
                        .Add(new Parameter(SpisumNames.Properties.Author, _identityUser.Id, ParameterType.GetOrPost))
                    );

            var shreddingPlan = await _alfrescoHttpClient.GetNodeInfo(shreddingPlanCreate?.Entry?.Id);

            var shredding = shreddingPlan?.Entry?.Properties.As<JObject>().ToDictionary();
            var pid = shredding.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString();

            await nodes.ForEachAsync(async x =>
            {
                await _nodesService.TryUnlockNode(x?.Entry?.Id);

                await _alfrescoHttpClient.CreateNodeSecondaryChildren(shreddingPlan?.Entry?.Id, new ChildAssociationBody
                {
                    AssocType = SpisumNames.Associations.ShreddingObjects,
                    ChildId = x?.Entry?.Id
                });

                await _alfrescoHttpClient.UpdateNode(x?.Entry?.Id, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.ProposalName, pid)
                );

                await _alfrescoHttpClient.NodeLock(x?.Entry?.Id, new NodeBodyLock
                {
                    Type = NodeBodyLockType.FULL
                });
            });

            return shreddingPlan;
        }

        #endregion
    }
}