using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.ClientSide.Enums;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Microsoft.AspNetCore.Http;
using RestSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IComponentService
    {
        #region Public Methods
        Task<NodeEntry> UpdateComponent(ComponentUpdate body, IImmutableList<Parameter> parameters = null, bool updateMainFileVersion = true);
        Task<NodeEntry> UpdateComponent(string componentId, IImmutableList<Parameter> parameters = null, bool updateMainFileVersion = true);
        Task<NodeEntry> UpdateComponent(string componentId, NodeBodyUpdate body, bool updateMainFileVersion = true);
        Task<List<string>> CancelComponent(string nodeId, List<string> componentsId);
        Task<NodeEntry> CreateVersionedComponent(string nodeId, IFormFile component, ImmutableList<Parameter> additionalParameters = null);
        Task<NodeEntry> CreateVersionedComponent(string nodeId, FormDataParam component, string fileName, ImmutableList<Parameter> additionalParameters = null);
        Task<string> GenerateComponentPID(string parentId, string separator, GeneratePIDComponentType type);
        Task<string> GetComponentDocument(string nodeId);
        Task UpdateAssociationCount(string nodeId);
        Task<NodeEntry> UpgradeDocumentVersion(string nodeId, bool isMajorVersion);
        Task<NodeEntry> UploadNewVersionComponent(string nodeId, string componentId, IFormFile component, IImmutableList<Parameter> additionalParameters = null);
        Task<NodeEntry> UploadNewVersionComponent(string nodeId, string componentId, byte[] component, string componentName, string mimeType, IImmutableList<Parameter> additionalParameters = null);        
        #endregion
    }
}
