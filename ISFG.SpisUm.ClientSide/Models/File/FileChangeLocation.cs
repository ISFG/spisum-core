using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileChangeLocation
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromBody]
        public FileChangeLocationBody Body { get; set; }

        #endregion
    }
    public class FileChangeLocationBody
    {
        #region Properties

        [Required]
        public string Location { get; set; }

        #endregion
    }
}
