using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.AuthApi;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApi.SearchApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Alfresco.Api.Models.Rules;
using ISFG.Alfresco.Api.Models.WebScripts;
using RestSharp;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace ISFG.Alfresco.Api.Interfaces
{
    public interface IAlfrescoHttpClient
    {
        #region Public Methods

        Task<CommentEntryFixed> CreateComment(string nodeId, CommentBody body, IImmutableList<Parameter> parameters = null);
        Task<GroupEntry> CreateGroup(GroupBodyCreate body, IImmutableList<Parameter> parameters = null);
        Task<GroupMemberEntry> CreateGroupMember(string groupId, GroupMembershipBodyCreate body, IImmutableList<Parameter> parameters = null);
        Task<NodeEntry> CreateNode(string parentNodeId, object body, IImmutableList<Parameter> parameters = null);
        Task<AssociationEntry> CreateNodeAssociation(string parentNodeId, object body, IImmutableList<Parameter> parameters = null);
        Task<ChildAssociationEntry> CreateNodeSecondaryChildren(string parentNodeId, ChildAssociationBody body, IImmutableList<Parameter> parameters = null);
        Task<PersonEntryFixed> CreatePerson(PersonBodyCreate body, IImmutableList<Parameter> parameters = null);
        Task<SiteEntry> CreateSite(object body, IImmutableList<Parameter> parameters = null);
        Task DeleteGroup(string groupId, IImmutableList<Parameter> parameters = null);
        Task DeleteGroupMember(string groupId, string groupMemberId);
        Task DeleteNode(string nodeId, IImmutableList<Parameter> parameters = null);
        Task DeletePerson(string personId, IImmutableList<Parameter> parameters = null);
        Task DeleteSecondaryChildren(string nodeId, string childId, IImmutableList<Parameter> parameters = null);
        Task<DownloadEntry> Download(DownloadBodyCreate body);
        Task DownloadDelete(string downloadId);
        Task<DownloadEntry> DownloadInfo(string downloadId);
        Task<FavoriteEntry> FavoriteAdd(string personId, FavoriteBodyCreate body, IImmutableList<Parameter> parameters = null);
        Task FavoriteRemove(string personId, string favouriteId);
        Task FormProcessor(string nodeId, FormProcessor body, IImmutableList<Parameter> parameters = null);
        Task<CommentPagingFixed> GetComments(string nodeId, IImmutableList<Parameter> parameters = null);

        Task<GroupEntry> GetGroup(string groupId, IImmutableList<Parameter> parameters = null);
        Task<GroupMemberPaging> GetGroupMembers(string groupId, IImmutableList<Parameter> parameters = null);
        Task<NodeChildAssociationPaging> GetNodeChildren(string nodeId, IImmutableList<Parameter> parameters = null);
        Task<NodeEntry> GetNodeInfo(string nodeId, IImmutableList<Parameter> parameters = null);
        Task<NodeAssociationPaging> GetNodeParents(string parentNodeId, IImmutableList<Parameter> parameters = null);
        Task<NodeChildAssociationPagingFixed> GetNodeSecondaryChildren(string nodeId, IImmutableList<Parameter> parameters = null);
        Task<PersonPagingFixed> GetPeople(IImmutableList<Parameter> parameters = null);
        Task<PersonEntryFixed> GetPerson(string personId, IImmutableList<Parameter> parameters = null);
        Task<GroupPagingFixed> GetPersonGroups(string personId, IImmutableList<Parameter> parameters = null);
        Task<PersonPaging> GetQueriesPeople(IImmutableList<Parameter> parameters = null);
        Task<SitePaging> GetSites(IImmutableList<Parameter> parameters = null);
        Task<FormDataParam> GetThumbnailPdf(string nodeId, IImmutableList<Parameter> parameters = null);
        Task<VersionPaging> GetVersions(string nodeId, IImmutableList<Parameter> parameters = null);
        Task<TicketEntry> Login(TicketBody authorization);
        Task Logout();
        Task<FormDataParam> NodeContent(string nodeId, IImmutableList<Parameter> parameters = null);
        Task<NodeEntry> NodeCopy(string nodeId, NodeBodyCopy body, IImmutableList<Parameter> parameters = null);
        Task NodeDeleteVersion(string nodeId, string versionId, IImmutableList<Parameter> parameters = null);
        Task<NodeEntry> NodeLock(string nodeId, NodeBodyLock body);
        Task<NodeEntry> NodeMove(string nodeId, NodeBodyMove body, IImmutableList<Parameter> parameters = null);
        Task<NodeEntry> NodeUnlock(string nodeId);
        Task<VersionEntry> NodeVersion(string nodeId, string versionId, IImmutableList<Parameter> parameters = null);
        Task<VersionPaging> NodeVersions(string nodeId, IImmutableList<Parameter> parameters = null);
        Task<VersionEntry> RevertVersion(string nodeId, string versionId, RevertBody body, IImmutableList<Parameter> parameters = null);
        Task<ResultSetPaging> Search(SearchRequest body);
        Task<NodeEntry> UpdateContent(string nodeId, byte[] content, IImmutableList<Parameter> parameters = null);
        Task<GroupEntry> UpdateGroup(string groupId, GroupBodyUpdate body, IImmutableList<Parameter> parameters = null);
        Task<NodeEntry> UpdateNode(string nodeId, NodeBodyUpdate body, IImmutableList<Parameter> parameters = null);
        Task<PersonEntryFixed> UpdatePerson(string personId, PersonBodyUpdate body, IImmutableList<Parameter> parameters = null);
        Task<object> UploadContent(FormDataParam body, IImmutableList<Parameter> parameters = null);
        Task<ValidTicketEntry> ValidateTicket();
        Task<SuccessARM> WebScriptsNodeRuleDelete(string storeType, string storeId, string Id, string ruleId, IImmutableList<Parameter> parameters = null);
        Task<GetNodeRulesARM> WebScriptsNodeRules (string storeType, string storeId, string Id, IImmutableList<Parameter> parameters = null);
        Task<CreateRuleARM> WebScriptsRuleCreate(string storeType, string storeId, string Id, CreateRuleAM body, IImmutableList<Parameter> parameters = null);
        Task<object> WebScriptsRuleInheritance(string storeType, string storeId, string Id);

        #endregion
    }
}