using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ISFG.SpisUm.ClientSide.Models.Shipments
{
    public class ShipmentReturn
    {
        #region Properties

        [Required]
        public string Reason { get; set; }

        [Required]
        public List<string> Ids { get; set; }

        #endregion
    }
}
