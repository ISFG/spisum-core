using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileFound
    {
        #region Properties

        [Required]
        [FromBody]
        public string[] NodeId { get; set; }

        #endregion
    }
}