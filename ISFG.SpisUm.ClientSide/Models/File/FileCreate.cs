using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileCreate
    {
        #region Properties

        [Required]
        [FromBody]
        public string DocumentId { get; set; }

        #endregion
    }
}
