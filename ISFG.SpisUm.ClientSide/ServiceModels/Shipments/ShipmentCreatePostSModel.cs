using System.Collections.Generic;

namespace ISFG.SpisUm.ClientSide.ServiceModels.Shipments
{
    public class ShipmentCreatePostSModel
    {
        #region Properties

        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Address4 { get; set; }
        public string AddressCity { get; set; }
        public string AddressState { get; set; }
        public string AddressStreet { get; set; }
        public string AddressZip { get; set; }
        public string NodeId { get; set; }
        public List<string> PostType { get; set; }
        public string PostTypeOther { get; set; }
        public string PostItemType { get; set; }
        public string PostItemTypeOther { get; set; }
        public double? PostItemCashOnDelivery { get; set; }
        public double? PostItemStatedPrice { get; set; }

        #endregion
    }
}