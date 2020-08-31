using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentSettleCancel
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public DocumentSettleCancelBody Body { get; set; }

        #endregion
    }
    public class DocumentSettleCancelBody
    {
        #region Properties

        [Required]        
        public string Reason { get; set; }

        #endregion
    }
}
