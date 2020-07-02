using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface IShipmentsService
    {
        #region Public Methods

        Task<List<string>> CancelShipment(List<string> shipmentIds);

        Task<NodeEntry> CreateShipmentDataBox(string nodeId, bool allowSubstDelivery, string legalTitleLaw, string legalTitleYear, string legalTitleSect, string legalTitlePar, string legalTitlePoint
            , bool personalDelivery, string recipient, string sender, string subject, string toHands, List<string> components);

        Task<NodeEntry> CreateShipmentEmail(string nodeId, string recipient, string sender, string subject, List<string> components, string textFilePath);

        Task<NodeEntry> CreateShipmentPersonally(string nodeId, string address1, string address2, string address3, string address4, string addressStreet,
            string addressCity, string addressZip, string addressState);

        Task<NodeEntry> CreateShipmentPost(string nodeId, string address1, string address2, string address3, string address4, string addressStreet,
            string addressCity, string addressZip, string addressState, string[] postType, string postTypeOther, string postItemType, string postItemTypeOther,
            double? postItemCashOnDelivery,
            double? postItemStatedPrice);

        Task<NodeEntry> CreateShipmentPublish(string nodeId, List<string> components, DateTime dateFrom, int? days, string note);
        Task<List<NodeChildAssociationEntry>> GetShipments(string documentId);
        Task<NodeEntry> ShipmentDispatchPost(string nodeId, string postItemId, string postItemNumber);
        Task<List<string>> ShipmentsDispatchPublish(List<string> shipmentsIds);
        Task<List<string>> ShipmentsResend(List<string> shipmentsIds);
        Task<List<string>> ShipmentsReturn(string reason, List<string> shipmentsIds);
        Task<List<string>> ShipmentsSend(string documentId, List<string> shipmentsId);

        Task<NodeEntry> UpdateShipmentDataBox(string shipmentId, List<string> componentsId, bool allowSubstDelivery, string legalTitleLaw, string legalTitleYear, string legalTitleSect,
            string legalTitlePar, string legalTitlePoint, bool personalDelivery, string recipient, string sender, string subject, string toHands);

        Task<NodeEntry> UpdateShipmentEmail(string nodeId, string recipient, string sender, string subject, List<string> componentsId, string textFilePath);

        Task<NodeEntry> UpdateShipmentPersonally(string shipmentId, string address1, string address2, string address3, string address4, string addressStreet,
           string addressCity, string addressZip, string addressState);

        Task<NodeEntry> UpdateShipmentPost(string shipmentId, string address1, string address2, string address3, string address4, string addressStreet,
            string addressCity, string addressZip, string addressState, string[] postType, string postTypeOther, string postItemType, string postItemTypeOther, double? postItemWeight,
            double? postItemPrice, string postItemNumber, string postItemId, double? postItemCashOnDelivery,
            double? postItemStatedPrice);

        Task<NodeEntry> UpdateShipmentPublish(string shipmentId, List<string> componentsId, DateTime dateFrom, int? days, string note);

        #endregion
    }
}
