using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.Data.Models;
using ISFG.DataBox.Api.Interfaces;
using ISFG.Email.Api.Interfaces;
using ISFG.Exceptions.Exceptions;
using ISFG.SpisUm.ClientSide.Enums;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using Newtonsoft.Json.Linq;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.ClientSide.Services
{
    public class ShipmentsService : IShipmentsService
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAlfrescoModelComparer _alfrescoModelComparer;
        private readonly IAuditLogService _auditLogService;
        private readonly IComponentService _componentService;
        private readonly IDataBoxHttpClient _dataBoxHttpClient;
        private readonly IDocumentService _documentService;
        private readonly IEmailHttpClient _emailHttpClient;
        private readonly IIdentityUser _identityUser;
        private readonly INodesService _nodesService;
        private readonly ITransactionHistoryService _transactionHistoryService;

        #endregion

        #region Constructors

        public ShipmentsService(
            INodesService nodesService,
            IAlfrescoHttpClient alfrescoHttpClient,
            ITransactionHistoryService transactionHistoryService,
            IComponentService componentService,
            IEmailHttpClient emailHttpClient,
            IDataBoxHttpClient dataBoxHttpClient,
            IIdentityUser identityUser,
            IAuditLogService auditLogService,
            IDocumentService documentService,
            IAlfrescoModelComparer alfrescoModelComparer
        )
        {
            _nodesService = nodesService;
            _alfrescoHttpClient = alfrescoHttpClient;
            _transactionHistoryService = transactionHistoryService;
            _componentService = componentService;
            _emailHttpClient = emailHttpClient;
            _dataBoxHttpClient = dataBoxHttpClient;
            _identityUser = identityUser;
            _auditLogService = auditLogService;
            _documentService = documentService;
            _alfrescoModelComparer = alfrescoModelComparer;
        }

        #endregion

        #region Implementation of IShipmentsService

        public async Task<List<string>> CancelShipment(List<string> shipmentIds)
        {
            var unprocessedNodes = new List<string>();

            await shipmentIds.ForEachAsync(async x =>
            {
                var lockedComponents = new List<string>();

                try
                {
                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(x, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                    var shipmentInfo = await _alfrescoHttpClient.GetNodeInfo(x);
                    var parentDocument = await GetShipmentDocument(x);

                    var parents = await _nodesService.GetParentsByAssociation(x, new List<string>
                    {
                        SpisumNames.Associations.ShipmentsCreated,
                        SpisumNames.Associations.ShipmentsToReturn,
                        SpisumNames.Associations.ShipmentsToDispatch
                    });

                    lockedComponents = await UnlockAllAssociations(x);

                    await _nodesService.DeleteNodePermanent(x, false);

                    try
                    {
                        var shipmentPid = shipmentInfo?.GetPid();

                        // Log for shipment
                        await _auditLogService.Record(x, shipmentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zruseni, 
                            TransactinoHistoryMessages.ShipmentCancel);

                        // Log for document
                        await _auditLogService.Record(parentDocument?.Entry?.Id, shipmentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zruseni, 
                            TransactinoHistoryMessages.ShipmentCancel);

                        var fileId = await _documentService.GetDocumentFileId(parentDocument?.Entry?.Id);

                        if (fileId != null)
                            // Log for file
                            await _auditLogService.Record(fileId, shipmentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zruseni,
                                TransactinoHistoryMessages.ShipmentCancel);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    unprocessedNodes.Add(x);
                    if (lockedComponents.Count > 0)
                        await lockedComponents.ForEachAsync(async locNodeId => await _alfrescoHttpClient.NodeLock(locNodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL)));
                }
            });

            return unprocessedNodes;
        }

        public async Task<NodeEntry> CreateShipmentDataBox(string nodeId, bool allowSubstDelivery, string legalTitleLaw, string legalTitleYear, string legalTitleSect,
            string legalTitlePar, string legalTitlePoint, bool personalDelivery, string recipient, string sender, string subject, string toHands, List<string> components)
        {
            var finnalyLock = false;

            try
            {
                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (nodeInfo?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(nodeId);
                    finnalyLock = true;
                }

                var parameters = await GetShipmentCreateDataBoxParameters(
                  await GetShipmentCreateParameters(nodeInfo, ShipmentCreateMode.DataBox),
                  nodeInfo,
                  new ShipmentCreateDataBoxInternal
                  {
                      Components = components,
                      Recipient = recipient,
                      Sender = sender,
                      Subject = subject,
                      AllowSubstDelivery = allowSubstDelivery,
                      LegalTitleLaw = legalTitleLaw,
                      LegalTitlePar = legalTitlePar,
                      LegalTitlePoint = legalTitlePoint,
                      LegalTitleSect = legalTitleSect,
                      LegalTitleYear = legalTitleYear,
                      PersonalDelivery = personalDelivery,
                      Ref = nodeId,
                      ToHands = toHands
                  });

                var shipmentCreate = await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new FormDataParam(new byte[] { 01 }), parameters);

                await CreateShipmentPermission(nodeInfo, shipmentCreate?.Entry?.Id);

                await CreateShipmentComponentAssociation(shipmentCreate?.Entry?.Id, components);

                await _nodesService.CreateSecondaryChildrenAsAdmin(nodeId, new ChildAssociationBody
                {
                    AssocType = SpisumNames.Associations.ShipmentsCreated,
                    ChildId = shipmentCreate?.Entry?.Id
                });

                try
                {
                    var shipmentPid = (await _alfrescoHttpClient.GetNodeInfo(shipmentCreate?.Entry?.Id))?.GetPid();

                    // Log for shipment
                    await _auditLogService.Record(shipmentCreate?.Entry?.Id, SpisumNames.NodeTypes.ShipmentDatabox, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.ShipmentDataBoxCreate);

                    // Log for document
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.ShipmentDatabox, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.ShipmentDataBoxCreate);

                    // Log for file
                    var fileId = await _documentService.GetDocumentFileId(nodeId);
                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentDatabox, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                            TransactinoHistoryMessages.ShipmentDataBoxCreate);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return shipmentCreate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<NodeEntry> CreateShipmentEmail(string nodeId, string recipient, string sender, string subject, List<string> components, string textFilePath)
        {
            var finnalyLock = false;

            try
            {
                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (nodeInfo?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(nodeId);
                    finnalyLock = true;
                }

                var parameters = await GetShipmentCreateEmailParameters(
                    await GetShipmentCreateParameters(nodeInfo, ShipmentCreateMode.Email),
                    nodeInfo,
                    new ShipmentCreateEmailInternal
                    {
                        Components = components,
                        Recipient = recipient,
                        Ref = nodeId,
                        Sender = sender,
                        Subject = subject,
                        TextFilePath = textFilePath
                    });

                // Create shipment
                var shipmentCreate = await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new FormDataParam(new byte[] { 01 }), parameters);

                // Shipment parameters
                await CreateShipmentPermission(nodeInfo, shipmentCreate?.Entry?.Id);

                // Create Association with components
                await CreateShipmentComponentAssociation(shipmentCreate?.Entry?.Id, components);

                // Create association between shipment and parent node (document)
                await _nodesService.CreateSecondaryChildrenAsAdmin(nodeId, new ChildAssociationBody
                {
                    AssocType = SpisumNames.Associations.ShipmentsCreated,
                    ChildId = shipmentCreate?.Entry?.Id
                });

                try
                {
                    var shipmentPid = (await _alfrescoHttpClient.GetNodeInfo(shipmentCreate?.Entry?.Id))?.GetPid();

                    await _auditLogService.Record(shipmentCreate?.Entry?.Id, SpisumNames.NodeTypes.ShipmentEmail, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                            TransactinoHistoryMessages.ShipmentEmailCreate);

                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.ShipmentEmail, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.ShipmentEmailCreate);

                    var fileId = await _documentService.GetDocumentFileId(nodeId);

                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentEmail, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                            TransactinoHistoryMessages.ShipmentEmailCreate);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return shipmentCreate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<NodeEntry> CreateShipmentPersonally(string nodeId, string address1, string address2, string address3, string address4, string addressStreet,
            string addressCity, string addressZip, string addressState)
        {
            var finnalyLock = false;

            try
            {
                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (nodeInfo?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(nodeId);
                    finnalyLock = true;
                }

                var parameters = GetShipmentCreatePersonallyParameters(
                    await GetShipmentCreateParameters(nodeInfo, ShipmentCreateMode.Personally),
                    nodeInfo,
                    new ShipmentCreatePersonalInternal
                    {
                        Address1 = address1,
                        Address2 = address2,
                        Address3 = address3,
                        Address4 = address4,
                        AddressStreet = addressStreet,
                        AddressCity = addressCity,
                        AddressState = addressState,
                        AddressZip = addressZip,
                        Ref = nodeId
                    });

                // Create shipment
                var shipmentCreate = await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new FormDataParam(new byte[] { 01 }), parameters);

                // Shipment parameters
                await CreateShipmentPermission(nodeInfo, shipmentCreate?.Entry?.Id);

                // Create association between shipment and parent node (document)
                await _nodesService.CreateSecondaryChildrenAsAdmin(nodeId, new ChildAssociationBody
                {
                    AssocType = SpisumNames.Associations.ShipmentsCreated,
                    ChildId = shipmentCreate?.Entry?.Id
                });

                try
                {
                    var shipmentPid = (await _alfrescoHttpClient.GetNodeInfo(shipmentCreate?.Entry?.Id))?.GetPid();

                    // Log for shipment
                    await _auditLogService.Record(shipmentCreate?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.ShipmentPersonallyCreate);

                    // Log for document
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.ShipmentPersonallyCreate);

                    // Log for file
                    if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    {
                        var fileId = await _documentService.GetDocumentFileId(nodeId);
                        if (fileId != null)
                            await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                                TransactinoHistoryMessages.ShipmentPersonallyCreate);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return shipmentCreate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<NodeEntry> CreateShipmentPost(string nodeId, string address1, string address2, string address3, string address4, string addressStreet,
            string addressCity, string addressZip, string addressState, string[] postType, string postTypeOther, string postItemType, string postItemTypeOther, double? postItemCashOnDelivery,
            double? postItemStatedPrice)
        {
            var finnalyLock = false;

            try
            {
                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (nodeInfo?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(nodeId);
                    finnalyLock = true;
                }

                var parameters = GetShipmentCreatePostParameters(
                    await GetShipmentCreateParameters(nodeInfo, ShipmentCreateMode.Post),
                    nodeInfo,
                    new ShipmentCreatePostInternal
                    {
                        Address1 = address1,
                        Address2 = address2,
                        Address3 = address3,
                        Address4 = address4,
                        AddressStreet = addressStreet,
                        AddressCity = addressCity,
                        AddressState = addressState,
                        AddressZip = addressZip,
                        PostItemType = postItemType,
                        PostItemTypeOther = postItemTypeOther,
                        PostType = postType,
                        PostTypeOther = postTypeOther,
                        PostItemCashOnDelivery = postItemCashOnDelivery,
                        PostItemStatedPrice = postItemStatedPrice,
                        Ref = nodeId
                    });

                // Create shipment
                var shipmentCreate = await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new FormDataParam(new byte[] { 01 }), parameters);

                // Shipment parameters
                await CreateShipmentPermission(nodeInfo, shipmentCreate?.Entry?.Id);

                // Create association between shipment and parent node (document)
                await _nodesService.CreateSecondaryChildrenAsAdmin(nodeId, new ChildAssociationBody
                {
                    AssocType = SpisumNames.Associations.ShipmentsCreated,
                    ChildId = shipmentCreate?.Entry?.Id
                });

                try
                {
                    var shipmentPid = (await _alfrescoHttpClient.GetNodeInfo(shipmentCreate?.Entry?.Id))?.GetPid();

                    // Log for shipment
                    await _auditLogService.Record(shipmentCreate?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPost, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.ShipmentPostCreate);

                    // Log for document
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.ShipmentPost, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.ShipmentPostCreate);

                    // Log for file
                    if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                    {
                        var fileId = await _documentService.GetDocumentFileId(nodeId);
                        if (fileId != null)
                            await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPost, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                                TransactinoHistoryMessages.ShipmentPostCreate);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                return shipmentCreate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<NodeEntry> CreateShipmentPublish(string nodeId, List<string> components, DateTime dateFrom, int? days, string note)
        {
            var finnalyLock = false;

            try
            {
                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (nodeInfo?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(nodeId);
                    finnalyLock = true;
                }

                var parameters = await GetShipmentCreatePublishParameters(
                    await GetShipmentCreateParameters(nodeInfo, ShipmentCreateMode.Publish),
                    nodeInfo, components, dateFrom, days, note, nodeId);

                // Create shipment
                var shipmentCreate = await _alfrescoHttpClient.CreateNode(AlfrescoNames.Aliases.Root, new FormDataParam(new byte[] { 01 }), parameters);

                // Shipment parameters
                await CreateShipmentPermission(nodeInfo, shipmentCreate?.Entry?.Id);

                // Create Association with components
                await CreateShipmentComponentAssociation(shipmentCreate?.Entry?.Id, components);

                // Create association between shipment and parent node (document)
                await _nodesService.CreateSecondaryChildrenAsAdmin(nodeId, new ChildAssociationBody
                {
                    AssocType = SpisumNames.Associations.ShipmentsCreated,
                    ChildId = shipmentCreate?.Entry?.Id
                });

                try
                {
                    var shipmentPid = (await _alfrescoHttpClient.GetNodeInfo(shipmentCreate?.Entry?.Id))?.GetPid();

                    // Log for shipment
                    await _auditLogService.Record(shipmentCreate?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.ShipmentPublishCreate);

                    // Log for document
                    await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                        TransactinoHistoryMessages.ShipmentPublishCreate);

                    // Log for file
                    var fileId = await _documentService.GetDocumentFileId(nodeId);
                    if (fileId != null)
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Zalozeni,
                            TransactinoHistoryMessages.ShipmentPublishCreate);
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }


                return shipmentCreate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<List<NodeChildAssociationEntry>> GetShipments(string nodeId)
        {
            return await _nodesService.GetSecondaryChildren(nodeId, new List<string> { SpisumNames.Associations.ShipmentsCreated, SpisumNames.Associations.ShipmentsToReturn });
        }

        public async Task<NodeEntry> ShipmentDispatchPost(string shipmentId, string postItemId, string postItemNumber)
        {
            var shipmentInfo = await _alfrescoHttpClient.GetNodeInfo(shipmentId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{ AlfrescoNames.Includes.Permissions }", ParameterType.QueryString)));

            var locallySet = shipmentInfo?.Entry?.Permissions?.LocallySet;
            var updateBody = new NodeBodyUpdateFixed
            {
                Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
            };

            var nodeParent = (await _nodesService.GetParentsByAssociation(shipmentId, new List<string> { SpisumNames.Associations.ShipmentsToDispatch })).FirstOrDefault();
            var parentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeParent?.Entry?.Id, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

            if (parentInfo?.Entry?.IsLocked == true)
                await _nodesService.NodeUnlockAsAdmin(parentInfo?.Entry?.Id);

            await _alfrescoHttpClient.UpdateNode(shipmentId, updateBody
                .AddProperty(SpisumNames.Properties.PostItemId, postItemId)
                .AddProperty(SpisumNames.Properties.PostItemNumber, postItemNumber)
                .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.Dispatched)
                .AddProperty(SpisumNames.Properties.DispatchedDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                .AddProperty(SpisumNames.Properties.ShipmentPostState, SpisumNames.ShipmentPostState.Vypraveno));

            await _nodesService.DeleteSecondaryChildrenAsAdmin(nodeParent?.Entry?.Id, shipmentId, ImmutableList<Parameter>.Empty
                            .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.ShipmentsToDispatch}')", ParameterType.QueryString)));
            await _nodesService.CreateSecondaryChildrenAsAdmin(nodeParent?.Entry?.Id, new ChildAssociationBody
            {
                AssocType = SpisumNames.Associations.ShipmentsDispatched,
                ChildId = shipmentId
            });

            await _nodesService.MoveByPath(shipmentId, SpisumNames.Paths.DispatchDispatched);
            
            var node = await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock
            {
                Type = NodeBodyLockType.FULL
            });

            await AddDispatchedPermissions(parentInfo, shipmentId);

            if (parentInfo?.Entry?.IsLocked == true)
                await _nodesService.NodeLockAsAdmin(parentInfo?.Entry?.Id);

            try
            {
                var shipmentPid = shipmentInfo?.GetPid();

                // Log for shipment
                await _auditLogService.Record(shipmentId, SpisumNames.NodeTypes.ShipmentPost, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                    TransactinoHistoryMessages.ShipmentDispatchPost);

                // Log for document
                await _auditLogService.Record(parentInfo?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPost, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                    TransactinoHistoryMessages.ShipmentDispatchPost);

                var fileId = await _documentService.GetDocumentFileId(parentInfo?.Entry?.Id);

                if (fileId != null)
                    // Log for file
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPost, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                        TransactinoHistoryMessages.ShipmentDispatchPost);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return node;
        }

        public async Task<List<string>> ShipmentsDispatchPublish(List<string> shipmentsIds)
        {
            List<string> unprocessedIds = new List<string>();

            await shipmentsIds.ForEachAsync(async shipmentId =>
            {
                try
                {
                    var shipmentInfo = await _alfrescoHttpClient.GetNodeInfo(shipmentId, ImmutableList<Parameter>.Empty
                              .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{ AlfrescoNames.Includes.Permissions }", ParameterType.QueryString)));

                    #region Validation

                    if (shipmentInfo?.Entry?.Path?.Name != AlfrescoNames.Prefixes.Path + SpisumNames.Paths.DispatchToDispatch || 
                        shipmentInfo?.Entry?.NodeType != SpisumNames.NodeTypes.ShipmentPublish)                    
                    {
                        unprocessedIds.Add(shipmentId);
                        return;
                    }

                    var nodeParents = await _nodesService.GetParentsByAssociation(shipmentId, new List<string> { SpisumNames.Associations.ShipmentsToDispatch });

                    if (nodeParents == null || nodeParents?.Count == 0)
                    {
                        unprocessedIds.Add(shipmentId);
                        return;
                    }

                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeParents?.FirstOrDefault()?.Entry?.Id);

                    #endregion

                    var datetimenow = DateTime.UtcNow.ToAlfrescoDateTimeString();

                    var locallySet = shipmentInfo?.Entry?.Permissions?.LocallySet;
                    var updateBody = new NodeBodyUpdateFixed
                    {
                        Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
                    };

                    var nodeParent = (await _nodesService.GetParentsByAssociation(shipmentId, new List<string> { SpisumNames.Associations.ShipmentsToDispatch })).FirstOrDefault();
                    var parentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeParent?.Entry?.Id, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

                    if (parentInfo?.Entry?.IsLocked == true)
                        await _nodesService.NodeUnlockAsAdmin(parentInfo?.Entry?.Id);

                    await _alfrescoHttpClient.UpdateNode(shipmentId, updateBody
                        .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.Delivered)
                        .AddProperty(SpisumNames.Properties.DispatchedDate, datetimenow)
                        .AddProperty(SpisumNames.Properties.DeliveryDate, datetimenow)
                    );

                    try
                    {
                        var shipmentPid = shipmentInfo?.GetPid();

                        // Log for shipment
                        await _auditLogService.Record(shipmentId, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                            TransactinoHistoryMessages.ShipmentDispatchPublish);

                        // Log for document
                        await _auditLogService.Record(parentInfo?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                            TransactinoHistoryMessages.ShipmentDispatchPublish);

                        var fileId = await _documentService.GetDocumentFileId(parentInfo?.Entry?.Id);

                        if (fileId != null)
                            // Log for file
                            await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                                TransactinoHistoryMessages.ShipmentDispatchPublish);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }

                    await nodeParents.ForEachAsync(async x =>
                    {
                        await _nodesService.DeleteSecondaryChildrenAsAdmin(x?.Entry?.Id, shipmentId, ImmutableList<Parameter>.Empty
                            .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.ShipmentsToDispatch}')", ParameterType.QueryString)));
                        await _nodesService.CreateSecondaryChildrenAsAdmin(x?.Entry?.Id, new ChildAssociationBody
                        {
                            AssocType = SpisumNames.Associations.ShipmentsDispatched,
                            ChildId = shipmentId
                        });
                    });

                    await _nodesService.MoveByPath(shipmentInfo?.Entry?.Id, SpisumNames.Paths.DispatchDispatched);

                    await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock
                    {
                        Type = NodeBodyLockType.FULL
                    });

                    await AddDispatchedPermissions(nodeInfo, shipmentId);

                    if (parentInfo?.Entry?.IsLocked == true)
                        await _nodesService.NodeLockAsAdmin(parentInfo?.Entry?.Id);

                    try
                    {
                        var shipmentPid = shipmentInfo?.GetPid();

                        // Log for shipment
                        await _auditLogService.Record(shipmentId, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Doruceno,
                            TransactinoHistoryMessages.ShipmentDeliveredPublish);

                        // Log for document
                        await _auditLogService.Record(parentInfo?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Doruceno,
                            TransactinoHistoryMessages.ShipmentDeliveredPublish);

                        var fileId = await _documentService.GetDocumentFileId(parentInfo?.Entry?.Id);

                        if (fileId != null)
                            // Log for file
                            await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Doruceno,
                                TransactinoHistoryMessages.ShipmentDeliveredPublish);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    unprocessedIds.Add(shipmentId);
                }
            });

            return unprocessedIds;
        }

        public async Task<List<string>> ShipmentsResend(List<string> shipmentsIds)
        {
            // Merge with shipment return

            List<string> unprocessedIds = new List<string>();

            await shipmentsIds.ForEachAsync(async shipmentId =>
            {
                try
                {
                    var shipmentInfo = await _alfrescoHttpClient.GetNodeInfo(shipmentId, ImmutableList<Parameter>.Empty
                              .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.Permissions}", ParameterType.QueryString)));

                    if (shipmentInfo?.Entry?.Path?.Name != AlfrescoNames.Prefixes.Path + SpisumNames.Paths.DispatchReturned)
                    {
                        unprocessedIds.Add(shipmentId);
                        return;
                    }

                    var nodeParents = await _nodesService.GetParentsByAssociation(shipmentId, new List<string> { SpisumNames.Associations.ShipmentsToReturn });

                    if (nodeParents == null || nodeParents?.Count == 0)
                    {
                        unprocessedIds.Add(shipmentId);
                        return;
                    }

                    var locallySet = shipmentInfo?.Entry?.Permissions?.LocallySet;
                    var updateBody = new NodeBodyUpdateFixed
                    {
                        Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
                    };

                    var nodeParent = nodeParents?.FirstOrDefault();
                    var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeParent?.Entry?.Id);

                    var shipmentAfterUpdate = await _alfrescoHttpClient.UpdateNode(shipmentId, updateBody
                        .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.ToDispatch)
                        .AddProperty(SpisumNames.Properties.ToDispatchDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                        .AddProperty(SpisumNames.Properties.ReasonForReturn, null)
                        .AddProperty(SpisumNames.Properties.ReturnedDate, null)
                    );

                    await nodeParents.ForEachAsync(async x =>
                    {
                        await _nodesService.DeleteSecondaryChildrenAsAdmin(x?.Entry?.Id, shipmentId, ImmutableList<Parameter>.Empty
                            .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.ShipmentsToReturn}')", ParameterType.QueryString)));
                        await _nodesService.CreateSecondaryChildrenAsAdmin(x?.Entry?.Id, new ChildAssociationBody
                        {
                            AssocType = SpisumNames.Associations.ShipmentsToDispatch,
                            ChildId = shipmentId
                        });
                    });

                    await _nodesService.MoveByPath(shipmentInfo?.Entry?.Id, SpisumNames.Paths.DispatchToDispatch);

                    await AddToDispatchPermissions(nodeInfo, shipmentId);

                    try
                    {
                        var shipmentPid = shipmentAfterUpdate?.GetPid();

                        // Log for shipment
                        await _auditLogService.Record(shipmentAfterUpdate?.Entry?.Id, nodeInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                            TransactinoHistoryMessages.ShipmentResend);

                        // Log for document
                        await _auditLogService.Record(nodeInfo?.Entry?.Id, nodeInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                            TransactinoHistoryMessages.ShipmentResend);

                        // Log for file
                        if (nodeInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                        {
                            var fileId = await _documentService.GetDocumentFileId(nodeInfo?.Entry?.Id);
                            if (fileId != null)
                                await _auditLogService.Record(fileId, nodeInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                                    TransactinoHistoryMessages.ShipmentResend);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    unprocessedIds.Add(shipmentId);
                }
            });

            return unprocessedIds;
        }

        public async Task<List<string>> ShipmentsReturn(string reason, List<string> shipmentsIds)
        {
            // Merge with Shipment resend

            List<string> unprocessedIds = new List<string>();

            await shipmentsIds.ForEachAsync(async shipmentId =>
            {
                try
                {
                    #region Validation

                    var shipmentInfo = await _alfrescoHttpClient.GetNodeInfo(shipmentId, ImmutableList<Parameter>.Empty
                                  .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{AlfrescoNames.Includes.Permissions},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

                    if (shipmentInfo?.Entry?.Path?.Name != AlfrescoNames.Prefixes.Path + SpisumNames.Paths.DispatchToDispatch)
                    {
                        unprocessedIds.Add(shipmentId);
                        return;
                    }

                    var nodeParents = (await _nodesService.GetParentsByAssociation(shipmentId, new List<string> { SpisumNames.Associations.ShipmentsToDispatch },
                        ImmutableList<Parameter>.Empty
                                  .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString))))?.FirstOrDefault();

                    if (nodeParents == null)
                    {
                        unprocessedIds.Add(shipmentId);
                        return;
                    }

                    #endregion

                    if (nodeParents?.Entry?.IsLocked == true)
                        await _nodesService.NodeUnlockAsAdmin(nodeParents?.Entry?.Id);

                    var locallySet = shipmentInfo?.Entry?.Permissions?.LocallySet;
                    var updateBody = new NodeBodyUpdateFixed
                    {
                        Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
                    };

                    var documentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeParents.Entry?.Id);

                    if (shipmentInfo?.Entry?.IsLocked == true)
                        await _alfrescoHttpClient.NodeUnlock(shipmentInfo?.Entry?.Id);

                    var shipmentAfterUpdate = await _alfrescoHttpClient.UpdateNode(shipmentId, updateBody
                         .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.Returned)
                         .AddProperty(SpisumNames.Properties.ReasonForReturn, reason)
                         .AddProperty(SpisumNames.Properties.ReturnedDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                    );

                    await CreateShipmentPermission(documentInfo, shipmentId);

                    await _nodesService.DeleteSecondaryChildrenAsAdmin(nodeParents?.Entry?.Id, shipmentId, ImmutableList<Parameter>.Empty
                            .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.ShipmentsToDispatch}')", ParameterType.QueryString)));

                    await _nodesService.CreateSecondaryChildrenAsAdmin(nodeParents?.Entry?.Id, new ChildAssociationBody
                    {
                        AssocType = SpisumNames.Associations.ShipmentsToReturn,
                        ChildId = shipmentId
                    });

                    await _nodesService.MoveByPath(shipmentInfo?.Entry?.Id, SpisumNames.Paths.DispatchReturned);

                    if (nodeParents?.Entry?.IsLocked == true)
                        await _nodesService.NodeLockAsAdmin(nodeParents?.Entry?.Id);

                    try
                    {
                        var shipmentPid = shipmentAfterUpdate?.GetPid();

                        // Log for shipment
                        await _auditLogService.Record(shipmentAfterUpdate?.Entry?.Id, shipmentAfterUpdate?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.VyjmutiZVypraveni,
                            string.Format(TransactinoHistoryMessages.ShipmentReturn, reason));

                        // Log for document
                        await _auditLogService.Record(documentInfo?.Entry?.Id, shipmentAfterUpdate?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.VyjmutiZVypraveni,
                            string.Format(TransactinoHistoryMessages.ShipmentReturn, reason));

                        // Log for file
                        if (documentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                        {
                            var fileId = await _documentService.GetDocumentFileId(documentInfo?.Entry?.Id);
                            if (fileId != null)
                                await _auditLogService.Record(fileId, shipmentAfterUpdate?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.VyjmutiZVypraveni,
                                    string.Format(TransactinoHistoryMessages.ShipmentReturn, reason));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger?.Error(ex, "Audit log failed");
                    }
                }
                catch
                {
                    unprocessedIds.Add(shipmentId);
                }
            });

            return unprocessedIds;
        }

        public async Task<List<string>> ShipmentsSend(string documentId, List<string> shipmentsId)
        {
            List<string> unprocessedIds = new List<string>();

            foreach (string shipmentId in shipmentsId)
                try
                {
                    var shipmentInfo = await _alfrescoHttpClient.GetNodeInfo(shipmentId, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Path},{ AlfrescoNames.Includes.Permissions }", ParameterType.QueryString)));

                    if (shipmentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.ShipmentPost ||
                        shipmentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.ShipmentPublish)
                        await ShipmentSendPostPublish(documentId, shipmentInfo);
                    else if (shipmentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.ShipmentEmail)
                        await ShipmentSendEmailDatabox(documentId, shipmentInfo, EmailOrDataboxEnum.Email);
                    else if (shipmentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.ShipmentDatabox)
                        await ShipmentSendEmailDatabox(documentId, shipmentInfo, EmailOrDataboxEnum.Databox);
                    else
                        await ShipmentSendPersonally(documentId, shipmentInfo);
                }
                catch
                {
                    // Unable to process
                    unprocessedIds.Add(shipmentId);
                }

            return unprocessedIds;
        }

        public async Task<NodeEntry> UpdateShipmentDataBox(string shipmentId, List<string> componentsId, bool allowSubstDelivery, string legalTitleLaw, string legalTitleYear, string legalTitleSect,
            string legalTitlePar, string legalTitlePoint, bool personalDelivery, string recipient, string sender, string subject, string toHands)
        {
            var lockedComponents = new List<string>();
            var finnalyLock = false;

            try
            {
                var shipmentBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(shipmentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (shipmentBeforeUpdate?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(shipmentBeforeUpdate.Entry.Id);
                    finnalyLock = true;
                }

                var componentsInfo = await _nodesService.GetNodesInfo(componentsId);
                var isShipmentToDispatch = await IsShipmentToDispatch(shipmentId);

                var parentDocument = await GetShipmentDocument(shipmentId);

                if (IsComponentsMaximumSizeReached(componentsInfo, 50))
                    throw new BadRequestException(ErrorCodes.V_MAX_SIZE);
                if (!await ComponentsBelongToDocument(parentDocument?.Entry?.Id, componentsId))
                    throw new BadRequestException("", $"Component is not associated with provided shipment {shipmentId}");

                var shipmentAfterUpdate = await _alfrescoHttpClient.UpdateNode(shipmentId, new NodeBodyUpdate()
                   .AddProperty(SpisumNames.Properties.Recipient, recipient)
                   .AddProperty(SpisumNames.Properties.Sender, sender)
                   .AddProperty(SpisumNames.Properties.Subject, subject)
                   .AddProperty(SpisumNames.Properties.ShComponentsRef, string.Join(",", componentsId.ToArray()))
                   .AddProperty(SpisumNames.Properties.ShFilesSize, GetshFileSize(componentsInfo))
                   .AddProperty(SpisumNames.Properties.AllowSubstDelivery, allowSubstDelivery)
                   .AddProperty(SpisumNames.Properties.LegalTitleLaw, legalTitleLaw)
                   .AddProperty(SpisumNames.Properties.LegalTitleYear, legalTitleYear)
                   .AddProperty(SpisumNames.Properties.LegalTitleSect, legalTitleSect)
                   .AddProperty(SpisumNames.Properties.LegalTitlePar, legalTitlePar)
                   .AddProperty(SpisumNames.Properties.LegalTitlePoint, legalTitlePoint)
                   .AddProperty(SpisumNames.Properties.PersonalDelivery, personalDelivery)
                   .AddProperty(SpisumNames.Properties.ToHands, toHands));

                lockedComponents = await UnlockAllAssociations(shipmentId);

                await DeleteOldComponentAssociation(shipmentId, SpisumNames.Associations.ShipmentsComponents, componentsId);

                try
                {
                    var shipmentPid = shipmentBeforeUpdate?.GetPid();

                    var difference = _alfrescoModelComparer.CompareProperties(
                        shipmentBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        shipmentAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary());

                    if (difference.Count > 0)
                    {
                        try
                        {
                            var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                            if (componentsJson != null)
                                difference.Remove(componentsJson);
                        }
                        catch { }

                        string message = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.ShipmentDataBoxUpdate, difference);

                        // Log for shipment
                        await _auditLogService.Record(shipmentId, SpisumNames.NodeTypes.ShipmentDatabox, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);

                        // Log for document
                        await _auditLogService.Record(parentDocument?.Entry?.Id, SpisumNames.NodeTypes.ShipmentDatabox, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);

                        var fileId = await _documentService.GetDocumentFileId(parentDocument?.Entry?.Id);

                        if (fileId != null)
                            // Log for file
                            await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentDatabox, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                if (isShipmentToDispatch)
                    return await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));

                return shipmentAfterUpdate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (lockedComponents.Count > 0)
                    await lockedComponents.ForEachAsync(async locNodeId => await _alfrescoHttpClient.NodeLock(locNodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL)));
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<NodeEntry> UpdateShipmentEmail(string shipmentId, string recipient, string sender, string subject, List<string> componentsId, string textFilePath)
        {
            var lockedComponents = new List<string>();
            var finnalyLock = false;

            try
            {
                var shipmentBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(shipmentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (shipmentBeforeUpdate?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(shipmentBeforeUpdate.Entry.Id);
                    finnalyLock = true;
                }

                var componentsInfo = await _nodesService.GetNodesInfo(componentsId);
                var isShipmentToDispatch = await IsShipmentToDispatch(shipmentId);

                var parentDocument = await GetShipmentDocument(shipmentId);

                if (IsComponentsMaximumSizeReached(componentsInfo, 10))
                    throw new BadRequestException(ErrorCodes.V_MAX_SIZE);
                if (!await ComponentsBelongToDocument(parentDocument?.Entry?.Id, componentsId))
                    throw new BadRequestException("", $"Component is not associated with provided shipment {shipmentId}");

                string emailText = await GetEmailTextFile(textFilePath, shipmentId, subject);

                var shipmentAfterUpdate = await _alfrescoHttpClient.UpdateNode(shipmentId, new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.Recipient, recipient)
                    .AddProperty(SpisumNames.Properties.Sender, sender)
                    .AddProperty(SpisumNames.Properties.Subject, subject)
                    .AddProperty(SpisumNames.Properties.ShComponentsRef, string.Join(",", componentsId.ToArray()))
                    .AddProperty(SpisumNames.Properties.ShEmailBody, emailText)
                    .AddProperty(SpisumNames.Properties.ShFilesSize, GetshFileSize(componentsInfo)));

                lockedComponents = await UnlockAllAssociations(shipmentId);

                await DeleteOldComponentAssociation(shipmentId, SpisumNames.Associations.ShipmentsComponents, componentsId);

                try
                {
                    var shipmentPid = shipmentBeforeUpdate?.GetPid();

                    var difference = _alfrescoModelComparer.CompareProperties(
                        shipmentBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        shipmentAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary());

                    if (difference.Count > 0)
                    {
                        try
                        {
                            var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                            if (componentsJson != null)
                                difference.Remove(componentsJson);
                        }
                        catch { }

                        string message = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.ShipmentEmailUpdate, difference);

                        // Log for shipment
                        await _auditLogService.Record(shipmentId, SpisumNames.NodeTypes.ShipmentEmail, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);

                        // Log for document
                        await _auditLogService.Record(parentDocument?.Entry?.Id, SpisumNames.NodeTypes.ShipmentEmail, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);

                        var fileId = await _documentService.GetDocumentFileId(parentDocument?.Entry?.Id);

                        if (fileId != null)
                            // Log for file
                            await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentEmail, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                if (isShipmentToDispatch)
                    return await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));

                return shipmentAfterUpdate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (lockedComponents.Count > 0)
                    await lockedComponents.ForEachAsync(async locNodeId => await _alfrescoHttpClient.NodeLock(locNodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL)));
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<NodeEntry> UpdateShipmentPersonally(string shipmentId, string address1, string address2, string address3, string address4, string addressStreet,
           string addressCity, string addressZip, string addressState)
        {
            var finnalyLock = false;

            try
            {
                var shipmentBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(shipmentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (shipmentBeforeUpdate?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(shipmentId);
                    finnalyLock = true;
                }

                var isShipmentToDispatch = await IsShipmentToDispatch(shipmentId);

                var parentDocument = await GetShipmentDocument(shipmentId);

                var shipmentAfterUpdate = await _alfrescoHttpClient.UpdateNode(shipmentId, new NodeBodyUpdate()
                  .AddProperty(SpisumNames.Properties.Address1, address1)
                  .AddProperty(SpisumNames.Properties.Address2, address2)
                  .AddProperty(SpisumNames.Properties.Address3, address3)
                  .AddProperty(SpisumNames.Properties.Address4, address4)
                  .AddProperty(SpisumNames.Properties.AddressStreet, addressStreet)
                  .AddProperty(SpisumNames.Properties.AddressCity, addressCity)
                  .AddProperty(SpisumNames.Properties.AddressZip, addressZip)
                  .AddProperty(SpisumNames.Properties.AddressState, addressState));

                try
                {
                    var shipmentPid = shipmentBeforeUpdate?.GetPid();

                    var difference = _alfrescoModelComparer.CompareProperties(
                        shipmentBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        shipmentAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary());

                    if (difference.Count > 0)
                    {
                        try
                        {
                            var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                            if (componentsJson != null)
                                difference.Remove(componentsJson);
                        }
                        catch { }

                        string message = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.ShipmentPersonallyUpdate, difference);

                        // Log for shipment
                        await _auditLogService.Record(shipmentId, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);

                        // Log for document
                        await _auditLogService.Record(parentDocument?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);

                        var fileId = await _documentService.GetDocumentFileId(parentDocument?.Entry?.Id);

                        if (fileId != null)
                            // Log for file
                            await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                if (isShipmentToDispatch)
                    return await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));

                return shipmentAfterUpdate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<NodeEntry> UpdateShipmentPost(string shipmentId, string address1, string address2, string address3, string address4, string addressStreet,
            string addressCity, string addressZip, string addressState, string[] postType, string postTypeOther, string postItemType, string postItemTypeOther, double? postItemWeight,
            double? postItemPrice, string postItemNumber, string postItemId, double? postItemCashOnDelivery,
            double? postItemStatedPrice)
        {
            var finnalyLock = false;

            try
            {
                var shipmentBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(shipmentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (shipmentBeforeUpdate?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(shipmentId);
                    finnalyLock = true;
                }

                var isShipmentToDispatch = await IsShipmentToDispatch(shipmentId);

                double? price = null;
                if (postItemPrice.HasValue)
                    price = Math.Round(postItemPrice.Value, 2);

                var parentDocument = await GetShipmentDocument(shipmentId);

                var shipmentAfterUpdate = await _alfrescoHttpClient.UpdateNode(shipmentId, new NodeBodyUpdate()
                  .AddProperty(SpisumNames.Properties.Address1, address1)
                  .AddProperty(SpisumNames.Properties.Address2, address2)
                  .AddProperty(SpisumNames.Properties.Address3, address3)
                  .AddProperty(SpisumNames.Properties.Address4, address4)
                  .AddProperty(SpisumNames.Properties.AddressStreet, addressStreet)
                  .AddProperty(SpisumNames.Properties.AddressCity, addressCity)
                  .AddProperty(SpisumNames.Properties.AddressZip, addressZip)
                  .AddProperty(SpisumNames.Properties.AddressState, addressState)
                  .AddProperty(SpisumNames.Properties.PostType, string.Join(",", postType))
                  .AddProperty(SpisumNames.Properties.PostTypeOther, postTypeOther)
                  .AddProperty(SpisumNames.Properties.PostItemType, postItemType)
                  .AddProperty(SpisumNames.Properties.PostItemTypeOther, postItemTypeOther)
                  .AddProperty(SpisumNames.Properties.PostItemWeight, postItemWeight)
                  .AddProperty(SpisumNames.Properties.PostItemPrice, price)
                  .AddProperty(SpisumNames.Properties.PostItemNumber, postItemNumber)
                  .AddProperty(SpisumNames.Properties.PostItemId, postItemId)
                  .AddProperty(SpisumNames.Properties.PostItemCashOnDelivery, postItemCashOnDelivery.ToString().Replace(",", "."))
                  .AddProperty(SpisumNames.Properties.PostItemStatedPrice, postItemStatedPrice.ToString().Replace(",", ".")));

                try
                {
                    var shipmentPid = shipmentBeforeUpdate?.GetPid();

                    var difference = _alfrescoModelComparer.CompareProperties(
                        shipmentBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        shipmentAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary());

                    if (difference.Count > 0)
                    {
                        try
                        {
                            var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                            if (componentsJson != null)
                                difference.Remove(componentsJson);
                        }
                        catch { }

                        string message = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.ShipmentPostUpdate, difference);

                        // Log for shipment
                        await _auditLogService.Record(shipmentId, SpisumNames.NodeTypes.ShipmentPost, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);

                        // Log for document
                        await _auditLogService.Record(parentDocument?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPost, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);

                        var fileId = await _documentService.GetDocumentFileId(parentDocument?.Entry?.Id);

                        if (fileId != null)
                            // Log for file
                            await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPost, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                if (isShipmentToDispatch)
                    return await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));

                return shipmentAfterUpdate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        public async Task<NodeEntry> UpdateShipmentPublish(string shipmentId, List<string> componentsId, DateTime dateFrom, int? days, string note)
        {
            var lockedComponents = new List<string>();
            var finnalyLock = false;

            try
            {
                var shipmentBeforeUpdate = await _alfrescoHttpClient.GetNodeInfo(shipmentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (shipmentBeforeUpdate?.Entry?.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(shipmentBeforeUpdate.Entry.Id);
                    finnalyLock = true;
                }

                var componentsInfo = await _nodesService.GetNodesInfo(componentsId);
                var isShipmentToDispatch = await IsShipmentToDispatch(shipmentId);

                var parentDocument = await GetShipmentDocument(shipmentId);

                if (!await ComponentsBelongToDocument(parentDocument?.Entry?.Id, componentsId))
                    throw new BadRequestException("", $"Component is not associated with provided shipment {shipmentId}");

                lockedComponents = await UnlockAllAssociations(shipmentId);

                await DeleteOldComponentAssociation(shipmentId, SpisumNames.Associations.ShipmentsComponents, componentsId);

                var body = new NodeBodyUpdate()
                    .AddProperty(SpisumNames.Properties.ShComponentsRef, string.Join(",", componentsId.ToArray()))
                    .AddProperty(SpisumNames.Properties.ShFilesSize, GetshFileSize(componentsInfo))
                    .AddProperty(SpisumNames.Properties.DateFrom, dateFrom.ToAlfrescoDateTimeString())
                    .AddProperty(SpisumNames.Properties.Note, note);
            
                if (days != null)
                    body.AddProperty(SpisumNames.Properties.DateTo, dateFrom.AddDays(days.Value).ToAlfrescoDateTimeString());
                else
                    body.AddProperty(SpisumNames.Properties.DateTo, null);

                var shipmentAfterUpdate = await _alfrescoHttpClient.UpdateNode(shipmentId, body);

                try
                {
                    var shipmentPid = shipmentBeforeUpdate?.GetPid();

                    var difference = _alfrescoModelComparer.CompareProperties(
                        shipmentBeforeUpdate?.Entry?.Properties?.As<JObject>().ToDictionary(),
                        shipmentAfterUpdate?.Entry?.Properties?.As<JObject>().ToDictionary());

                    if (difference.Count > 0)
                    {
                        try
                        {
                            var componentsJson = difference.FirstOrDefault(x => x.Key == SpisumNames.Properties.ComponentVersionJSON);
                            if (componentsJson != null)
                                difference.Remove(componentsJson);
                        }
                        catch { }

                        string message = TransactinoHistoryMessages.GetMessagePropertiesChange(TransactinoHistoryMessages.ShipmentPublishUpdate, difference);

                        // Log for shipment
                        await _auditLogService.Record(shipmentId, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);

                        // Log for document
                        await _auditLogService.Record(parentDocument?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);

                        var fileId = await _documentService.GetDocumentFileId(parentDocument?.Entry?.Id);

                        if (fileId != null)
                            // Log for file
                            await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Uprava, message);
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger?.Error(ex, "Audit log failed");
                }

                if (isShipmentToDispatch)
                    return await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));

                return shipmentAfterUpdate;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (lockedComponents.Count > 0)
                    await lockedComponents.ForEachAsync(async locNodeId => await _alfrescoHttpClient.NodeLock(locNodeId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL)));
                if (finnalyLock)
                    await _alfrescoHttpClient.NodeLock(shipmentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            }
        }

        #endregion

        #region Private Methods

        private async Task AddDispatchedPermissions(NodeEntry nodeParent, string shipmentId)
        {
            var properties = nodeParent?.Entry?.Properties.As<JObject>().ToDictionary();
            var group = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

            var body = _nodesService.SetPermissions(group, _identityUser.Id);

            body.RemovePermission(SpisumNames.Groups.DispatchGroup);
            body.AddPermission(SpisumNames.Groups.DispatchGroup, $"{GroupPermissionTypes.Consumer}");

            body.RemovePermission($"{SpisumNames.Prefixes.UserGroup}{_identityUser.Id}");
            body.AddPermission($"{SpisumNames.Prefixes.UserGroup}{_identityUser.Id}", $"{GroupPermissionTypes.Consumer}");

            await _nodesService.UpdateNodeAsAdmin(shipmentId, body);
        }

        private async Task AddDispatchGroupPermissions(NodeEntry nodeParent, string shipmentId, GroupPermissionTypes type)
        {
            var properties = nodeParent?.Entry?.Properties.As<JObject>().ToDictionary();
            var group = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

            var body = _nodesService.SetPermissions(group, _identityUser.Id);

            body.RemovePermission(SpisumNames.Groups.DispatchGroup);
            body.AddPermission(SpisumNames.Groups.DispatchGroup, $"{type}");

            await _alfrescoHttpClient.UpdateNode(shipmentId, body);
        }

        private async Task AddToDispatchPermissions(NodeEntry nodeParent, string shipmentId)
        {
            var properties = nodeParent?.Entry?.Properties.As<JObject>().ToDictionary();
            var group = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

            var body = _nodesService.SetPermissions(group, _identityUser.Id);
            
            body.RemovePermission(SpisumNames.Groups.DispatchGroup);
            body.AddPermission(SpisumNames.Groups.DispatchGroup, $"{GroupPermissionTypes.Coordinator}");

            body.RemovePermission($"{SpisumNames.Prefixes.UserGroup}{_identityUser.Id}");
            body.AddPermission($"{SpisumNames.Prefixes.UserGroup}{_identityUser.Id}", $"{GroupPermissionTypes.Consumer}");

            await _alfrescoHttpClient.UpdateNode(shipmentId, body);
        }

        private async Task<bool> ComponentsBelongToDocument(string documentId, List<string> componentsIds)
        {
            var childrens = await _nodesService.GetSecondaryChildren(documentId, SpisumNames.Associations.Components);

            if (childrens.Count == 0)
                return false;

            foreach (var componentId in componentsIds)
                if (!childrens.Any(x => x.Entry.Id.Contains(componentId)))
                    return false;

            return true;
        }

        private async Task CreateShipmentComponentAssociation(string shipmentId, List<string> components)
        {
            await components.ForEachAsync(async componentId =>
            {
                var componentInfo = await _alfrescoHttpClient.GetNodeInfo(componentId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (componentInfo?.Entry?.IsLocked == true)
                    await _alfrescoHttpClient.NodeUnlock(componentId);

                await _nodesService.CreateSecondaryChildrenAsAdmin(shipmentId, new ChildAssociationBody
                {
                    AssocType = SpisumNames.Associations.ShipmentsComponents,
                    ChildId = componentId
                });

                if (componentInfo?.Entry?.IsLocked == true)
                    await _alfrescoHttpClient.NodeLock(componentId, new NodeBodyLock().AddLockType(NodeBodyLockType.FULL));
            });
        }

        private async Task CreateShipmentPermission(NodeEntry nodeEntry, string shipmentId)
        {
            var properties = nodeEntry?.Entry?.Properties.As<JObject>().ToDictionary();
            var group = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

            var body =  _nodesService.SetPermissions(group, _identityUser.Id);

            await _nodesService.UpdateNodeAsAdmin(shipmentId, body);
        }

        private async Task DeleteOldComponentAssociation(string nodeId, string association, List<string> newComponentsIds)
        {
            var existingShipmentChildrens = await _nodesService.GetSecondaryChildren(nodeId, association);

            // Delete those, who are not in the list
            foreach (var children in existingShipmentChildrens)
                if (newComponentsIds.Contains(children.Entry.Id))
                    continue;
                else
                    await _alfrescoHttpClient.DeleteSecondaryChildren(nodeId, children?.Entry?.Id, ImmutableList<Parameter>.Empty
                        .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{association}')", ParameterType.QueryString)));

            foreach (var newComponentId in newComponentsIds.Where(x => !existingShipmentChildrens.Any(y => y.Entry.Id == x)))
                await _alfrescoHttpClient.CreateNodeSecondaryChildren(nodeId, new ChildAssociationBody
                {
                    AssocType = association,
                    ChildId = newComponentId
                });
        }

        private async Task EmailDataBoxSendFailedAction(string nodeId, NodeEntry shipmentInfo)
        {
            await _alfrescoHttpClient.UpdateNode(shipmentInfo?.Entry?.Id, new NodeBodyUpdate()
               .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.Created)
               .AddProperty(SpisumNames.Properties.ToDispatchDate, null)
               );

            var parentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            try
            {
                var shipmentPid = shipmentInfo?.GetPid();

                // Log for shipment
                await _auditLogService.Record(shipmentInfo?.Entry?.Id, shipmentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.VyjmutiZVypraveni,
                    TransactinoHistoryMessages.ShipmentSendEmailDataBoxFailed);

                // Log for document
                await _auditLogService.Record(parentInfo?.Entry?.Id, parentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.VyjmutiZVypraveni,
                    TransactinoHistoryMessages.ShipmentSendEmailDataBoxFailed);

                if (parentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                {
                    var fileId = await _documentService.GetDocumentFileId(nodeId);
                    if (fileId != null)
                        // Log for file
                        await _auditLogService.Record(fileId, shipmentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.VyjmutiZVypraveni,
                            TransactinoHistoryMessages.ShipmentSendEmailDataBoxFailed);
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        private long? GetComponentsSize(List<NodeEntry> components)
        {
            return components.Sum(x => x.Entry?.Content?.SizeInBytes);
        }

        private async Task<string> GetEmailTextFile(string path, string nodeId, string subject)
        {
            var nodeEntry = await _alfrescoHttpClient.GetNodeInfo(nodeId);

            return GetEmailTextFile(
                path,
                nodeEntry,
                subject
            );
        }

        private string GetEmailTextFile(string path, NodeEntry nodeInfo, string subject)
        {
            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();

            return GetEmailTextFile(
                path,
                subject,
                properties.GetNestedValueOrDefault(SpisumNames.Properties.Ssid)?.ToString(),
                properties.GetNestedValueOrDefault(SpisumNames.Properties.FileIdentificator)?.ToString(),
                properties.GetNestedValueOrDefault(SpisumNames.Properties.SenderSSID)?.ToString(),
                properties.GetNestedValueOrDefault(SpisumNames.Properties.SenderIdent)?.ToString(),
                properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "displayName")?.ToString());
        }

        private string GetEmailTextFile(string path, string subject, string ssid, string fileIdentificator, string senderSsid, string senderIdent, string nodeOwnerDisplayName)
        {
            return File.ReadAllText(path)
                   .Replace("[emailSubject]", subject)
                   .Replace("[CisloJednaci]", ssid)
                   .Replace("[SpisovaZnacka]", fileIdentificator)
                   .Replace("[CjOdesilatel]", senderSsid)
                   .Replace("[tEvidencniCislo]", senderIdent)
                   .Replace("[VlastniKdo]", nodeOwnerDisplayName);
        }

        private string GetshFileSize(List<NodeEntry> componentsInfo)
        {
            var result = GetComponentsSize(componentsInfo);

            return GetshFileSizeFormat(result?.ConvertBytesToKilobytes());
        }

        private string GetshFileSizeFormat(double? componentsTotalSizeKb)
        {
            return componentsTotalSizeKb.ToString().Replace(",", ".");
        }

        private async Task<ImmutableList<Parameter>> GetShipmentCreateDataBoxParameters(ImmutableList<Parameter> parameters, NodeEntry nodeInfo, ShipmentCreateDataBoxInternal body)
        {
            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();
            var components = body.Components;
            var maximumSizeMB = body.MaximumComponentSizeMB;

            // Validate components
            var componentValidationReult = await ValidateComponents(components, nodeInfo.Entry.Id, maximumSizeMB);
            var componentsTotalSize = componentValidationReult.TotalSizeKiloBytes;

            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Recipient, body.Recipient, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Sender, body.Sender, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Subject, body.Subject, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShRef, properties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString(), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShRefId, nodeInfo?.Entry?.Id, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShComponentsRef, string.Join(",", components.ToArray()), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShFilesSize, GetshFileSizeFormat(componentsTotalSize), ParameterType.GetOrPost));            
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Ref, body.Ref, ParameterType.GetOrPost));

            // Special for databox
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.AllowSubstDelivery, body.AllowSubstDelivery, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.LegalTitleLaw, body.LegalTitleLaw, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.LegalTitleYear, body.LegalTitleYear, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.LegalTitleSect, body.LegalTitleSect, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.LegalTitlePar, body.LegalTitlePar, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.LegalTitlePoint, body.LegalTitlePoint, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.PersonalDelivery, body.PersonalDelivery, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ToHands, body.ToHands, ParameterType.GetOrPost));

            return parameters;
        }

        private async Task<ImmutableList<Parameter>> GetShipmentCreateEmailParameters(ImmutableList<Parameter> parameters, NodeEntry nodeInfo, ShipmentCreateEmailInternal body)
        {
            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();

            var components = body.Components;
            var maximumSizeMB = body.MaximumComponentSizeMB;

            // Validate components
            var componentValidationResult = await ValidateComponents(components, nodeInfo.Entry.Id, maximumSizeMB);
            var componentsTotalSize = componentValidationResult.TotalSizeKiloBytes;

            var emailText = GetEmailTextFile(body.TextFilePath, nodeInfo, body.Subject);

            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShEmailBody, emailText, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Recipient, body.Recipient, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Sender, body.Sender, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Subject, body.Subject, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShRef, properties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString(), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShRefId, nodeInfo?.Entry?.Id, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShComponentsRef, string.Join(",", components.ToArray()), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShFilesSize, GetshFileSizeFormat(componentsTotalSize), ParameterType.GetOrPost));            
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Ref, body.Ref, ParameterType.GetOrPost));

            return parameters;
        }

        private async Task<ImmutableList<Parameter>> GetShipmentCreateParameters(NodeEntry nodeInfo, ShipmentCreateMode mode)
        {
            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();

            var nodeOwnerId = properties.GetNestedValueOrDefault(AlfrescoNames.ContentModel.Owner, "id")?.ToString();
            var ssid = properties.GetNestedValueOrDefault(SpisumNames.Properties.Ssid)?.ToString();
            var fileIdentificator = properties.GetNestedValueOrDefault(SpisumNames.Properties.FileIdentificator)?.ToString();
            var senderSsid = properties.GetNestedValueOrDefault(SpisumNames.Properties.SenderSSID)?.ToString();
            var senderIdent = properties.GetNestedValueOrDefault(SpisumNames.Properties.SenderIdent)?.ToString();
            var shipmentPid = await _componentService.GenerateComponentPID(nodeInfo.Entry.Id, "/Z", GeneratePIDComponentType.Shipment);
            var group = properties.GetNestedValueOrDefault(SpisumNames.Properties.Group)?.ToString();

            return ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.RelativePath, SpisumNames.Paths.DispatchCreated, ParameterType.GetOrPost))
                    .Add(new Parameter(AlfrescoNames.Headers.NodeType, GetShipmentNodeType(mode), ParameterType.GetOrPost))
                    .Add(new Parameter(AlfrescoNames.ContentModel.Owner, nodeOwnerId, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Group, group, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.InternalState, SpisumNames.InternalState.Created, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Pid, shipmentPid, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.Ssid, ssid, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.FileIdentificator, fileIdentificator, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.SenderSSID, senderSsid, ParameterType.GetOrPost))
                    .Add(new Parameter(SpisumNames.Properties.SenderIdent, senderIdent, ParameterType.GetOrPost))
                ;
        }

        private ImmutableList<Parameter> GetShipmentCreatePersonallyParameters(ImmutableList<Parameter> parameters, NodeEntry nodeInfo, ShipmentCreatePersonalInternal body)
        {
            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();

            var subject = properties.GetNestedValueOrDefault(SpisumNames.Properties.Subject)?.ToString();

            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Subject, subject, ParameterType.GetOrPost));

            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShRef, properties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString(), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShRefId, nodeInfo?.Entry?.Id, ParameterType.GetOrPost));

            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Address1, body.Address1, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Address2, body.Address2, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Address3, body.Address3, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Address4, body.Address4, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.AddressStreet, body.AddressStreet, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.AddressCity, body.AddressCity, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.AddressZip, body.AddressZip, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.AddressState, body.AddressState, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Ref, body.Ref, ParameterType.GetOrPost));

            return parameters;
        }

        private ImmutableList<Parameter> GetShipmentCreatePostParameters(ImmutableList<Parameter> parameters, NodeEntry nodeInfo, ShipmentCreatePostInternal body)
        {
            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();

            var subject = properties.GetNestedValueOrDefault(SpisumNames.Properties.Subject)?.ToString();

            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Subject, subject, ParameterType.GetOrPost));

            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShRef, properties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString(), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShRefId, nodeInfo?.Entry?.Id, ParameterType.GetOrPost));

            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Address1, body.Address1, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Address2, body.Address2, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Address3, body.Address3, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Address4, body.Address4, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.AddressStreet, body.AddressStreet, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.AddressCity, body.AddressCity, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.AddressZip, body.AddressZip, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.AddressState, body.AddressState, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Ref, body.Ref, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShipmentPostState, SpisumNames.ShipmentPostState.Nevypraveno, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.PostType, string.Join(",", body.PostType), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.PostTypeOther, body.PostTypeOther, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.PostItemType, body.PostItemType, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.PostItemTypeOther, body.PostItemTypeOther, ParameterType.GetOrPost));

            parameters = parameters.Add(body.PostItemCashOnDelivery != null ? 
                new Parameter(SpisumNames.Properties.PostItemCashOnDelivery, body.PostItemCashOnDelivery?.ToString().Replace(",", "."), ParameterType.GetOrPost) : 
                new Parameter(SpisumNames.Properties.PostItemCashOnDelivery, null, ParameterType.GetOrPost));

            parameters = parameters.Add(body.PostItemStatedPrice != null ?
                new Parameter(SpisumNames.Properties.PostItemStatedPrice, body.PostItemStatedPrice?.ToString().Replace(",", "."), ParameterType.GetOrPost) :
                new Parameter(SpisumNames.Properties.PostItemStatedPrice, null, ParameterType.GetOrPost));            

            return parameters;
        }

        private async Task<ImmutableList<Parameter>> GetShipmentCreatePublishParameters(ImmutableList<Parameter> parameters, NodeEntry nodeInfo, List<string> componentsId, DateTime dateFrom,
            int? days, string note, string reference)
        {
            var componentsInfo = await _nodesService.GetNodesInfo(componentsId);

            ValidateComponentsAllowedType(componentsInfo);

            var properties = nodeInfo.Entry.Properties.As<JObject>().ToDictionary();

            var subject = properties.GetNestedValueOrDefault(SpisumNames.Properties.Subject)?.ToString();

            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Subject, subject, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShRef, properties.GetNestedValueOrDefault(SpisumNames.Properties.Pid)?.ToString(), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShRefId, nodeInfo?.Entry?.Id, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShComponentsRef, string.Join(",", componentsId.ToArray()), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.ShFilesSize, GetshFileSize(componentsInfo), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.DateFrom, dateFrom.ToAlfrescoDateTimeString(), ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Note, note, ParameterType.GetOrPost));
            parameters = parameters.Add(new Parameter(SpisumNames.Properties.Ref, reference, ParameterType.GetOrPost));

            if (days != null) parameters = parameters.Add(new Parameter(SpisumNames.Properties.DateTo, dateFrom.AddDays(days.Value).ToAlfrescoDateTimeString(), ParameterType.GetOrPost));


            return parameters;
        }

        private async Task<NodeAssociationEntry> GetShipmentDocument(string shipmentId)
        {
            return (await _nodesService.GetParentsByAssociation(shipmentId, new List<string> {
                        SpisumNames.Associations.ShipmentsCreated,
                        SpisumNames.Associations.ShipmentsToReturn,
                        SpisumNames.Associations.ShipmentsToDispatch
                        })).Where(x => x?.Entry?.NodeType == SpisumNames.NodeTypes.Document || x?.Entry?.NodeType == SpisumNames.NodeTypes.File).FirstOrDefault();
        }

        private string GetShipmentNodeType(ShipmentCreateMode mode)
        {
            return mode switch
            {
                ShipmentCreateMode.Email      => SpisumNames.NodeTypes.ShipmentEmail,
                ShipmentCreateMode.DataBox    => SpisumNames.NodeTypes.ShipmentDatabox,
                ShipmentCreateMode.Personally => SpisumNames.NodeTypes.ShipmentPersonally,
                ShipmentCreateMode.Post       => SpisumNames.NodeTypes.ShipmentPost,
                ShipmentCreateMode.Publish    => SpisumNames.NodeTypes.ShipmentPublish,
                _                             => null
            };
        }

        private async Task<bool> IsComponentsAssociatedWithNode(string nodeId, List<NodeEntry> components)
        {
            var nodeSecondaryChildrens = await _nodesService.GetSecondaryChildren(nodeId, SpisumNames.Associations.Components);

            foreach (var component in components)
                if (nodeSecondaryChildrens.Any(x => x.Entry.Id == component.Entry.Id) == false)
                    return false;

            return true;
        }

        private bool IsComponentsMaximumSizeReached(List<NodeEntry> components, long limitMB)
        {
            var totalSizeMb = components.Sum(x => x.Entry?.Content?.SizeInBytes)?.ConvertBytesToMegabytes();

            return totalSizeMb < limitMB ? false : true;
        }

        private async Task<bool> IsShipmentToDispatch(string shipmentId)
        {
            var shipmentParents = await _alfrescoHttpClient.GetNodeParents(shipmentId, ImmutableList<Parameter>.Empty
                       .Add(new Parameter(AlfrescoNames.Headers.Where, $"(assocType='{SpisumNames.Associations.ShipmentsToDispatch}')", ParameterType.QueryString)));

            return shipmentParents?.List?.Entries?.Count > 0;
        }

        private async Task ShipmentSendEmailDatabox(string nodeId, NodeEntry shipmentInfo, EmailOrDataboxEnum emailOrDataboxEnum)
        {
            var nodeParentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Permissions},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

            // Phase 1
            await _alfrescoHttpClient.UpdateNode(shipmentInfo?.Entry?.Id, new NodeBodyUpdate()
                .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.ToDispatch)
                .AddProperty(SpisumNames.Properties.ToDispatchDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                );

            try
            {
                var shipmentPid = shipmentInfo?.GetPid();

                // Log for shipment
                await _auditLogService.Record(shipmentInfo?.Entry?.Id, shipmentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                    TransactinoHistoryMessages.ShipmentSendEmailDataBoxToDispatch);

                // Log for document
                await _auditLogService.Record(nodeParentInfo?.Entry?.Id, nodeParentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                    TransactinoHistoryMessages.ShipmentSendEmailDataBoxToDispatch);

                if (nodeParentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                {
                    var fileId = await _documentService.GetDocumentFileId(nodeId);
                    if (fileId != null)
                        // Log for file
                        await _auditLogService.Record(fileId, shipmentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                            TransactinoHistoryMessages.ShipmentSendEmailDataBoxToDispatch);
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            // Phase 2
            var properties = shipmentInfo?.Entry?.Properties.As<JObject>().ToDictionary();

            var sender = properties.GetNestedValueOrDefault(SpisumNames.Properties.Sender)?.ToString();
            var recipient = properties.GetNestedValueOrDefault(SpisumNames.Properties.Recipient)?.ToString();
            var subject = properties.GetNestedValueOrDefault(SpisumNames.Properties.Subject)?.ToString();
            var body = properties.GetNestedValueOrDefault(SpisumNames.Properties.ShEmailBody)?.ToString();

            var shipmentComponents = await _nodesService.GetSecondaryChildren(shipmentInfo?.Entry?.Id, SpisumNames.Associations.ShipmentsComponents, false, true);
            List<FormDataParam> attachments = new List<FormDataParam>();
            await shipmentComponents?.ForEachAsync(async x =>
            {
                var componentProperties = x?.Entry?.Properties.As<JObject>().ToDictionary();
                var originalFileName = componentProperties.GetNestedValueOrDefault(SpisumNames.Properties.FileName)?.ToString();

                var data = await _alfrescoHttpClient.NodeContent(x?.Entry?.Id);

                attachments.Add(new FormDataParam(data.File, originalFileName, originalFileName, data.ContentType));
            });

            if (emailOrDataboxEnum == EmailOrDataboxEnum.Email)
            //EMAIL
                try
                {
                    var response = await _emailHttpClient.Send(recipient, sender, subject, body, attachments);

                    if (!response?.IsSuccesfullySended ?? false)
                    {
                        await EmailDataBoxSendFailedAction(nodeId, shipmentInfo);
                        throw new BadRequestException("", "Failed to send email");
                    }

                    var locallySet = shipmentInfo?.Entry?.Permissions?.LocallySet;
                    var updateBody = new NodeBodyUpdateFixed
                    {
                        Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
                    };

                    await _alfrescoHttpClient.UpdateNode(shipmentInfo?.Entry?.Id, updateBody
                        .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.Dispatched)
                        .AddProperty(SpisumNames.Properties.DispatchedDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                    );
                }
                catch
                {
                    await EmailDataBoxSendFailedAction(nodeId, shipmentInfo);
                    throw new BadRequestException("", "Failed to send email");
                }
            else
            // DATABOX
                try
                {
                    var response = await _dataBoxHttpClient.Send(new DataBox.Api.Models.DataBoxSend(recipient, sender, subject, attachments));
                    if (!response?.IsSuccessfullySent ?? true)
                    {
                        await EmailDataBoxSendFailedAction(nodeId, shipmentInfo);
                        Log.Error(response?.Exception, "DataBox send error");
                        throw new BadRequestException("", "Failed to send databox message");
                    }

                    var locallySet = shipmentInfo?.Entry?.Permissions?.LocallySet;
                    var updateBody = new NodeBodyUpdateFixed
                    {
                        Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
                    };

                    await _alfrescoHttpClient.UpdateNode(shipmentInfo?.Entry?.Id, updateBody
                        .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.Dispatched)
                        .AddProperty(SpisumNames.Properties.DispatchedDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                        .AddProperty(SpisumNames.Properties.ItemId, response.MessageId)
                    );

                }
                catch
                {
                    await EmailDataBoxSendFailedAction(nodeId, shipmentInfo);
                    throw new BadRequestException("", "Failed to send databox message");
                }

            if (nodeParentInfo?.Entry?.IsLocked == true)
                await _alfrescoHttpClient.NodeUnlock(nodeId);

            await _nodesService.DeleteSecondaryChildrenAsAdmin(nodeId, shipmentInfo?.Entry?.Id);
            await _nodesService.CreateSecondaryChildrenAsAdmin(nodeId, new ChildAssociationBody
            {
                AssocType = SpisumNames.Associations.ShipmentsDispatched,
                ChildId = shipmentInfo?.Entry.Id
            });

            await _nodesService.MoveByPath(shipmentInfo?.Entry?.Id, SpisumNames.Paths.DispatchDispatched);

            await _alfrescoHttpClient.NodeLock(shipmentInfo?.Entry?.Id, new NodeBodyLock {Type = NodeBodyLockType.FULL});

            await AddDispatchedPermissions(nodeParentInfo, shipmentInfo?.Entry?.Id);

            if (nodeParentInfo?.Entry?.IsLocked == true)
                await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock() { Type = NodeBodyLockType.FULL });

            try
            {
                var shipmentPid = shipmentInfo?.GetPid();

                // Log for shipment
                await _auditLogService.Record(shipmentInfo?.Entry?.Id, shipmentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                    TransactinoHistoryMessages.ShipmentSendEmailDataBoxDispatched);

                // Log for document
                await _auditLogService.Record(nodeParentInfo?.Entry?.Id, nodeParentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                    TransactinoHistoryMessages.ShipmentSendEmailDataBoxDispatched);

                if (nodeParentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                {
                    var fileId = await _documentService.GetDocumentFileId(nodeId);
                    if (fileId != null)
                        // Log for file
                        await _auditLogService.Record(fileId, shipmentInfo?.Entry?.NodeType, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                            TransactinoHistoryMessages.ShipmentSendEmailDataBoxDispatched);
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        private async Task ShipmentSendPersonally(string nodeId, NodeEntry shipmentInfo)
        {
            var nodeParentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Permissions},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

            if (nodeParentInfo?.Entry?.IsLocked == true)
                await _alfrescoHttpClient.NodeUnlock(nodeId);

            var locallySet = shipmentInfo?.Entry?.Permissions?.LocallySet;
            var updateBody = new NodeBodyUpdateFixed
            {
                Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
            };

            // Phase 1
            await _alfrescoHttpClient.UpdateNode(shipmentInfo?.Entry?.Id, updateBody
                      .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.ToDispatch)
                      .AddProperty(SpisumNames.Properties.ToDispatchDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                      );

            locallySet = shipmentInfo?.Entry?.Permissions?.LocallySet; 
            updateBody = new NodeBodyUpdateFixed
            {
                Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
            };

            await AddDispatchGroupPermissions(nodeParentInfo, shipmentInfo?.Entry?.Id, GroupPermissionTypes.Consumer);

            try
            {
                var shipmentPid = shipmentInfo?.GetPid();

                // Log for shipment
                await _auditLogService.Record(shipmentInfo?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                    TransactinoHistoryMessages.ShipmentSendPersonallyToDispatch);

                // Log for document
                await _auditLogService.Record(nodeParentInfo?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                    TransactinoHistoryMessages.ShipmentSendPersonallyToDispatch);

                if (nodeParentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                {
                    var fileId = await _documentService.GetDocumentFileId(nodeId);
                    if (fileId != null)
                        // Log for file
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                            TransactinoHistoryMessages.ShipmentSendPersonallyToDispatch);
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            // Phase 2
            await _alfrescoHttpClient.UpdateNode(shipmentInfo?.Entry?.Id, updateBody
                      .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.Dispatched)
                      .AddProperty(SpisumNames.Properties.DispatchedDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
                      );

            await _nodesService.DeleteSecondaryChildrenAsAdmin(nodeId, shipmentInfo?.Entry?.Id);
            await _nodesService.CreateSecondaryChildrenAsAdmin(nodeId, new ChildAssociationBody
            {
                AssocType = SpisumNames.Associations.ShipmentsDispatched,
                ChildId = shipmentInfo?.Entry.Id
            });

            await _nodesService.MoveByPath(shipmentInfo?.Entry?.Id, SpisumNames.Paths.DispatchDispatched);

            await AddDispatchGroupPermissions(nodeParentInfo, shipmentInfo?.Entry?.Id, GroupPermissionTypes.Consumer);

            try
            {
                var shipmentPid = shipmentInfo?.GetPid();

                // Log for shipment
                await _auditLogService.Record(shipmentInfo?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                    TransactinoHistoryMessages.ShipmentSendPersonallyDispatched);

                // Log for document
                await _auditLogService.Record(nodeParentInfo?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                    TransactinoHistoryMessages.ShipmentSendPersonallyDispatched);

                if (nodeParentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                {
                    var fileId = await _documentService.GetDocumentFileId(nodeId);
                    if (fileId != null)
                        // Log for file
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Vypraveno,
                            TransactinoHistoryMessages.ShipmentSendPersonallyDispatched);
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            // Phase 3

            await _alfrescoHttpClient.UpdateNode(shipmentInfo?.Entry?.Id, new NodeBodyUpdate()
              .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.Delivered)
              .AddProperty(SpisumNames.Properties.DeliveryDate, DateTime.UtcNow.ToAlfrescoDateTimeString())
            );

            await _alfrescoHttpClient.NodeLock(shipmentInfo?.Entry?.Id, new NodeBodyLock
            {
                Type = NodeBodyLockType.FULL
            });

            await AddDispatchedPermissions(nodeParentInfo, shipmentInfo?.Entry.Id);

            if (nodeParentInfo?.Entry?.IsLocked == true)
                await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock() { Type = NodeBodyLockType.FULL });

            try
            {
                var shipmentPid = shipmentInfo?.GetPid();

                // Log for shipment
                await _auditLogService.Record(shipmentInfo?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Doruceno,
                    TransactinoHistoryMessages.ShipmentSendPersonallyDelivered);

                // Log for document
                await _auditLogService.Record(nodeParentInfo?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Doruceno,
                    TransactinoHistoryMessages.ShipmentSendPersonallyDelivered);

                var fileId = await _documentService.GetDocumentFileId(nodeId);

                if (fileId != null)
                    // Log for file
                    await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPersonally, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.Doruceno,
                        TransactinoHistoryMessages.ShipmentSendPersonallyDelivered);
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        private async Task ShipmentSendPostPublish(string nodeId, NodeEntry shipmentInfo)
        {
            var locallySet = shipmentInfo?.Entry?.Permissions?.LocallySet;
            var updateBody = new NodeBodyUpdateFixed
            {
                Permissions = { LocallySet = locallySet != null && locallySet.Any() ? locallySet.ToList() : new List<PermissionElement>() }
            };

            var nodeParentInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty
                .Add(new Parameter(AlfrescoNames.Headers.Include, $"{AlfrescoNames.Includes.Permissions},{AlfrescoNames.Includes.IsLocked}", ParameterType.QueryString)));

            if (nodeParentInfo?.Entry?.IsLocked == true)
                await _alfrescoHttpClient.NodeUnlock(nodeId);

            await _alfrescoHttpClient.UpdateNode(shipmentInfo?.Entry?.Id, updateBody
                .AddProperty(SpisumNames.Properties.InternalState, SpisumNames.InternalState.ToDispatch)
                .AddProperty(SpisumNames.Properties.ReasonForReturn, null)
                .AddProperty(SpisumNames.Properties.ReturnedDate, null)
                .AddProperty(SpisumNames.Properties.ToDispatchDate, DateTime.UtcNow.ToAlfrescoDateTimeString()));

            await _nodesService.DeleteSecondaryChildrenAsAdmin(nodeId, shipmentInfo?.Entry?.Id);
            await _nodesService.CreateSecondaryChildrenAsAdmin(nodeId, new ChildAssociationBody
            {
                AssocType = SpisumNames.Associations.ShipmentsToDispatch,
                ChildId = shipmentInfo?.Entry?.Id
            });

            await _nodesService.MoveByPath(shipmentInfo?.Entry?.Id, SpisumNames.Paths.DispatchToDispatch);

            await AddToDispatchPermissions(nodeParentInfo, shipmentInfo?.Entry?.Id);

            if (nodeParentInfo?.Entry?.IsLocked == true)
                await _alfrescoHttpClient.NodeLock(nodeId, new NodeBodyLock() { Type = NodeBodyLockType.FULL });

            try
            {
                var shipmentPid = shipmentInfo?.GetPid();

                // Log for shipment
                await _auditLogService.Record(shipmentInfo?.Entry?.Id, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                    TransactinoHistoryMessages.ShipmentSendPostPublish);

                // Log for document
                await _auditLogService.Record(nodeId, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                    TransactinoHistoryMessages.ShipmentSendPostPublish);

                if (nodeParentInfo?.Entry?.NodeType == SpisumNames.NodeTypes.Document)
                {
                    var fileId = await _documentService.GetDocumentFileId(nodeId);
                    if (fileId != null)
                        // Log for file
                        await _auditLogService.Record(fileId, SpisumNames.NodeTypes.ShipmentPublish, shipmentPid, NodeTypeCodes.Zasilka, EventCodes.PredaniVypravne,
                            TransactinoHistoryMessages.ShipmentSendPostPublish);
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }
        }

        private async Task<List<string>> UnlockAllAssociations(string nodeId)
        {
            var lockedComponents = new List<string>();

            await _nodesService.TraverseAllChildren(nodeId, async locNodeId =>
            {
                var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(locNodeId, ImmutableList<Parameter>.Empty
                    .Add(new Parameter(AlfrescoNames.Headers.Include, AlfrescoNames.Includes.IsLocked, ParameterType.QueryString)));

                if (nodeInfo.Entry.IsLocked == true)
                {
                    await _alfrescoHttpClient.NodeUnlock(locNodeId);
                    lockedComponents.Add(nodeInfo.Entry.Id);
                }

                return nodeInfo;
            });

            return lockedComponents;
        }

        private async Task<ShipmentComponentValidation> ValidateComponents(List<string> componentsId, string nodeId, int maximumSize)
        {
            var componentsInfo = await _nodesService.GetNodesInfo(componentsId);

            if (IsComponentsMaximumSizeReached(componentsInfo, maximumSize))
                throw new BadRequestException(ErrorCodes.V_MAX_SIZE);
            if (!await IsComponentsAssociatedWithNode(nodeId, componentsInfo))
                throw new BadRequestException("", "One or more provided components are not associated with provided nodeId");

            ValidateComponentsAllowedType(componentsInfo);

            return new ShipmentComponentValidation
            {
                ComponentsInfo = componentsInfo,
                TotalSizeBytes = GetComponentsSize(componentsInfo)
            };
        }
        private void ValidateComponentsAllowedType(List<NodeEntry> componentsInfo)
        {
            if (!componentsInfo.All(x => new List<string>
            {
                SpisumNames.NodeTypes.Component,
                SpisumNames.NodeTypes.EmailComponent,
                SpisumNames.NodeTypes.DataBoxComponent
            }.Contains(x?.Entry?.NodeType)))
                throw new BadRequestException(string.Empty, $"One or more provided components are not allowed nodeType " +
                    $"{SpisumNames.NodeTypes.Component}, " +
                    $"{SpisumNames.NodeTypes.Email}" +
                    $"{SpisumNames.NodeTypes.DataBox}");
        }
        #endregion
    }
}