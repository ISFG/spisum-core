using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Shipments
{
    public class ShipmentUpdateDataBox
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromBody]
        public ShipmentUpdateDataBoxBody Body { get; set; }

        #endregion
    }
    public class ShipmentUpdateDataBoxBody
    {
        #region Properties

        public bool AllowSubstDelivery { get; set; }
        public string LegalTitleLaw { get; set; }
        public string LegalTitleYear { get; set; }
        public string LegalTitleSect { get; set; }
        public string LegalTitlePar { get; set; }
        public string LegalTitlePoint { get; set; }
        public bool PersonalDelivery { get; set; }

        [Required]
        public string Recipient { get; set; }

        [Required]
        public string Sender { get; set; }

        [Required]
        public string Subject { get; set; }

        public string ToHands { get; set; }
        public List<string> Components { get; set; }

        #endregion
    }
}
