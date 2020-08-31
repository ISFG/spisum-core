using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentFromSignature
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        #endregion
    }
}