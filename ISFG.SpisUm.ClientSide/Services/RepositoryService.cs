using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.GsApi.GsApi;
using ISFG.Alfresco.Api.Models.WebScripts;
using ISFG.Common.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class RepositoryService : IRepositoryService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;

        #endregion

        #region Constructors

        public RepositoryService(IAlfrescoHttpClient alfrescoHttpClient)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
        }

        #endregion

        #region Implementation of IRepositoryService

        public async Task CompleteRecord(string nodeId)
        {
            await _alfrescoHttpClient.CompleteRecord(nodeId);
        }

        public async Task<RecordEntry> CreateDocumentRecord(string filePlan, string fileMark, string nodeId)
        {
            return await CreateRecord(filePlan, fileMark, nodeId, SpisumNames.NodeTypes.DocumentRM, SpisumNames.Associations.DocumentInRepository);
        }

        public async Task<RecordEntry> CreateFileRecord(string filePlan, string fileMark, string nodeId)
        {
            return await CreateRecord(filePlan, fileMark, nodeId, SpisumNames.NodeTypes.FileRM, SpisumNames.Associations.FileInRepository);
        }

        public async Task CutOff(string nodeId)
        {
            await _alfrescoHttpClient.ExecutionQueue(new ExecutionQueue
            {
                Name = "cutoff",
                NodeRef = $"workspace://SpacesStore/{nodeId}"
            });
        }

        public async Task<NodeEntry> ChangeRetention(string rmNodeId, string retentionMark, int retentionPeriod, DateTime? settleDate = null, string fileMark = null)
        {
            try { await UndoCutOff(rmNodeId); } catch { }

            await UncompleteRecord(rmNodeId);

            var body = new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.RetentionMark, retentionMark)
                .AddProperty(SpisumNames.Properties.RetentionMode, $"{retentionMark}/{retentionPeriod}")
                .AddProperty(SpisumNames.Properties.RetentionPeriod, retentionPeriod);

            if (settleDate != null)
                body.AddProperty(SpisumNames.Properties.RetentionPeriod, new DateTime(settleDate.Value.Year + 1 + retentionPeriod, 1, 1).ToAlfrescoDateTimeString());

            if (fileMark != null)
                body.AddProperty(SpisumNames.Properties.FileMark, fileMark);

            var node = await _alfrescoHttpClient.UpdateNode(rmNodeId, body);                

            await CompleteRecord(rmNodeId);

            try { await CutOff(rmNodeId); } catch { }

            return node;
        }
        public async Task UncompleteRecord(string nodeId)
        {
            await _alfrescoHttpClient.ExecutionQueue(new ExecutionQueue
            {
                Name = "undeclareRecord",
                NodeRef = $"workspace://SpacesStore/{nodeId}"
            });
        }

        public async Task UndoCutOff(string nodeId)
        {
            await _alfrescoHttpClient.ExecutionQueue(new ExecutionQueue
            {
                Name = "unCutoff",
                NodeRef = $"workspace://SpacesStore/{nodeId}"
            });
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets file plan information
        /// </summary>
        /// <param name="filePlanId">Id of fileplan. If null it will use default alias -filePlan-</param>
        /// <returns>FilePlanEntry information.</returns>
        public async Task<FilePlanEntry> GetFilePlan(string filePlanId = null)
        {
            filePlanId ??= AlfrescoNames.Aliases.FilePlan;

            return await _alfrescoHttpClient.GetFilePlan(filePlanId);
        }

        #endregion

        #region Private Methods

        private async Task<RecordEntry> CreateRecord(string filePlan, string fileMark, string nodeId, string nodeType, string assocType)
        {
            var folderInfo = await GetRMFolder(filePlan, fileMark);
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();

            int.TryParse(properties.GetNestedValueOrDefault(SpisumNames.Properties.RetentionPeriod)?.ToString(),
                out int retentionPeriod);
            DateTime.TryParse(properties.GetNestedValueOrDefault(SpisumNames.Properties.SettleDate)?.ToString(),
                out DateTime settleDate);

            var shreddingDate = new DateTime(settleDate.Year + 1 + retentionPeriod, 1, 1);

            var body = new RMNodeBodyCreate
            {
                Name = nodeInfo?.Entry?.Name,
                NodeType = nodeType,
                Properties = new Dictionary<string, string>
                {
                    { SpisumNames.Properties.Ref, nodeId }
                }
            };

            if (properties.ContainsKey(SpisumNames.Properties.Pid) && properties.ContainsKey(SpisumNames.Properties.PidRef))
                properties.Remove(SpisumNames.Properties.PidRef);

            properties.ForEach(x =>
            {
                var (key, value) = x;

                if (!key.StartsWith("ssl:"))
                    return;

                if (key.Equals(SpisumNames.Properties.Pid))
                {
                    body.Properties.Add(SpisumNames.Properties.PidRef, $"{value}");
                    body.Properties.Add(SpisumNames.Properties.Pid, $"RM-{value}");
                    return;
                }

                // Problems only with datetime for now
                if (DateTime.TryParse(value?.ToString(), out DateTime datetime))
                    body.Properties.Add(key, datetime.ToAlfrescoDateTimeString());
                else
                    body.Properties.Add(key, $"{value}");
            });

            var node = await _alfrescoHttpClient.CreateRecord(folderInfo?.Entry?.Id, body);

            // Create secondary children to orig document/file. It cannot be set from repository to RM
            await _alfrescoHttpClient.CreateNodeSecondaryChildren(node?.Entry?.Id, new ChildAssociationBody
            {
                    AssocType = assocType,
                    ChildId = nodeId
                });

            // Set record as complete
            await _alfrescoHttpClient.CompleteRecord(node?.Entry?.Id);

            // Call retention date
            await _alfrescoHttpClient.ExecutionQueue(new ExecutionQueue
            {
                Name = "editDispositionActionAsOfDate",
                NodeRef = $"workspace://SpacesStore/{node?.Entry?.Id}",
                Params = new ExecutionQueueParams
                {
                    AsOfDate = new ExecutionQueueParamsAsOfDate
                    {
                        Iso8601 = shreddingDate.ToAlfrescoDateTimeString()
                    }
                }
            });

            return node;
        }

        private async Task<NodeEntry> GetRMFolder(string fileId, string fileMark)
        {
            return await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.Root, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.RelativePath, SpisumNames.Paths.RMShreddingPlanFolderContents(fileId, fileMark), ParameterType.QueryString)));
        }

        #endregion
    }
}