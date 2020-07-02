using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class DocumentCancel
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public DocumentCancelBody Body { get; set; }

        #endregion

        #region Nested Types, Enums, Delegates

        public class DocumentCancelBody
        {
            #region Properties

            public string Reason { get; set; }

            #endregion
        }

        #endregion
    }
}
