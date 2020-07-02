using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Shipments
{
    public class ShipmentCreatePublish
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromBody]
        public ShipmentCreatePublishBody Body { get; set; }

        #endregion
    }
    public class ShipmentCreatePublishBody
    {
        #region Properties

        [Required]
        public DateTime? DateFrom { get; set; }

        public int? Days { get; set; }
        public string Note { get; set; }
        public List<string> Components { get; set; }

        #endregion
    }
}
