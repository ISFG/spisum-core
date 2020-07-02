using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.ClientSide.Enums;
using Microsoft.AspNetCore.Http;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IComponentService
    {
        #region Public Methods

        Task<List<string>> CancelComponent(string nodeId, List<string> componentsId);
        Task<NodeEntry> CreateVersionedComponent(string nodeId, IFormFile component);
        Task<string> GenerateComponentPID(string parentId, string separator, GeneratePIDComponentType type);
        Task<string> GetComponentDocument(string nodeId);
        Task UpdateAssociationCount(string nodeId);
        Task<NodeEntry> UpgradeDocumentVersion(string nodeId);
        Task<NodeEntry> UploadNewVersionComponent(string nodeId, string componentId, IFormFile component);
        Task<NodeEntry> UploadNewVersionComponent(string nodeId, string componentId, byte[] component, string componentName, string mimeType);

        #endregion
    }
}
