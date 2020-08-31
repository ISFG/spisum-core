using System.Collections.Generic;

namespace ISFG.SpisUm.ClientSide.Models.Shipments
{
    internal class ShipmentCreateEmailInternal
    {
        #region Properties

        public string Recipient { get; set; }
        public string Ref { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }
        public List<string> Components { get; set; }
        public int MaximumComponentSizeMB { get => 10; }
        public string TextFilePath { get; set; }

        #endregion
    }

    internal class ShipmentCreateDataBoxInternal
    {
        #region Properties

        public bool AllowSubstDelivery { get; set; }
        public string LegalTitleLaw { get; set; }
        public string LegalTitleYear { get; set; }
        public string LegalTitleSect { get; set; }
        public string LegalTitlePar { get; set; }
        public string LegalTitlePoint { get; set; }
        public bool PersonalDelivery { get; set; }
        public string Recipient { get; set; }
        public string Ref { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }
        public string ToHands { get; set; }
        public List<string> Components { get; set; }
        public int MaximumComponentSizeMB { get => 50; }

        #endregion
    }

    internal class ShipmentCreatePostInternal
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
        public string[] PostType { get; set; }
        public string PostTypeOther { get; set; }
        public string PostItemType { get; set; }
        public string PostItemTypeOther { get; set; }
        public double? PostItemCashOnDelivery { get; set; }
        public double? PostItemStatedPrice { get; set; }
        public string Ref { get; set; }

        #endregion
    }

    internal class ShipmentCreatePersonalInternal
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
        public string Ref { get; set; }

        #endregion
    }

    internal class ShipmentCreatePublishInternal
    {
        #region Properties

        public List<string> Components { get; set; }

        #endregion
    }
}
