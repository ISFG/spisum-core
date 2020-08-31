using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.Sites;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface INodesService
    {
        #region Public Methods

        Task AcceptOwner(string nodeId);
        bool AreNodeTypesValid(List<NodeEntry> nodeEntries, string nodeType);
        Task CancelNode(string nodeId, string reason);        
        Task CreateAssociation(string parentId, string childId, string assocType);
        Task CreateAssociation(string parentId, List<string> childsId, string assocType);
        Task<NodeEntry> CreateFolder(string relativePath);
        Task<NodeEntry> CreateNodeAsAdmin(string parentNodeId, object body, IImmutableList<Parameter> parameters = null);
        Task<NodeEntry> CreatePermissions(string nodeId, string prefix = null, string owner = null, bool isInheritanceEnabled = false);
        Task<NodeEntry> CreatePermissionsAsAdmin(string nodeId, string prefix = null, string owner = null, bool isInheritanceEnabled = false);
        Task<NodeEntry> CreatePermissionsWithoutPostfixes(string nodeId, string prefix = null, string owner = null, bool isInheritanceEnabled = false);
        Task CreateSecondaryChildrenAsAdmin(string parentId, ChildAssociationBody body);
        ImmutableList<Parameter> CloneProperties(Dictionary<string, object> properties, ImmutableList<Parameter> parameters, bool pidToPidRef = false);
        Task DeclineOwner(string nodeId, bool cancelAction);
        Task DeleteNodeAsAdmin(string nodeId);
        Task DeleteNodePermanent(string nodeId, bool permanent = true);
        Task DeleteSecondaryChildrenAsAdmin(string parentId, string childrenId, IImmutableList<Parameter> parameters = null);
        Task<FileContentResult> Download(List<string> nodesId, string zipName);
        Task<List<string>> FoundNode(string[] nodeIds, string nodeType);
        Task<NodeEntry> GenerateSsid(string nodeId, GenerateSsid ssid);
        Task<List<NodeChildAssociationEntry>> GetChildren(string nodeId, bool includeProperties = false, bool includePath = false);
        Task<NodeBodyUpdate> GetNodeBodyUpdateWithPermissions(string nodeId);
        Task<NodeBodyUpdate> GetNodeBodyUpdateWithPermissionsAsAdmin(string nodeId);
        Task<List<NodeEntry>> GetNodesInfo(List<string> nodesId);
        Task<List<NodeAssociationPaging>> GetParents(List<string> nodeIds);
        Task<List<NodeAssociationEntry>> GetParentsByAssociation(string nodeId, List<string> associations, IImmutableList<Parameter> parameters = null);
        Task<List<NodeChildAssociationEntry>> GetSecondaryChildren(string nodeId, string association, bool includePath = false, bool includeProperties = false);
        Task<List<NodeChildAssociationEntry>> GetSecondaryChildren(string nodeId, List<string> associations, bool includePath = false, bool includeProperties = false);
        List<ShreddingPlanModel> GetShreddingPlans();
        Task<bool> IsMemberOfRepository(string requestGroup);
        Task<bool> IsNodeLocked(string nodeId);
        Task LockAll(string nodeId);
        Task LockAll(string nodeId, List<string> excludedNodeTypes);
        Task LostDestroyedNode(string nodeId, string reason);
        Task<List<NodeEntry>> MoveAllComponets(string nodeId);
        Task<List<NodeEntry>> MoveByPath(List<string> nodesId, string targetPath);
        Task<NodeEntry> MoveByPath(string nodeId, string targetPath);
        Task<NodeEntry> MoveByPathAsAdmin(string nodeId, string targetPath);
        Task<NodeEntry> MoveByPathCached(string nodeId, string destinationPath);
        Task MoveForSignature(string nodeId, string group, string signGroup, string signUser);
        Task<NodeEntry> MoveFromSignature(string nodeId);
        Task MoveHandOverPath(string nodeId, string group, string nextGroup, string nextOwner);
        Task<List<NodeEntry>> MoveChildren(string nodeId, string targetNodeId);
        Task<List<NodeEntry>> MoveChildrenByPath(string nodeId, string targetPath);
        Task MoveOwner(string nodeId, string group, bool isMemberOfDispatch = false, bool isDecline = false);
        Task<NodeEntry> NodeLockAsAdmin(string nodeId);
        Task<NodeEntry> NodeUnlockAsAdmin(string nodeId);
        Task<NodeEntry> OpenFile(string nodeId, string reason);
        Task<List<string>> Recover(List<string> nodeIds, string reason, string nodeType, string allowedPath);
        Task<NodeEntry> RemoveAllVersions(string nodeId);
        NodeBodyUpdate SetPermissions(string prefix = null, string owner = null, bool isInheritanceEnabled = false);
        NodeBodyUpdate SetPermissionsWithoutPostfixes(string prefix = null, string owner = null, bool isInheritanceEnabled = false);
        Task TraverseAllChildren<T>(string nodeId, Func<string, Task<T>> funcAction);
        Task<NodeEntry> TryUnlockNode(string nodeId);
        Task UnlockAll(string nodeId);
        Task<NodeEntry> UnlockNodeAsAdmin(string nodeId);
        Task<NodeEntry> Update(NodeUpdate body, IImmutableList<Parameter> parameters = null);
        Task UpdateForSignaturePermisionsAll(string nodeId, string user, string group);
        Task<NodeEntry> UpdateHandOverPermissions(string nodeId, string nextGroup = null, string nextOwner = null);
        Task UpdateHandOverPermissionsAll(string nodeId, string nextGroup = null, string nextOwner = null);
        Task<NodeEntry> UpdateHandOverRepositoryPermissions(string nodeId, string nextGroup);
        Task UpdateHandOverRepositoryPermissionsAll(string nodeId, string group);
        Task<NodeEntry> UpdateNodeAsAdmin(string nodeId, NodeBodyUpdate updateBody);
        Task<NodeEntry> CopyNode(string relativePath, string nodeId, string nodeType, bool pidToPidRef = false, List<string> removePropertiesKey = null);
        Task<NodeEntry> CopyNode(string relativePath, NodeEntry nodeToCopy, string nodeType, bool pidToPidRef = false, List<string> removePropertiesKey = null);

        #endregion
    }
}