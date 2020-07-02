using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Extensions;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Models.CoreApiFixed;
using ISFG.Common.Interfaces;
using ISFG.Data.Models;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Extensions;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models;
using ISFG.SpisUm.ClientSide.Models.File;
using ISFG.SpisUm.ClientSide.Models.Nodes;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Models.V1;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using Serilog;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/file")]
    public class FileController : ControllerBase
    {
        #region Fields

        private readonly IAlfrescoHttpClient _alfrescoHttpClient;
        private readonly IAuditLogService _auditLogService;
        private readonly IFileService _fileService;
        private readonly IIdentityUser _identityUser;
        private readonly INodesService _nodesService;
        private readonly IShipmentsService _shipmentsService;
        private readonly ITransactionHistoryService _transactionHistoryService;

        #endregion

        #region Constructors

        public FileController(
            IAlfrescoHttpClient alfrescoHttpClient,
            IAuditLogService auditLogService,
            IFileService fileService,
            IIdentityUser identityUser,
            INodesService nodesService, 
            IShipmentsService shipmentsService,
            ITransactionHistoryService transactionHistoryService
        )
        {
            _alfrescoHttpClient = alfrescoHttpClient;
            _auditLogService = auditLogService;
            _fileService = fileService;
            _identityUser = identityUser;
            _nodesService = nodesService;
            _shipmentsService = shipmentsService;
            _transactionHistoryService = transactionHistoryService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Associates documnents to the provided file (provided NodeId)
        /// </summary>
        /// <param name="fileDocumentAdd">Contains node Ids that will be associated to the file</param>
        /// <returns></returns>
        [HttpPost("{nodeId}/document/add")]
        public async Task<List<string>> AddDocumentToFile([FromRoute] FileDocumentAdd fileDocumentAdd)
        {
            return await _fileService.AddDocumentsToFile(fileDocumentAdd.NodeId, fileDocumentAdd.DocumentIds);
        }

        /// <summary>
        /// Borrow a file
        /// </summary>
        [HttpPost("{nodeId}/borrow")]
        public async Task<NodeEntry> Borrow([FromRoute] FileBorrow input)
        {
            return await _fileService.Borrow(input.NodeId, input.Body.Group, input.Body.User);
        }

        /// <summary>
        /// Create new file
        /// </summary>
        /// <param name="fileCancel">Body</param>
        /// <returns></returns>
        [HttpPost("{nodeId}/cancel")]
        public async Task Cancel([FromRoute] FileCancel fileCancel)
        {
            await _nodesService.CancelNode(fileCancel.NodeId, fileCancel.Body.Reason);
        }

        /// <summary>
        /// Close a File
        /// </summary>
        [HttpPost("{nodeId}/close")]
        public async Task<NodeEntry> Close([FromRoute] FileClose fileClose)
        {
            return await _fileService.Close(fileClose.NodeId, fileClose.Body.SettleMethod, fileClose.Body.SettleDate, fileClose.Body.CustomSettleMethod, fileClose.Body.SettleReason);
        }

        /// <summary>
        /// Create new file
        /// </summary>
        /// <param name="fileCreate">Body</param>
        /// <returns></returns>
        [HttpPost("create")]
        public async Task<NodeEntry> Create(FileCreate fileCreate)
        {
            return await _fileService.Create(fileCreate.DocumentId);
        }

        /// <summary>
        /// Create a new file comment
        /// </summary>
        [HttpPost("{nodeId}/comment/create")]
        public async Task<CommentEntryFixed> CreateComment([FromRoute] string nodeId, [FromBody] CommentBody body)
        {
            return await _alfrescoHttpClient.CreateComment(nodeId, body);
        }

        /// <summary>
        /// Marks file as favourite
        /// </summary>
        /// <param name="favouriteAdd"></param>
        /// <returns></returns>
        [HttpPost("favorite/add")]
        public async Task<List<string>> FavoriteAdd([FromQuery] FileFavouriteAdd favouriteAdd)
        {
            return await _fileService.FavoriteAdd(favouriteAdd.FileNodeIds);
        }

        /// <summary>
        /// Unmarks file as favourite
        /// </summary>
        /// <param name="favouriteRemove"></param>
        /// <returns></returns>
        [HttpPost("favorite/remove")]
        public async Task<List<string>> FavoriteRemove([FromQuery] FileFavouriteRemove favouriteRemove)
        {
            return await _fileService.FavoriteRemove(favouriteRemove.FileNodeIds);
        }

        /// <summary>
        /// Found a file that has been lost
        /// </summary>
        [HttpPost("found")]
        public async Task<List<string>> Found([FromRoute] FileFound fileFound)
        {
            return await _nodesService.FoundNode(fileFound.NodeId, SpisumNames.NodeTypes.File);
        }

        /// <summary>
        /// Get File comments
        /// </summary>
        [HttpGet("{nodeId}/comments")]
        public async Task<CommentPagingFixed> GetComments([FromRoute] string nodeId, [FromQuery] SimpleQueryParams queryParams)
        {
            return await _alfrescoHttpClient.GetComments(nodeId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));
        }

        /// <summary>
        /// Get a File NodeEntry
        /// </summary>
        [HttpGet("{nodeId}")]
        public async Task<NodeEntry> GetFile([FromRoute] string nodeId, [FromQuery] BasicNodeQueryParamsWithRelativePath queryParams)
        {
            var nodeInfo = await _alfrescoHttpClient.GetNodeInfo(nodeId, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));

            try
            {
                var componentPid = nodeInfo?.GetPid();

                await _auditLogService.Record(nodeInfo?.Entry?.Id, SpisumNames.NodeTypes.File, componentPid, NodeTypeCodes.Spis, EventCodes.Zobrazeni,
                    TransactinoHistoryMessages.File);

            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "Audit log failed");
            }

            return nodeInfo;
        }

        /// <summary>
        /// Get history for node and all associated nodes
        /// </summary>
        /// <returns>List of history records</returns>  
        [HttpGet("{nodeId}/history")]
        public async Task<HistoryResponseModel> GetHistory([FromRoute] string nodeId, [FromQuery] SimpleQueryParams queryParams)
        {
            if (queryParams.SkipCount < 0) queryParams.SkipCount = 0;
            if (queryParams.MaxItems < 0 || queryParams.MaxItems > 100) queryParams.MaxItems = 100;

            var history = await _auditLogService.GetEvents(new TransactionHistoryQuery
            {
                NodeId = nodeId,
                CurrentPage = queryParams.SkipCount + 1,
                PageSize = queryParams.MaxItems
            });

            return new HistoryResponseModel
            {
                List = new HistoryListModel
                {
                    Pagination = new Pagination
                    {
                        Count = history.PageCount,
                        HasMoreItems = history.CurrentPage < history.PageCount,
                        TotalItems = history.RecordCount,
                        SkipCount = queryParams.SkipCount,
                        MaxItems = queryParams.MaxItems
                    },
                    Entries = history.Results.ToList().Select(x => new TransactionHistoryEntry { Entry = x })
                }
            };
        }

        /// <summary>
        /// Changes location of the document
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/change-file-mark")]
        public async Task<NodeEntry> ChangeFileMark([FromRoute] FileChangeFileMark input)
        {
            return await _fileService.ChangeFileMark(input.NodeId, input.Body.FileMark);
        }

        /// <summary>
        /// Changes location of the document
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/change-location")]
        public async Task<NodeEntry> ChangeLocation([FromRoute] FileChangeLocation input)
        {
            return await _fileService.ChangeLocation(input.NodeId, input.Body.Location);
        }

        /// <summary>
        /// Change File retention mark to A
        /// </summary>
        [HttpPost("{nodeId}/change-to-a")]
        public async Task ChangeToA([FromRoute] FileShreddingA fileShreddingA)
        {
            await _fileService.ShreddingChange(fileShreddingA.NodeId, "A");
        }

        /// <summary>
        /// Change File retention mark to S
        /// </summary>
        [HttpPost("{nodeId}/change-to-s")]
        public async Task ChangeToS([FromRoute] FileShreddingS fileShreddingS)
        {
            await _fileService.ShreddingChange(fileShreddingS.NodeId, "S");
        }

        /// <summary>
        /// Lost or Destroy a file action
        /// </summary>
        [HttpPost("{nodeId}/lost-destroyed")]
        public async Task LostDestroyed([FromRoute] FileLostDestroyed nodeBody)
        {
            await _nodesService.LostDestroyedNode(nodeBody.NodeId, nodeBody.Body.Reason);
        }

        /// <summary>
        /// Open a file
        /// </summary>
        [HttpPost("{nodeId}/open")]
        public async Task<NodeEntry> Open([FromRoute] FileOpen fileOpen)
        {
            return await _nodesService.OpenFile(fileOpen.NodeId, fileOpen.Body.Reason);
        }

        /// <summary>
        /// Accept handover
        /// </summary>
        [HttpPost("{nodeId}/owner/accept")]
        public async Task OwnerAccept([FromRoute] DocumentOwnerAccept ownerAccept)
        {
            await _nodesService.AcceptOwner(ownerAccept.NodeId);
        }

        /// <summary>
        /// Cancel handover
        /// </summary>
        [HttpPost("{nodeId}/owner/cancel")]
        public async Task OwnerCancel([FromRoute] DocumentOwnerCancel ownerCancel)
        {
            await _nodesService.DeclineOwner(ownerCancel.NodeId, true);
        }

        /// <summary>
        /// Decline a handover
        /// </summary>
        [HttpPost("{nodeId}/owner/decline")]
        public async Task OwnerDecline([FromRoute] DocumentOwnerDecline ownerDecline)
        {
            await _nodesService.DeclineOwner(ownerDecline.NodeId, false);
        }

        /// <summary>
        /// Handover a file
        /// </summary>
        [HttpPost("{nodeId}/owner/handover")]
        public async Task OwnerHandover([FromRoute] DocumentOwnerHandOver nodeBody)
        {
            if (await _nodesService.IsNodeLocked(nodeBody.NodeId))
                await _nodesService.UnlockAll(nodeBody.NodeId);

            await _nodesService.UpdateHandOverPermissionsAll(nodeBody.NodeId, nodeBody.Body.NextGroup, nodeBody.Body.NextOwner);

            await _nodesService.MoveHandOverPath(nodeBody.NodeId, _identityUser.RequestGroup, nodeBody.Body.NextGroup, nodeBody.Body.NextOwner);
            await _nodesService.LockAll(nodeBody.NodeId);

            await _transactionHistoryService.LogHandover(nodeBody.NodeId, nodeBody.Body.NextGroup, nodeBody.Body.NextOwner);
        }

        /// <summary>
        /// Recover provided file
        /// </summary>
        /// <param name="favouriteRemove"></param>
        /// <returns>List of Ids that cannot or failed to be proceed.</returns>
        [HttpPost("recover")]
        public async Task<List<string>> Recover(FileRecover favouriteRemove)
        {
            return await _fileService.Recover(favouriteRemove.Ids, favouriteRemove.Reason);
        }

        /// <summary>
        /// Removes association of documnets from provided file
        /// </summary>
        /// <param name="fileDocumentRemove">Contains node Ids that their association will be removed</param>
        /// <returns></returns>
        [HttpPost("{nodeId}/document/remove")]
        public async Task<List<string>> RemoveDocument([FromRoute] FileDocumentRemove fileDocumentRemove)
        {
            return await _fileService.RemoveDocumentsFromFile(fileDocumentRemove.NodeId, fileDocumentRemove.DocumentIds);
        }

        /// <summary>
        /// Return a borrowed file
        /// </summary>
        [HttpPost("{nodeId}/return")]
        public async Task<NodeEntry> Return([FromRoute] FileReturn input)
        {
            return await _fileService.Return(input.NodeId);
        }

        /// <summary>
        /// Create shipment - post
        /// </summary>
        /// <param name="shipmentCreatePost"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/shipment/create/post")]
        public async Task<NodeEntry> ShipmentFileCreate([FromRoute] ShipmentFileCreatePost shipmentCreatePost)
        {
            return await _shipmentsService.CreateShipmentPost(
                shipmentCreatePost.NodeId,
                shipmentCreatePost.Body.Address1,
                shipmentCreatePost.Body.Address2,
                shipmentCreatePost.Body.Address3,
                shipmentCreatePost.Body.Address4,
                shipmentCreatePost.Body.AddressStreet,
                shipmentCreatePost.Body.AddressCity,
                shipmentCreatePost.Body.AddressZip,
                shipmentCreatePost.Body.AddressState,
                shipmentCreatePost.Body.PostType,
                shipmentCreatePost.Body.PostTypeOther,
                shipmentCreatePost.Body.PostItemType,
                shipmentCreatePost.Body.PostItemTypeOther,
                shipmentCreatePost.Body.PostItemCashOnDelivery,
                shipmentCreatePost.Body.PostItemStatedPrice
                );
        }

        /// <summary>
        /// Create shipment - personally
        /// </summary>
        /// <param name="shipmentCreatePersonally"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/shipment/create/personally")]
        public async Task<NodeEntry> ShipmentFileCreatePublish([FromRoute] ShipmentFileCreatePersonally shipmentCreatePersonally)
        {
            return await _shipmentsService.CreateShipmentPersonally(
                shipmentCreatePersonally.NodeId,
                shipmentCreatePersonally.Body.Address1,
                shipmentCreatePersonally.Body.Address2,
                shipmentCreatePersonally.Body.Address3,
                shipmentCreatePersonally.Body.Address4,
                shipmentCreatePersonally.Body.AddressStreet,
                shipmentCreatePersonally.Body.AddressCity,
                shipmentCreatePersonally.Body.AddressZip,
                shipmentCreatePersonally.Body.AddressState
                );
        }

        /// <summary>
        /// Send all provided shipments
        /// </summary>
        [HttpPost("{nodeId}/shipment/send")]
        public async Task ShipmentSend([FromRoute] FileShipmentSend fileShipmentSend)
        {
            await _shipmentsService.ShipmentsSend(fileShipmentSend.NodeId, fileShipmentSend.ShipmentsId);
        }

        /// <summary>
        /// Cancel file shredding discard action
        /// </summary>
        [HttpPost("{nodeId}/shredding/cancel-discard")]
        public async Task<NodeEntry> ShreddingCancelDiscard([FromRoute] FileShreddingCancelDiscard input)
        {
            return await _fileService.ShreddingCancelDiscard(input.NodeId);
        }

        /// <summary>
        /// file shredding discard action
        /// </summary>
        [HttpPost("{nodeId}/shredding/discard")]
        public async Task<NodeEntry> ShreddingDiscard([FromRoute] FileShreddingDiscard input)
        {
            return await _fileService.ShreddingDiscard(input.NodeId, input.Body.Date.Value, input.Body.Reason);
        }

        /// <summary>
        /// Handover a file to repository
        /// </summary>
        [HttpPost("to-repository")]
        public async Task<List<string>> ToRepository(FileToRepository fileToRepository)
        {
            return await _fileService.ToRepository(fileToRepository.Group, fileToRepository.Ids);
        }

        /// <summary>
        /// Update file
        /// </summary>
        /// <param name="nodeBody"></param>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        [HttpPost("{nodeId}/update")]
        public async Task<NodeEntry> UpdateFile([FromRoute] NodeUpdate nodeBody, [FromQuery] IncludeFieldsQueryParams queryParams)
        {
            return await _fileService.Update(nodeBody, ImmutableList<Parameter>.Empty.AddQueryParams(queryParams));            
        }

        #endregion
    }
}
