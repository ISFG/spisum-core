using System.Collections.Generic;

namespace ISFG.SpisUm.ClientSide.ServiceModels.Shipments
{
    public class ShipmentCreateDataBoxSModel
    {
        #region Properties

        public bool AllowSubstDelivery { get; set; }
        public List<string> Components { get; set; }
        public string LegalTitleLaw { get; set; }
        public string LegalTitlePar { get; set; }
        public string LegalTitlePoint { get; set; }
        public string LegalTitleSect { get; set; }
        public string LegalTitleYear { get; set; }
        public string NodeId { get; set; }
        public bool PersonalDelivery { get; set; }
        public string Recipient { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }
        public string ToHands { get; set; }

        #endregion
    }
}