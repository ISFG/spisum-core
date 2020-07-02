using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentShreddingDiscard
    {
        #region Properties

        [Required]
        [FromBody]
        public DocumentShreddingDiscardBody Body { get; set; }

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        #endregion
    }

    public class DocumentShreddingDiscardBody
    {
        #region Properties

        [Required]
        public DateTime? Date { get; set; }

        [Required]
        public string Reason { get; set; }

        #endregion
    }
}