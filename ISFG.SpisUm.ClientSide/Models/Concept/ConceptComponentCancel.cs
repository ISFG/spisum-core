using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Concept
{
    public class ConceptComponentCancel
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromBody]
        public List<string> ComponentsId { get; set; }

        #endregion
    }
}
