using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Email
{
    public class EmailSend
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromForm]
        public string Subject { get; set; }

        [Required]
        [FromForm]
        public string Body { get; set; }

        [FromForm]
        public List<IFormFile> Files { get; set; }

        #endregion
    }
}
