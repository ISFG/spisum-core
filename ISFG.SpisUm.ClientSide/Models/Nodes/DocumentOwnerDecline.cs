using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class DocumentOwnerDecline
    {
        #region Properties

        [Required]
        [FromRoute] 
        public string NodeId { get; set; }

        [FromBody]
        public DocumentOwnerDeclineBody Body { get; set; }

        #endregion

        #region Nested Types, Enums, Delegates

        public class DocumentOwnerDeclineBody
        {}

        #endregion
    }
}