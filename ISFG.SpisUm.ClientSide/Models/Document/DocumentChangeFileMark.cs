using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentChangeFileMark
    {
        #region Properties

        [Required]
        [FromBody]
        public DocumentChangeFileMarkBody Body { get; set; }

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        #endregion
    }

    public class DocumentChangeFileMarkBody
    {
        #region Properties

        [Required]
        public string FileMark { get; set; }

        #endregion
    }
}