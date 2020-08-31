using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileChangeFileMark
    {
        #region Properties

        [Required]
        [FromBody]
        public FileChangeFileMarkBody Body { get; set; }

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        #endregion
    }

    public class FileChangeFileMarkBody
    {
        #region Properties

        [Required]
        public string FileMark { get; set; }

        #endregion
    }
}
