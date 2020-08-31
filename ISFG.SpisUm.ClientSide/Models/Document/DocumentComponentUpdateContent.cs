using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentComponentUpdateContent
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromRoute]
        public string ComponentId { get; set; }

        [Required]
        public IFormFile FileData { get; set; }

        #endregion
    }
}
