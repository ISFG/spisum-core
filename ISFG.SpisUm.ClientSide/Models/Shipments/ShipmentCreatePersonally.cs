using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Shipments
{
    public class ShipmentCreatePersonally
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public ShipmentCreatePersonallyBody Body { get; set; }

        #endregion
    }
    public class ShipmentCreatePersonallyBody
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

        #endregion
    }
}
