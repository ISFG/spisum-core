using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentBorrow
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromBody]
        public DocumentBorrowBody Body { get; set; }

        #endregion
    }

    public class DocumentBorrowBody
    {
        #region Properties

        [Required]
        public string Group { get; set; }

        [Required]
        public string User { get; set; }

        #endregion
    }
}
