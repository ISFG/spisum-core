using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentReturnForRework
    {
        #region Properties

        [Required]
        [FromBody]
        public DocumentReturnForReworkBody Body { get; set; }

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        #endregion
    }

    public class DocumentReturnForReworkBody
    {
        #region Properties

        public string Reason { get; set; }

        #endregion
    }
}