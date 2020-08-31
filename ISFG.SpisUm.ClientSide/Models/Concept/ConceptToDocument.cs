using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Concept
{
    public class ConceptToDocument
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public ConceptToDocumentBody Body { get; set; }

        #endregion
    }
    public class ConceptToDocumentBody
    {
        #region Properties

        [Required]
        public string Author { get; set; }

        [Required]
        public int? AttachmentsCount { get; set; }

        public DateTime? SettleTo { get; set; }

        [Required]
        public string Subject { get; set; }

        #endregion
    }
}
