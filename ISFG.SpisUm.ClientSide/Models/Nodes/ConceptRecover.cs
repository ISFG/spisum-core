using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class ConceptRecover
    {
        #region Properties

        [Required]
        [FromBody]
        public List<string> Ids { get; set; }

        [Required]
        [FromBody]
        public string Reason { get; set; }

        #endregion
    }
}