using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Signer
{
    public class SignerGetStatus
    {
        #region Properties

        [Required]
        [FromQuery]
        public string[] ComponentId { get; set; }

        #endregion
    }
}