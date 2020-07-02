using System;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface ITransactionHistoryService
    {
        #region Public Methods

        string GetDateFormatForTransaction(DateTime datetime);
        Task LogForSignature(string nodeId, string fileId, string nextGroup, string nextOwner);
        Task LogHandover(string nodeId, string nextGroup, string nextOwner);
        Task LogHandoverAccept(string nodeId, bool isRepository);
        Task LogHandoverCancel(string nodeId, NodeEntry nodeEntry);
        Task LogHandoverDecline(string nodeId, NodeEntry nodeEntry);

        #endregion
    }
}
