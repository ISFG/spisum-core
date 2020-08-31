using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentChangeLocation
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromBody]
        public DocumentChangeLocationBody Body { get; set; }

        #endregion
    }

    public class DocumentChangeLocationBody
    {
        #region Properties

        [Required]
        public string Location { get; set; }

        #endregion
    }
}
