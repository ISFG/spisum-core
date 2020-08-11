using System;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.GsApi.GsApi;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IRepositoryService
    {
        #region Public Methods

        public Task CompleteRecord(string nodeId);
        public Task<RecordEntry> CreateDocumentRecord(string filePlan, string fileMark, string nodeId);
        public Task<RecordEntry> CreateFileRecord(string filePlan, string fileMark, string nodeId);
        public Task CutOff(string nodeId);
        public Task<NodeEntry> ChangeRetention(string rmNodeId, string retentionMark, int retentionPeriod, DateTime? settleDate = null, string fileMark = null);
        public Task UncompleteRecord(string nodeId);
        public Task UndoCutOff(string nodeId);

        #endregion
    }
}
