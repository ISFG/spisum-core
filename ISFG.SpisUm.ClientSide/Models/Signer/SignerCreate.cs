﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Signer
{
    public class SignerCreate
    {
        #region Properties

        [Required]
        [FromQuery]
        public string DocumentId { get; set; }

        [Required]
        [FromQuery]
        public string[] ComponentId { get; set; }

        [FromQuery]
        public bool Visual { get; set; }
        
        #endregion
    }
}