using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Shipments
{
    public class ShipmentCreateEmail
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public ShipmentCreateEmailBody Body { get; set; }

        #endregion
    }
    public class ShipmentCreateEmailBody
    {
        #region Properties

        [Required]
        [FromBody]
        public string Recipient { get; set; }

        [Required]
        [FromBody]
        public string Sender { get; set; }

        [Required]
        [FromBody]
        public string Subject { get; set; }

        [Required]
        [FromBody]
        public List<string> Components { get; set; }

        #endregion
    }
}
