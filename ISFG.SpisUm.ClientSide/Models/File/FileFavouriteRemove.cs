using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileFavouriteRemove
    {
        #region Properties

        [Required]
        [FromBody]
        public List<string> FileNodeIds { get; set; }

        #endregion
    }
}
