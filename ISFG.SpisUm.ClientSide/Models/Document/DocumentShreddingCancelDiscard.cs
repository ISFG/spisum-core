﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentShreddingCancelDiscard
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        #endregion
    }
}