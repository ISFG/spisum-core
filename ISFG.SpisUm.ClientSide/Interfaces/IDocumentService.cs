using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.SpisUm.ClientSide.Models.Nodes;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IDocumentService
    {
        #region Public Methods

        Task<NodeEntry> Borrow(string documentId, string group, string user, bool moveDocument = true);
        Task<NodeEntry> Create(string nodeType, string relativePath, string documentForm = null, string nodeId = null);
        Task<NodeEntry> DocumentReturnForRework(string documentId, string reason);
        Task<List<string>> FavoriteAdd(List<string> documentId);
        Task<List<string>> FavoriteRemove(List<string> favouriteId);
        Task FavoriteRemove(string favouriteId);
        Task<List<NodeChildAssociationEntry>> GetComponents(string documentId, bool includePath = false, bool includeProperties = false);
        Task<string> GetDocumentFileId(string documentId);
        Task<NodeEntry> ChangeFileMark(string nodeId, string fileMark, ShreddingPlanItemModel plan = null);
        Task<NodeEntry> ChangeLocation(string nodeId, string location, bool? isMemberOfRepository = null);
        Task<bool> IsDocumentInFile(string documentId);
        Task<List<string>> Recover(List<string> nodeIds, string reason);
        Task<NodeEntry> Register(NodeUpdate body, GenerateSsid ssidConfiguration);
        Task<NodeEntry> Return(string documentId, bool moveDocument = true);
        Task<NodeEntry> Revert(string nodeId, string versionId);
        Task<NodeEntry> Settle(string nodeId, string settleMethod, DateTime settleDate, string movePath, string customSettleMethod = null, string settleReason = null);
        Task<NodeEntry> SettleCancel(string nodeId, string reason);

        Task<NodeEntry> ShreddingCancelDiscard(string nodeId);
        Task<NodeEntry> ShreddingDiscard(string nodeId, DateTime date, string reason, DateTime discardDate);
        Task ShreddingChange(string nodeId, string retentionMark);
        Task<List<string>> ToRepository(string group, List<string> documents, bool moveDocument = true);

        #endregion
    }
}