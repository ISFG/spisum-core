using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CodeList;
using ISFG.Alfresco.Api.Models.CoreApi.AuthApi;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApi.SearchApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Alfresco.Api.Models.GsApi.GsApi;
using ISFG.Alfresco.Api.Models.Rules;
using ISFG.Alfresco.Api.Models.WebScripts;
using ISFG.Common.Exceptions;
using ISFG.Common.Extensions;
using ISFG.Common.HttpClient;
using ISFG.Exceptions.Exceptions;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using RestSharp;
using Serilog;
using Serilog.Events;

namespace ISFG.Alfresco.Api.Services
{
    public class AlfrescoHttpClient : HttpClient, IAlfrescoHttpClient
    {
        #region Fields

        private readonly IAuthenticationHandler _authenticationHandler;

        #endregion

        #region Constructors

        public AlfrescoHttpClient(IAlfrescoConfiguration alfrescoConfig, IAuthenticationHandler authenticationHandler) :
            base(alfrescoConfig.Url) =>
            _authenticationHandler = authenticationHandler;

        #endregion

        #region Implementation of IAlfrescoHttpClient

        public async Task<CodeListCreateARM> CodeListCreate(CodeListCreateAM body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<CodeListCreateARM>(Method.POST,
                "alfresco/service/api/rma/admin/rmconstraints",
                body, parameters);

        public async Task<CodeListDummyModel> CodeListDelete(string listName, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<CodeListDummyModel>(Method.DELETE,
                $"alfresco/service/api/rma/admin/rmconstraints/{listName}",
                null, parameters);

        public async Task<CodeListAllARM> CodeListGetAll(IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<CodeListAllARM>(Method.GET, "alfresco/service/api/rma/admin/rmconstraints",
                null, parameters);

        public async Task<CodeListValuesARM> CodeListGetWithValues(string listName, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<CodeListValuesARM>(Method.GET,
                $"alfresco/service/api/rma/admin/rmconstraints/{listName}/values",
                null, parameters);

        public async Task<CodeListUpdateARM> CodeListUpdate(string listName, CodeListUpdateAM body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<CodeListUpdateARM>(Method.PUT,
                $"alfresco/service/api/rma/admin/rmconstraints/{listName}",
                body, parameters);

        public async Task<CodeListUpdateValuesARM> CodeListUpdateValues(string listName, CodeListUpdateValuesAM body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<CodeListUpdateValuesARM>(Method.PUT,
                $"alfresco/service/api/rma/admin/rmconstraints/{listName}",
                body, parameters);

        public async Task<CodeListUpdateValuesAthoritiesARM> CodeListUpdateValuesWithAuthority(string listName, CodeListUpdateValuesAuthorityAM body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<CodeListUpdateValuesAthoritiesARM>(Method.POST,
                $"alfresco/service/api/rma/admin/rmconstraints/{listName}/values",
                body, parameters);

        public async Task<RecordEntry> CompleteRecord(string recordId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<RecordEntry>(Method.POST,
                $"alfresco/api/-default-/public/gs/versions/1/records/{recordId}/complete", null, parameters);

        public async Task<CommentEntryFixed> CreateComment(string nodeId, CommentBody body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<CommentEntryFixed>(Method.POST,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/comments", body, parameters);

        public async Task<GroupEntry> CreateGroup(GroupBodyCreate body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<GroupEntry>(Method.POST,
                 "alfresco/api/-default-/public/alfresco/versions/1/groups", body, parameters);

        public async Task<GroupMemberEntry> CreateGroupMember(string groupId, GroupMembershipBodyCreate body, IImmutableList<Parameter> parameters = null)
           => await ExecuteRequest<GroupMemberEntry>(Method.POST,
                $"alfresco/api/-default-/public/alfresco/versions/1/groups/{groupId}/members", body, parameters);

        public async Task<NodeEntry> CreateNode(string parentNodeId, object body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<NodeEntry>(Method.POST,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{parentNodeId}/children", body, parameters);

        public async Task<AssociationEntry> CreateNodeAssociation(string parentNodeId, object body, IImmutableList<Parameter> parameters = null)
           => await ExecuteRequest<AssociationEntry>(Method.POST,
               $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{parentNodeId}/targets", body, parameters);

        public async Task<ChildAssociationEntry> CreateNodeSecondaryChildren(string parentNodeId, ChildAssociationBody body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<ChildAssociationEntry>(Method.POST,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{parentNodeId}/secondary-children", body, parameters);

        public async Task<PersonEntryFixed> CreatePerson(PersonBodyCreate body, IImmutableList<Parameter> parameters = null)
           => await ExecuteRequest<PersonEntryFixed>(Method.POST,
               "alfresco/api/-default-/public/alfresco/versions/1/people", body, parameters);

        public async Task<RecordEntry> CreateRecord(string recordFolderId, RMNodeBodyCreate body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<RecordEntry>(Method.POST,
                $"alfresco/api/-default-/public/gs/versions/1/record-folders/{recordFolderId}/records", body, parameters);

        public async Task<RecordCategoryEntry> CreateRecordCategory(string filePlanId, object body,
            IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<RecordCategoryEntry>(Method.POST,
                $"alfresco/api/-default-/public/gs/versions/1/file-plans/{filePlanId}/categories", body, parameters);

        public async Task<RecordCategoryChildEntry> CreateRecordCategoryChild(string nodeId, object body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<RecordCategoryChildEntry>(Method.POST,
                $"alfresco/api/-default-/public/gs/versions/1/record-categories/{nodeId}/children", body, parameters);

        public async Task<SiteEntry>
            CreateSite(object body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<SiteEntry>(Method.POST,
                "alfresco/api/-default-/public/alfresco/versions/1/sites", body, parameters);

        public async Task<RMSiteEntry>
            CreateSiteRM(object body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<RMSiteEntry>(Method.POST,
                "alfresco/api/-default-/public/gs/versions/1/gs-sites", body, parameters);

        public async Task DeleteGroup(string groupId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<object>(Method.DELETE,
                $"alfresco/api/-default-/public/alfresco/versions/1/groups/{groupId}", null, parameters);

        public async Task DeleteGroupMember(string groupId, string groupMemberId)
            => await ExecuteRequest<object>(Method.DELETE,
                $"alfresco/api/-default-/public/alfresco/versions/1/groups/{groupId}/members/{groupMemberId}");

        public async Task DeleteNode(string nodeId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<object>(Method.DELETE,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}", null, parameters);

        public async Task DeletePerson(string personId, IImmutableList<Parameter> parameters = null)
          => await ExecuteRequest<object>(Method.DELETE,
               $"alfresco/s/api/people/{personId}", null, parameters);

        public async Task DeleteSecondaryChildren(string nodeId, string childId, IImmutableList<Parameter> parameters = null)
          => await ExecuteRequest<object>(Method.DELETE,
              $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/secondary-children/{childId}", null, parameters);

        public async Task DispositionActionDefinitions(string nodeId, DispositionActionDefinitions body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<object>(Method.POST,
                $"alfresco/s/api/node/workspace/SpacesStore/{nodeId}/dispositionschedule/dispositionactiondefinitions", body, parameters);

        public async Task<DownloadEntry> Download(DownloadBodyCreate body)
            => await ExecuteRequest<DownloadEntry>(Method.POST,
                "alfresco/api/-default-/public/alfresco/versions/1/downloads", body);

        public async Task DownloadDelete(string downloadId)
            => await ExecuteRequest<object>(Method.DELETE,
                $"alfresco/api/-default-/public/alfresco/versions/1/downloads/{downloadId}");

        public async Task<DownloadEntry> DownloadInfo(string downloadId)
            => await ExecuteRequest<DownloadEntry>(Method.GET,
                $"alfresco/api/-default-/public/alfresco/versions/1/downloads/{downloadId}");

        public async Task ExecutionQueue(ExecutionQueue body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<object>(Method.POST,
                "alfresco/s/api/rma/actions/ExecutionQueue", body, parameters);

        public async Task<FavoriteEntry> FavoriteAdd(string personId, FavoriteBodyCreate body, IImmutableList<Parameter> parameters = null)
          => await ExecuteRequest<FavoriteEntry>(Method.POST,
               $"alfresco/api/-default-/public/alfresco/versions/1/people/{personId}/favorites", body, parameters);

        public async Task FavoriteRemove(string personId, string favouriteId)
          => await ExecuteRequest<object>(Method.DELETE,
               $"alfresco/api/-default-/public/alfresco/versions/1/people/{personId}/favorites/{favouriteId}");

        public async Task FormProcessor(string nodeId, FormProcessor body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<object>(Method.POST,
                $"alfresco/s/api/node/workspace/SpacesStore/{nodeId}/formprocessor", body, parameters);

        public async Task<CommentPagingFixed> GetComments(string nodeId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<CommentPagingFixed>(Method.GET,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/comments", null, parameters);

        public async Task<FilePlanEntry> GetFilePlan(string filePlanId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<FilePlanEntry>(Method.GET,
                $"alfresco/api/-default-/public/gs/versions/1/file-plans/{filePlanId}", null, parameters);

        public async Task<GroupEntry> GetGroup(string groupId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<GroupEntry>(Method.GET,
                $"alfresco/api/-default-/public/alfresco/versions/1/groups/{groupId}", null, parameters);

        public async Task<GroupMemberPaging> GetGroupMembers(string groupId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<GroupMemberPaging>(Method.GET,
                $"alfresco/api/-default-/public/alfresco/versions/1/groups/{groupId}/members", null, parameters);

        public async Task<NodeChildAssociationPaging> GetNodeChildren(string nodeId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<NodeChildAssociationPaging>(Method.GET,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/children", null, parameters);

        public async Task<NodeEntry> GetNodeInfo(string nodeId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<NodeEntry>(Method.GET,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}",
                null, parameters);

        public async Task<NodeAssociationPaging> GetNodeParents(string parentNodeId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<NodeAssociationPaging>(Method.GET,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{parentNodeId}/parents", null, parameters);

        public async Task<NodeChildAssociationPagingFixed> GetNodeSecondaryChildren(string nodeId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<NodeChildAssociationPagingFixed>(Method.GET,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/secondary-children", null, parameters);

        public async Task<PersonPagingFixed> GetPeople(IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<PersonPagingFixed>(Method.GET,
                "alfresco/api/-default-/public/alfresco/versions/1/people", null, parameters);

        public async Task<PersonEntryFixed> GetPerson(string personId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<PersonEntryFixed>(Method.GET,
                $"alfresco/api/-default-/public/alfresco/versions/1/people/{personId}", null, parameters);

        public async Task<GroupPagingFixed> GetPersonGroups(string personId, IImmutableList<Parameter> parameters = null) => 
            await ExecuteRequest<GroupPagingFixed>(Method.GET, 
                $"alfresco/api/-default-/public/alfresco/versions/1/people/{personId}/groups", null, parameters);

        public async Task<PersonPaging> GetQueriesPeople(IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<PersonPaging>(Method.GET,
                "alfresco/api/-default-/public/alfresco/versions/1/queries/people", null, parameters);

        public async Task<RecordCategoryEntry> GetRecordCategory(string nodeId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<RecordCategoryEntry>(Method.GET,
                $"alfresco/api/-default-/public/gs/versions/1/record-categories/{nodeId}", null, parameters);

        public async Task<RecordCategoryChildPaging> GetRecordCategoryChildren(string nodeId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<RecordCategoryChildPaging>(Method.GET,
                $"alfresco/api/-default-/public/gs/versions/1/record-categories/{nodeId}/children", null, parameters);

        public async Task<RMSiteEntry> GetRMSite(IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<RMSiteEntry>(Method.GET, "alfresco/api/-default-/public/gs/versions/1/gs-sites/rm",
                null, parameters);

        public async Task<SitePaging> GetSites(IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<SitePaging>(Method.GET, "alfresco/api/-default-/public/alfresco/versions/1/sites",
                null, parameters);

        public async Task<FormDataParam> GetThumbnailPdf(string nodeId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<FormDataParam>(Method.GET, $"alfresco/s/api/node/workspace/SpacesStore/{nodeId}/content/thumbnails/pdf",
                null, parameters);

        public async Task<VersionPaging> GetVersions(string nodeId, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<VersionPaging>(Method.GET, $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/versions",
                null, parameters);

        public async Task<TicketEntry> Login(TicketBody authorization)
            => await ExecuteRequest<TicketEntry>(Method.POST,
                "alfresco/api/-default-/public/authentication/versions/1/tickets", authorization);

        public async Task Logout()
            => await ExecuteRequest<object>(Method.DELETE,
                "alfresco/api/-default-/public/authentication/versions/1/tickets/-me-");

        public async Task<FormDataParam> NodeContent(string nodeId, IImmutableList<Parameter> parameters = null)
             => await ExecuteRequest<FormDataParam>(Method.GET,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/content",
                null, parameters);

        public async Task<NodeEntry> NodeCopy(string nodeId, NodeBodyCopy body, IImmutableList<Parameter> parameters = null)
         => await ExecuteRequest<NodeEntry>(Method.POST,
             $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/copy", body, parameters);

        public async Task NodeDeleteVersion(string nodeId, string versionId, IImmutableList<Parameter> parameters = null)
          => await ExecuteRequest<object>(Method.DELETE,
               $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/versions/{versionId}", null, parameters);

        public async Task<NodeEntry> NodeLock(string nodeId, NodeBodyLock body)
            => await ExecuteRequest<NodeEntry>(Method.POST, 
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/lock", body);

        public async Task<NodeEntry> NodeMove(string nodeId, NodeBodyMove body, IImmutableList<Parameter> parameters = null)
          => await ExecuteRequest<NodeEntry>(Method.POST,
              $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/move", body, parameters);

        public async Task<NodeEntry> NodeUnlock(string nodeId)
            => await ExecuteRequest<NodeEntry>(Method.POST, 
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/unlock");

        public async Task<VersionEntry> NodeVersion(string nodeId, string versionId, IImmutableList<Parameter> parameters = null)
          => await ExecuteRequest<VersionEntry>(Method.GET,
               $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/versions/{versionId}", null, parameters);

        public async Task<VersionPaging> NodeVersions(string nodeId, IImmutableList<Parameter> parameters = null)
          => await ExecuteRequest<VersionPaging>(Method.GET,
               $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/versions", null, parameters);

        public async Task<VersionEntry> RevertVersion(string nodeId, string versionId, RevertBody body, IImmutableList<Parameter> parameters = null)
          => await ExecuteRequest<VersionEntry>(Method.POST,
               $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/versions/{versionId}/revert", body, parameters);

        public async Task<ResultSetPaging> Search(SearchRequest body)
           => await ExecuteRequest<ResultSetPaging>(Method.POST,
              "alfresco/api/-default-/public/search/versions/1/search", body);

        public async Task<NodeEntry> UpdateContent(string nodeId, byte[] content, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<NodeEntry>(Method.PUT,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}/content", content, parameters);

        public async Task<GroupEntry> UpdateGroup(string groupId, GroupBodyUpdate body, IImmutableList<Parameter> parameters = null) => await ExecuteRequest<GroupEntry>(Method.PUT,
                $"alfresco/api/-default-/public/alfresco/versions/1/groups/{groupId}", body, parameters);

        public async Task<NodeEntry> UpdateNode(string nodeId, NodeBodyUpdate body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<NodeEntry>(Method.PUT,
                $"alfresco/api/-default-/public/alfresco/versions/1/nodes/{nodeId}", body, parameters);

        public async Task<PersonEntryFixed> UpdatePerson(string personId, PersonBodyUpdate body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<PersonEntryFixed>(Method.PUT,
                $"alfresco/api/-default-/public/alfresco/versions/1/people/{personId}", body, parameters);

        public async Task<object> UploadContent(FormDataParam body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<object>(Method.POST,
                "alfresco/service/api/upload", body, parameters);

        public async Task<ValidTicketEntry> ValidateTicket()
            => await ExecuteRequest<ValidTicketEntry>(Method.GET,
                "alfresco/api/-default-/public/authentication/versions/1/tickets/-me-");

        public async Task<SuccessARM> WebScriptsNodeRuleDelete(string storeType, string storeId, string Id, string ruleId, IImmutableList<Parameter> parameters = null)
           => await ExecuteRequest<SuccessARM>(Method.DELETE,
                $"/alfresco/service/api/node/{storeType}/{storeId}/{Id}/ruleset/rules/{ruleId}", null, parameters);

        public async Task<GetNodeRulesARM> WebScriptsNodeRules(string storeType, string storeId, string Id, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<GetNodeRulesARM>(Method.GET,
                $"/alfresco/service/api/node/{storeType}/{storeId}/{Id}/ruleset/rules", null, parameters);

        public async Task<CreateRuleARM> WebScriptsRuleCreate(string storeType, string storeId, string Id, CreateRuleAM body, IImmutableList<Parameter> parameters = null)
            => await ExecuteRequest<CreateRuleARM>(Method.POST,
                $"/alfresco/service/api/node/{storeType}/{storeId}/{Id}/ruleset/rules", body, parameters);
        public async Task<object> WebScriptsRuleInheritance(string storeType, string storeId, string Id)
            => await ExecuteRequest<object>(Method.POST,
                $"/alfresco/service/api/node/{storeType}/{storeId}/{Id}/ruleset/inheritrules/toggle");

        #endregion

        #region Override of HttpClient

        protected override void BuildContent(RestRequest request, object body)
        {
            if (body is FormDataParam content)
            {
                request.AddFileBytes(content.Name, content.File, content.FileName, content.ContentType);
                return;
            }
            if (body is List<FormDataParam> attachments)
            {
                attachments.ForEach(x => request.AddFileBytes("attachments", x.File, x.FileName, x.ContentType));
                return;
            }
            if (body is byte[])
            {
                request.AddParameter("file", body, ParameterType.RequestBody);
                return;
            }

            base.BuildContent(request, body);
        }

        protected override object BuildResponse<T>(IRestResponse response)
        {
            if (typeof(T) != typeof(FormDataParam)) 
                return base.BuildResponse<T>(response);
            
            var contentDisposition = response.Headers.FirstOrDefault(x => x.Name == HeaderNames.ContentDisposition)?.Value?.ToString();
            if (!string.IsNullOrEmpty(contentDisposition))
            {
                var parseContentDisposition = ContentDispositionHeaderValue.Parse(contentDisposition);
                
                if (parseContentDisposition.FileNameStar != null || parseContentDisposition.FileName != null)
                    return new FormDataParam(response.RawBytes, parseContentDisposition.FileNameStar != null ? parseContentDisposition.FileNameStar.ToString() : parseContentDisposition.FileName.ToString(), null, response.ContentType);
            }
            
            return new FormDataParam(response.RawBytes, null, null, response.ContentType);
        }

        protected override async Task<T> ExecuteRequest<T>(Method httpMethod, string url, object body = null, IImmutableList<Parameter> parameters = null)
        {
            try
            {
                return await base.ExecuteRequest<T>(httpMethod, url, body, parameters);
            }
            catch (NotAuthenticatedException)
            {
                if (await _authenticationHandler.HandleNotAuthenticated())
                    return await base.ExecuteRequest<T>(httpMethod, url, body, parameters);

                throw;
            }
        }

        protected override void HandleException(Exception ex)
        {
            if (!(ex is HttpClientException httpException))
                return;
            
            var errorBody = JsonConvert.DeserializeObject<Models.CoreApi.AuthApi.Error>(httpException.Content);
            if (errorBody == null)
                return;
            
            if (httpException.HttpStatusCode == HttpStatusCode.Unauthorized)
                throw new NotAuthenticatedException(errorBody.Error1.StatusCode.ToString(), errorBody.Error1.BriefSummary);

            if (httpException.HttpStatusCode == HttpStatusCode.Forbidden)
                throw new ForbiddenException(errorBody.Error1.StatusCode.ToString(), errorBody.Error1.BriefSummary);
            
            if (httpException.HttpStatusCode == HttpStatusCode.NotFound)
                throw new BadRequestException(errorBody.Error1.StatusCode.ToString(), errorBody.Error1.BriefSummary);
            
            if (httpException.HttpStatusCode == HttpStatusCode.BadRequest)
                throw new BadRequestException(errorBody.Error1.StatusCode.ToString(), errorBody.Error1.BriefSummary);

            if (httpException.HttpStatusCode == HttpStatusCode.UnprocessableEntity)
                throw new BadRequestException(errorBody.Error1.StatusCode.ToString(), errorBody.Error1.BriefSummary);
        }

        protected override void LogHttpRequest(IRestResponse response)
        {
            if (!Log.IsEnabled(LogEventLevel.Debug))
                return;
            
            Log.Debug(response.ToMessage());
        }

        protected override void PrepareRequest(IRestRequest request) =>
            _authenticationHandler?.AuthenticateRequest(request);

        #endregion
    }
}