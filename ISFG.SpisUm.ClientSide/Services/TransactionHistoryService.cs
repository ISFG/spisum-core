using System;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Data.Models;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class TransactionHistoryService : ITransactionHistoryService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAuditLogService _auditLogService;

        #endregion

        #region Constructors

        public TransactionHistoryService(IAlfrescoHttpClient alfrescoHttpClient, IAuditLogService auditLogService)
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _auditLogService = auditLogService;
        }

        #endregion

        #region Implementation of ITransactionHistoryService

        public string GetDateFormatForTransaction(DateTime datetime)
        {
            return datetime.ToLocalTime().ToString("dd.MM.yyyy");
        }

        public async Task LogForSignature(string nodeId, string fileId, string nextGroup, string nextOwner)
        {
            try
            {
                var nodeEntry = await _alfrescoHttpClient.GetNodeInfo(nodeId);

                var personEntry = await _alfrescoHttpClient.GetPerson(nextOwner);
                var groupEntry = await _alfrescoHttpClient.GetGroup(nextGroup);
                var pid = nodeEntry?.GetPid();

                // Audit log for a document
                await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, pid, NodeTypeCodes.Dokument, EventCodes.PostoupeniAgende,
                    string.Format(TransactinoHistoryMessages.DocumentForSignature, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));

                // Audit log for a file
                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Document, pid, NodeTypeCodes.Dokument, EventCodes.PostoupeniAgende,
                        string.Format(TransactinoHistoryMessages.DocumentForSignature, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        public async Task LogHandover(string nodeId, string nextGroup, string nextOwner)
        {
            try
            {
                var nodeEntry = await _alfrescoHttpClient.GetNodeInfo(nodeId);
                
                var groupEntry = await _alfrescoHttpClient.GetGroup(nextGroup);
                var pid = nodeEntry?.GetPid();
                
                if (!string.IsNullOrWhiteSpace(nextOwner))
                {
                    var personEntry = await _alfrescoHttpClient.GetPerson(nextOwner);

                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    {
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, pid, NodeTypeCodes.Dokument, EventCodes.PostoupeniAgende,
                            string.Format(TransactinoHistoryMessages.DocumentHandover, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));
                        return;
                    }
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                    {
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Concept, pid, NodeTypeCodes.Dokument, EventCodes.PostoupeniAgende,
                            string.Format(TransactinoHistoryMessages.ConceptHandover, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));
                        return;
                    }
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, pid, NodeTypeCodes.Spis, EventCodes.PostoupeniAgende,
                            string.Format(TransactinoHistoryMessages.FileHandover, personEntry?.Entry?.DisplayName, groupEntry?.Entry?.DisplayName));
                }
                else
                {
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    {
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, pid, NodeTypeCodes.Dokument, EventCodes.PostoupeniAgende,
                            string.Format(TransactinoHistoryMessages.DocumentHandoverGroup, groupEntry?.Entry?.DisplayName));
                        return;
                    }
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                    {
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Concept, pid, NodeTypeCodes.Dokument, EventCodes.PostoupeniAgende,
                            string.Format(TransactinoHistoryMessages.ConceptHandoverGroup, groupEntry?.Entry?.DisplayName));
                        return;
                    }
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, pid, NodeTypeCodes.Spis, EventCodes.PostoupeniAgende,
                            string.Format(TransactinoHistoryMessages.FileHandoverGroup, groupEntry?.Entry?.DisplayName));
                }
               
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        public async Task LogHandoverAccept(string nodeId, bool isRepository)
        {
            try
            {
                var nodeEntry = await _alfrescoHttpClient.GetNodeInfo(nodeId);

                var pid = nodeEntry?.GetPid();

                if (!isRepository)
                {
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    {
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, pid, NodeTypeCodes.Dokument, EventCodes.ZmenaZpracovatele,
                            TransactinoHistoryMessages.DocumentHandoverAccept);
                        return;
                    }
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                    {
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Concept, pid, NodeTypeCodes.Dokument, EventCodes.ZmenaZpracovatele,
                            TransactinoHistoryMessages.ConceptHandoverAccept);
                        return;
                    }
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, pid, NodeTypeCodes.Spis, EventCodes.ZmenaZpracovatele,
                            TransactinoHistoryMessages.FileHandoverAccept);
                }
                else
                {
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    {
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, pid, NodeTypeCodes.Dokument, EventCodes.PrevzetiNaSpisovnu,
                            TransactinoHistoryMessages.DocumentHandoverRepositoryAccept);
                        return;
                    }
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                    {
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Concept, pid, NodeTypeCodes.Dokument, EventCodes.PrevzetiNaSpisovnu,
                            TransactinoHistoryMessages.ConceptHandoverRepositoryAccept);
                        return;
                    }
                    if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                        await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, pid, NodeTypeCodes.Spis, EventCodes.PrevzetiNaSpisovnu,
                            TransactinoHistoryMessages.FileHandoverRepositoryAccept);
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        public async Task LogHandoverCancel(string nodeId, NodeEntry nodeEntry)
        {
            try
            {
                var pid = nodeEntry?.GetPid();

                if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                {
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Concept, pid, NodeTypeCodes.Dokument, EventCodes.VraceniZAgendy,
                        TransactinoHistoryMessages.ConceptHandoverCancel);
                    return;
                }
                if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                {
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, pid, NodeTypeCodes.Dokument, EventCodes.VraceniZAgendy,
                        TransactinoHistoryMessages.DocumentHandoverCancel);
                    return;
                }
                if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, pid, NodeTypeCodes.Spis, EventCodes.VraceniZAgendy,
                        TransactinoHistoryMessages.FileHandoverCancel);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        public async Task LogHandoverDecline(string nodeId, NodeEntry nodeEntry)
        {
            try
            {
                var pid = nodeEntry?.GetPid();

                if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Concept)
                {
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Concept, pid, NodeTypeCodes.Dokument, EventCodes.VraceniZAgendy,
                        TransactinoHistoryMessages.ConceptHandoverDecline);
                    return;
                }
                if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                {
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.Document, pid, NodeTypeCodes.Dokument, EventCodes.VraceniZAgendy,
                        TransactinoHistoryMessages.DocumentHandoverDecline);
                    return;
                }                
                if (nodeEntry?.Entry?.NodeType == SpisumNames.NodeTypes.File)
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.File, pid, NodeTypeCodes.Spis, EventCodes.VraceniZAgendy,
                        TransactinoHistoryMessages.FileHandoverDecline);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        #endregion
    }
}
