using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileBorrow
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromBody]
        public FileBorrowBody Body { get; set; }

        #endregion
    }

    public class FileBorrowBody
    {
        #region Properties

        [Required]
        public string Group { get; set; }

        [Required]
        public string User { get; set; }

        #endregion
    }
}
