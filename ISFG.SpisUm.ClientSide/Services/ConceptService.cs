using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Common.Utils;
using ISFG.Data.Models;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Enums;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class ConceptService : IConceptService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAuditLogService _auditLogService;
        private readonly IComponentService _componentService;
        private readonly IDocumentService _documentService;
        private readonly IIdentityUser _identityUser;
        private readonly INodesService _nodesService;
        private readonly ITransactionHistoryService _transactionHistoryService;

        #endregion

        #region Constructors

        public ConceptService(IAlfrescoHttpClient alfrescoHttpClient,
                              INodesService nodesService,
                              ITransactionHistoryService transactionHistoryService,
                              IIdentityUser identityUser,
                              IComponentService componentService,
                              IDocumentService documentService,
                              IAuditLogService auditLogService
            )
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _nodesService = nodesService;
            _transactionHistoryService = transactionHistoryService;
            _identityUser = identityUser;
            _componentService = componentService;
            _documentService = documentService;
            _auditLogService = auditLogService;
        }

        #endregion

        #region Implementation of IConceptService

        public async Task<NodeEntry> ToDocument(string conceptId, string authorId, string subject, int attachmentCount, GenerateSsid ssidConfiguration, DateTime? settleTo = null)
        {
            var authorInfo = await _alfrescoHttpClient.GetPerson(authorId);

            if (authorInfo == null)
                throw new BadRequestException("", "Provided author does not exists");

            var myNode = await _alfrescoHttpClient.GetNodeInfo(AlfrescoNames.Aliases.My);

            var conceptInfo = await _alfrescoHttpClient.GetNodeInfo(conceptId);
            var conceptProperties = conceptInfo.Entry.Properties.As<JObject>().ToDictionary();

            var properties = conceptInfo?.Entry?.Properties?.As<JObject>().ToDictionary();
            var authorProperties = authorInfo?.Entry?.Properties?.As<JObject>().ToDictionary();


            // Get all components and update history properties
            List<VersionRecord> componentsVersionList = new List<VersionRecord>();

            var components = await _nodesService.GetSecondaryChildren(conceptId, SpisumNames.Associations.Components, false, true);
            components.ForEach(x =>
            {
                var properties = x?.Entry?.Properties.As<JObject>().ToDictionary();
                var versionLabel = properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.VersionLabel)?.ToString();

                componentsVersionList.Add(new VersionRecord { Id = x?.Entry?.Id, Version = "1.0" }); // Verison will be reset and this is neccesary so that revert will work fine.
            });


            // Create a document
            var parameters = ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.NodeType, SpisumNames.NodeTypes.Document, ParameterType.GetOrPost))
                .Add(new Parameter(AlfrescoNames.Headers.RelativePath, SpisumNames.Paths.EvidenceDocumentsForProcessing(_identityUser.RequestGroup), ParameterType.GetOrPost))
                ;

            try
            {
                conceptProperties.Remove(SpisumNames.Properties.Subject);
                conceptProperties.Remove(SpisumNames.Properties.Version);
                conceptProperties.Remove(SpisumNames.Properties.KeepForm);
                conceptProperties.Remove(SpisumNames.Properties.ComponentCounter);
                conceptProperties.Remove(SpisumNames.Properties.State);
                conceptProperties.Remove(SpisumNames.Properties.ComponentVersion);
                conceptProperties.Remove(SpisumNames.Properties.ComponentVersionOperation);
                conceptProperties.Remove(SpisumNames.Properties.ComponentVersionJSON);
            }
            catch { }

            parameters = _nodesService.CloneProperties(conceptProperties, parameters);

            parameters = parameters
                .Add(new Parameter(SpisumNames.Properties.Author, authorId, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Subject, subject, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.SettleToDate, settleTo?.ToAlfrescoDateTimeString(), ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.AttachmentsCount, attachmentCount, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.AuthorId, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserId)?.ToString(), ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.AuthorOrgId, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgId)?.ToString(), ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.AuthorOrgName, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgName)?.ToString(), ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.AuthorOrgUnit, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgUnit)?.ToString(), ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.AuthorJob, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserJob)?.ToString(), ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.AuthorOrgAddress, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgAddress)?.ToString(), ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.KeepForm, SpisumNames.StoreForm.Original, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.SenderType, SpisumNames.SenderType.Own, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Sender, SpisumNames.Other.Own, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Sender_Name, SpisumNames.Other.Own, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Sender_Address, SpisumNames.Other.Own, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Sender_Contact, SpisumNames.Other.Own, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.State, SpisumNames.State.Unprocessed, ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.ComponentVersionJSON, JsonConvert.SerializeObject(componentsVersionList), ParameterType.GetOrPost))
                .Add(new Parameter(AlfrescoNames.ContentModel.Owner, properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString(), ParameterType.GetOrPost))
                .Add(new Parameter(SpisumNames.Properties.Version, 1, ParameterType.GetOrPost))
                ;

            var documentEntry = await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new FormDataParam(new byte[] { 01 }), parameters);

            // Copy association of components
            var conceptChildrens = await _nodesService.GetSecondaryChildren(conceptId, SpisumNames.Associations.Components);
            await conceptChildrens.ForEachAsync(async x =>
            {
                // Remove old versions old file and associate with new node
                await _nodesService.RemoveAllVersions(x?.Entry?.Id);

                // Generate new PID
                var componentPID = await _componentService.GenerateComponentPID(documentEntry?.Entry?.Id, "/", GeneratePIDComponentType.Component);
                await _alfrescoHttpClient.UpdateNode(x?.Entry?.Id, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.Pid, componentPID)
                  );

                await _alfrescoHttpClient.CreateNodeSecondaryChildren(documentEntry?.Entry?.Id, new ChildAssociationBody()
                {
                    AssocType = SpisumNames.Associations.Components,
                    ChildId = x?.Entry?.Id
                });
            });

            // Create permissions for the node
            await _nodesService.CreatePermissions(documentEntry?.Entry?.Id, _identityUser.RequestGroup, _identityUser.Id);

            // Generate Ssid
            var conceptEntry = await _nodesService.GenerateSsid(documentEntry?.Entry?.Id, ssidConfiguration);

            // Delete deleted components
            var deletedComponents = await _nodesService.GetSecondaryChildren(conceptId, SpisumNames.Associations.DeletedComponents);
            await deletedComponents.ForEachAsync(async x =>
            {
                await _nodesService.DeleteNodePermanent(x?.Entry?.Id);
            });

            // Delete concept
            await _nodesService.DeleteNodeAsAdmin(conceptId);

            try
            {
                documentEntry = await _alfrescoHttpClient.GetNodeInfo(documentEntry?.Entry?.Id);

                var pidDocument = documentEntry?.GetPid();
                var pidConcept = conceptInfo?.GetPid();

                // Audit log for a document
                await _auditLogService.Record(documentEntry?.Entry.Id, SpisumNames.NodeTypes.Document, pidDocument, NodeTypeCodes.Dokument, EventCodes.Zalozeni,
                    string.Format(TransactinoHistoryMessages.ConceptToDocument, pidConcept));

                var fileId = await _documentService.GetDocumentFileId(documentEntry?.Entry.Id);
                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Document, pidDocument, NodeTypeCodes.Dokument, EventCodes.Zalozeni,
                        string.Format(TransactinoHistoryMessages.ConceptToDocument, pidConcept));
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return conceptEntry;
        }

        #endregion
    }
}
