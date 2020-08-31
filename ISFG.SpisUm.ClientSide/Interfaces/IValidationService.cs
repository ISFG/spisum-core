using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using Microsoft.AspNetCore.Http;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IValidationService
    {
        #region Public Methods
        List<string> GetAllowedExtensions();
        string[] GetFileExtensions();
        Task<NodeEntry> UpdateFileSecurityFeatures(string fileId);
        Task<NodeEntry> UpdateDocumentSecurityFeatures(string documentId);
        Task<NodeEntry> UpdateFileOutputFormat(string fileId);
        Task<NodeEntry> UpdateDocumentOutputFormat(string documentId);        
        Task<NodeEntry> CheckOutputFormat(string nodeId);
        Task<NodeEntry> CheckOutputFormat(string nodeId, byte[] file, string mimeType);
        Task<NodeEntry> CheckOutputFormat(string nodeId, IFormFile componentFile);
        Task<Dictionary<string, object>> GetCheckOutputFormatProperties(byte[] file, string mimeType);
        Task<NodeEntry> ConvertToOutputFormat(string documentId, string componentId, string reason, string organization);
        #endregion
    }
}
