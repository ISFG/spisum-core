using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentRevert
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromRoute]
        public string VersionId { get; set; }

        #endregion
    }
}
