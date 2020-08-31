using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Shipments
{
    public class ShipmentUpdatePost
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public ShipmentUpdatePostBody Body { get; set; }

        #endregion
    }
    public class ShipmentUpdatePostBody
    {
        #region Properties

        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Address4 { get; set; }
        public string AddressStreet { get; set; }
        public string AddressCity { get; set; }
        public string AddressZip { get; set; }
        public string AddressState { get; set; }
        [Required]
        public string[] PostType { get; set; }
        public string PostTypeOther { get; set; }
        public string PostItemType { get; set; }
        public string PostItemTypeOther { get; set; }
        public double? PostItemWeight { get; set; }
        public double? PostItemPrice { get; set; }
        public string PostItemNumber { get; set; }
        public string PostItemId { get; set; }
        public double? PostItemCashOnDelivery { get; set; }
        public double? PostItemStatedPrice { get; set; }

        #endregion
    }
}
