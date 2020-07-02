using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class DocumentFound
    {
        #region Properties

        [Required]
        [FromBody]
        public string[] NodeId { get; set; }

        #endregion
    }
}