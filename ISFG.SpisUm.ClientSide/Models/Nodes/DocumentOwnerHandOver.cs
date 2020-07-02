using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class DocumentOwnerHandOver
    {
        #region Properties

        [Required]
        [FromRoute] 
        public string NodeId { get; set; }

        [FromBody]
        public DocumentOwnerHandOverBody Body { get; set; }

        #endregion

        #region Nested Types, Enums, Delegates

        public class DocumentOwnerHandOverBody
        {
            #region Properties

            public string NextGroup { get; set; }

            public string NextOwner { get; set; }

            #endregion
        }

        #endregion
    }
}