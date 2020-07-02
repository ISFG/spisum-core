using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Shipments
{
    public class ShipmentDispatchPost
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public ShipmentDispatchBody Body { get; set; }

        #endregion
    }
    public class ShipmentDispatchBody
    {
        #region Properties

        public string PostItemId { get; set; }

        public string PostItemNumber { get; set; }

        #endregion
    }
}
