using System.Collections.Generic;

namespace ISFG.SpisUm.ClientSide.ServiceModels.Shipments
{
    public class ShipmentCreateEmailSModel
    {
        #region Properties

        public List<string> Components { get; set; }
        public string NodeId { get; set; }
        public string Recipient { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }

        #endregion
    }
}