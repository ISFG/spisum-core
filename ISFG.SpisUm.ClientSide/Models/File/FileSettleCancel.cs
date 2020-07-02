using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileOpen
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public FileSettleCancelBody Body { get; set; }

        #endregion
    }
    public class FileSettleCancelBody
    {
        #region Properties

        [Required]        
        public string Reason { get; set; }

        #endregion
    }
}
