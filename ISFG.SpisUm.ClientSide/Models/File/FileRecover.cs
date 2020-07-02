using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileRecover
    {
        #region Properties

        [Required]
        [FromBody]
        public List<string> Ids { get; set; }

        [Required]
        [FromBody]
        public string Reason { get; set; }

        #endregion
    }
}
