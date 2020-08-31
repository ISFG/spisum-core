using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileShreddingDiscard
    {
        #region Properties

        [Required]
        [FromBody]
        public FileShreddingDiscardBody Body { get; set; }

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        #endregion
    }

    public class FileShreddingDiscardBody
    {
        #region Properties

        [Required]
        public DateTime? Date { get; set; }

        [Required]
        public string Reason { get; set; }

        #endregion
    }
}