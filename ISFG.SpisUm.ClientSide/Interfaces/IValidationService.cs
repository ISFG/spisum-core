using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using Microsoft.AspNetCore.Http;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IValidationService
    {
        #region Public Methods

        Task<NodeEntry> ConvertToOutputFormat(string documentId, string componentId, string reason, string organization);
        Task<NodeEntry> CheckOutputFormat(string nodeId);
        Task<NodeEntry> CheckOutputFormat(string nodeId, byte[] file, string mimeType);
        Task<NodeEntry> CheckOutputFormat(string nodeId, IFormFile componentFile);

        #endregion
    }
}
