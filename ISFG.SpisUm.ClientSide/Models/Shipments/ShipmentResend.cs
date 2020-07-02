using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Shipments
{
    public class ShipmentResend
    {
        #region Properties

        [Required]
        [FromBody]
        public List<string> Ids { get; set; }

        #endregion
    }
}
