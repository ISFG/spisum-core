﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentFavouriteAdd
    {
        #region Properties

        [Required]
        [FromBody]
        public List<string> NodeId { get; set; }

        #endregion
    }
}
