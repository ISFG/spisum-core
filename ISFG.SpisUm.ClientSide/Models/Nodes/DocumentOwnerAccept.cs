using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class DocumentOwnerAccept
    {
        #region Properties

        [Required]
        [FromRoute] 
        public string NodeId { get; set; }

        #endregion
    }
}