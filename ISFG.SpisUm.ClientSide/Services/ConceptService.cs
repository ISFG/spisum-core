using System;
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
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using Newtonsoft.Json.Linq;
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

            // Create a copy of concept
            var conceptCopyInfo = await _alfrescoHttpClient.NodeCopy(conceptId, new NodeBodyCopy
            {
                TargetParentId = myNode?.Entry?.Id,
                Name = IdGenerator.GenerateId()
            });

            // Copy association of components
            var conceptChildrens = await _nodesService.GetSecondaryChildren(conceptId, SpisumNames.Associations.Components);
            await conceptChildrens.ForEachAsync(async x =>
            {
                // Remove old versions old file and associate with new node
                await _nodesService.RemoveAllVersions(x?.Entry?.Id);
            });

            var properties = conceptInfo?.Entry?.Properties?.As<JObject>().ToDictionary();
            var authorProperties = authorInfo?.Entry?.Properties?.As<JObject>().ToDictionary();

            // Save properties
            var documentInfo = await _alfrescoHttpClient.UpdateNode(conceptCopyInfo?.Entry?.Id, new NodeBodyUpdate
                {
                NodeType = SpisumNames.NodeTypes.Document
            }
                .AddProperty(SpisumNames.Properties.Pid, null)
                .AddProperty(SpisumNames.Properties.Author, authorId)
                .AddProperty(SpisumNames.Properties.Subject, subject)
                .AddProperty(SpisumNames.Properties.SettleToDate, settleTo)
                .AddProperty(SpisumNames.Properties.AttachmentsCount, attachmentCount)

                .AddProperty(SpisumNames.Properties.AuthorId, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserId)?.ToString())
                .AddProperty(SpisumNames.Properties.AuthorOrgId, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgId)?.ToString())
                .AddProperty(SpisumNames.Properties.AuthorOrgName, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgName)?.ToString())
                .AddProperty(SpisumNames.Properties.AuthorOrgUnit, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgUnit)?.ToString())
                .AddProperty(SpisumNames.Properties.AuthorJob, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserJob)?.ToString())
                .AddProperty(SpisumNames.Properties.AuthorOrgAddress, authorProperties.GetNestedValueOrDefault(SpisumNames.Properties.UserOrgAddress)?.ToString())

                .AddProperty(SpisumNames.Properties.KeepForm, SpisumNames.StoreForm.Original)
                .AddProperty(SpisumNames.Properties.SenderType, SpisumNames.SenderType.Own)
                .AddProperty(SpisumNames.Properties.Sender, SpisumNames.Other.Own)
                .AddProperty(SpisumNames.Properties.Sender_Name, SpisumNames.Other.Own)
                .AddProperty(SpisumNames.Properties.Sender_Address, SpisumNames.Other.Own)
                .AddProperty(SpisumNames.Properties.Sender_Contact, SpisumNames.Other.Own)

                .AddProperty(SpisumNames.Properties.State, SpisumNames.State.Unprocessed)

                .AddProperty(AlfrescoNames.ContentModel.Owner, properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString())
                .AddProperty(SpisumNames.Properties.Version, 1)
            );

            // Create permissions for the node
            await _nodesService.CreatePermissions(conceptCopyInfo?.Entry?.Id, _identityUser.RequestGroup, _identityUser.Id);

            await _nodesService.MoveByPath(conceptCopyInfo?.Entry?.Id, SpisumNames.Paths.EvidenceDocumentsForProcessing(_identityUser.RequestGroup));

            // Generate Ssid
            var conceptEntry = await _nodesService.GenerateSsid(conceptCopyInfo?.Entry?.Id, ssidConfiguration);

            // Delete deleted components
            var deletedComponents = await _nodesService.GetSecondaryChildren(conceptId, SpisumNames.Associations.DeletedComponents);
            await deletedComponents.ForEachAsync(async x =>
             {
                 await _nodesService.DeleteNodePermanent(x?.Entry?.Id);
             });

            // Delete concept
            await _nodesService.DeleteNodePermanent(conceptId);

            try
            {
                var conceptPid = conceptInfo?.GetPid();

                // Audit log for a document
                await _auditLogService.Record(documentInfo?.Entry.Id, SpisumNames.NodeTypes.Document, conceptPid, NodeTypeCodes.Dokument, EventCodes.Zalozeni,
                    TransactinoHistoryMessages.ConceptToDocument);

                var fileId = await _documentService.GetDocumentFileId(documentInfo?.Entry.Id);
                if (fileId != null)
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.Document, conceptPid, NodeTypeCodes.Dokument, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.ConceptToDocument);
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
