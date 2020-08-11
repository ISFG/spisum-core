using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.SpisUm.Attributes;
using ISFG.SpisUm.ClientSide.Interfaces;
using ISFG.SpisUm.ClientSide.Models.Shipments;
using ISFG.SpisUm.ClientSide.Models.TransactionHistory;
using ISFG.SpisUm.Endpoints;
using ISFG.SpisUm.Interfaces;
using ISFG.SpisUm.Models.V1;
using ISFG.Translations.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.Controllers.App.V1
{
    [AlfrescoAuthentication]
    [ApiVersion("1")]
    [ApiController]
    [Route(EndpointsUrl.ApiRoute + "/shipment")]
    public class ShipmentController : ControllerBase
    {
        #region Fields

        private readonly IAuditLogService _auditLogService;
        private readonly IShipmentsService _shipmentsService;
        private readonly ISpisUmConfiguration _spisUmConfiguration;
        private readonly ITranslateService _translateService;

        #endregion

        #region Constructors

        public ShipmentController(
            IAuditLogService auditLogService, 
            IShipmentsService shipmentsService, 
            ISpisUmConfiguration spisUmConfiguration, 
            ITranslateService translateService
        )
        {
            _auditLogService = auditLogService;
            _shipmentsService = shipmentsService;
            _spisUmConfiguration = spisUmConfiguration;
            _translateService = translateService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Cancels all the shipments
        /// </summary>
        /// <returns></returns>
        [HttpPost("cancel")]
        public async Task<List<string>> Cancel([FromRoute] ShipmentCancel shipmentCancel)
        {
            return await _shipmentsService.CancelShipment(shipmentCancel.NodeIds);
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
        /// Dispatch a shipment type of post
        /// </summary>
        [HttpPost("{nodeId}/post/dispatch")]
        public async Task<NodeEntry> ShipmentPostDispatch([FromRoute] ShipmentDispatchPost input)
        {
            return await _shipmentsService.ShipmentDispatchPost(input.NodeId, input.Body.PostItemId, input.Body.PostItemNumber);
        }

        /// <summary>
        /// Dispatch a shipment type of publish
        /// </summary>
        [HttpPost("publish/dispatch")]
        public async Task<List<string>> ShipmentPublishDispatch([FromRoute] ShipmentDispatchPublish input)
        {
            return await _shipmentsService.ShipmentsDispatchPublish(input.Ids);
        }

        /// <summary>
        /// Resend a shipment
        /// </summary>
        [HttpPost("resend")]
        public async Task<List<string>> ShipmentsResend([FromRoute] ShipmentResend input)
        {
            return await _shipmentsService.ShipmentsResend(input.Ids);
        }

        /// <summary>
        /// Return a shipment
        /// </summary>
        [HttpPost("return")]
        public async Task<List<string>> ShipmentsResend([FromBody] ShipmentReturn input)
        {
            return await _shipmentsService.ShipmentsReturn(input.Reason, input.Ids);
        }

        /// <summary>
        /// Update shipment type of databox
        /// </summary>
        [HttpPost("{nodeId}/update/databox")]
        public async Task<NodeEntry> ShipmentUpdateDatabox([FromRoute] ShipmentUpdateDataBox update)
        {
            return await _shipmentsService.UpdateShipmentDataBox(
                update.NodeId,
                update.Body.Components,
                update.Body.AllowSubstDelivery,
                update.Body.LegalTitleLaw,
                update.Body.LegalTitleYear,
                update.Body.LegalTitleSect,
                update.Body.LegalTitlePar,
                update.Body.LegalTitlePoint,
                update.Body.PersonalDelivery,
                update.Body.Recipient,
                update.Body.Sender,
                update.Body.Subject,
                update.Body.ToHands);
        }

        /// <summary>
        /// Update shipment type of email
        /// </summary>
        [HttpPost("{nodeId}/update/email")]
        public async Task<NodeEntry> ShipmentUpdateEmail([FromRoute] ShipmentUpdateEmail update)
        {
            return await _shipmentsService.UpdateShipmentEmail(
                update.NodeId,
                update.Body.Recipient,
                update.Body.Sender,
                update.Body.Subject,
                update.Body.Components,
                Path.Combine(_spisUmConfiguration.Shipments.ConfigurationFilesFolder, _spisUmConfiguration.Shipments.FolderName, _spisUmConfiguration.Shipments.ShipmentCreateTextFile));
        }

        /// <summary>
        /// Update shipment type of post
        /// </summary>
        [HttpPost("{nodeId}/update/post")]
        public async Task<NodeEntry> ShipmentUpdatePost([FromRoute] ShipmentUpdatePost update)
        {
            return await _shipmentsService.UpdateShipmentPost(
                update.NodeId,
                update.Body.Address1,
                update.Body.Address2,
                update.Body.Address3,
                update.Body.Address4,
                update.Body.AddressStreet,
                update.Body.AddressCity,
                update.Body.AddressZip,
                update.Body.AddressState,
                update.Body.PostType,
                update.Body.PostTypeOther,
                update.Body.PostItemType,
                update.Body.PostItemTypeOther,
                update.Body.PostItemWeight,
                update.Body.PostItemPrice,
                update.Body.PostItemNumber,
                update.Body.PostItemId,
                update.Body.PostItemCashOnDelivery,
                update.Body.PostItemStatedPrice);
        }

        /// <summary>
        /// Update shipment type of publish
        /// </summary>
        [HttpPost("{nodeId}/update/publish")]
        public async Task<NodeEntry> ShipmentUpdatePublish([FromRoute] ShipmentUpdatePublish update)
        {
            return await _shipmentsService.UpdateShipmentPublish(
                update.NodeId,
                update.Body.Components,
                update.Body.DateFrom.Value,
                update.Body.Days,
                update.Body.Note);
        }

        /// <summary>
        /// Update shipment type of personally
        /// </summary>
        [HttpPost("{nodeId}/update/personally")]
        public async Task<NodeEntry> ShipmentUpdatePublish([FromRoute] ShipmentUpdatePersonally update)
        {
            return await _shipmentsService.UpdateShipmentPersonally(
                update.NodeId,
                update.Body.Address1,
                update.Body.Address2,
                update.Body.Address3,
                update.Body.Address4,
                update.Body.AddressStreet,
                update.Body.AddressCity,
                update.Body.AddressZip,
                update.Body.AddressState);
        }

        #endregion
    }
}
