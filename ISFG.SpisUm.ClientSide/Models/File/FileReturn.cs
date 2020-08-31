using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileReturn
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        #endregion
    }
}