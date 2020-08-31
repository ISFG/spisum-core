using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IFileService
    {
        #region Public Methods

        Task<List<string>> AddDocumentsToFile(string fileNodeId, string[] nodeIds);
        Task<NodeEntry> Borrow(string fileId, string group, string user);
        Task<NodeEntry> Close(string nodeId, string settleMethod, DateTime settleDate, string customSettleMethod = null, string settleReason = null);
        Task<NodeEntry> Create(string documentId);
        Task<List<string>> FavoriteAdd(List<string> fileIds);
        Task<List<string>> FavoriteRemove(List<string> favouriteIds);
        Task<List<NodeChildAssociationEntry>> GetDocuments(string fileId, bool includePath = false, bool includeProperties = false);
        Task<NodeEntry> ChangeFileMark(string nodeId, string fileMark);
        Task<NodeEntry> ChangeLocation(string nodeId, string location);
        Task<List<string>> Recover(List<string> nodeIds, string reason);
        Task<List<string>> RemoveDocumentsFromFile(string fileNodeId, string[] nodeIds);
        Task<NodeEntry> Return(string fileId);
        Task<NodeEntry> ShreddingCancelDiscard(string nodeId);
        Task<NodeEntry> ShreddingDiscard(string nodeId, DateTime date, string reason);
        Task ShreddingChange(string nodeId, string retentionMark);
        Task<List<string>> ToRepository(string group, List<string> files);
        Task<NodeEntry> Update(NodeUpdate nodeBody, ImmutableList<Parameter> queryParams);

        #endregion
    }
}