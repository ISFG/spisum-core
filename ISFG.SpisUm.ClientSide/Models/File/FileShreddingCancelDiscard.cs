using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileShreddingCancelDiscard
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        #endregion
    }
}