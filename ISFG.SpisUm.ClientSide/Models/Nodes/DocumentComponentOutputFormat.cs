using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class DocumentComponentOutputFormat
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [Required]
        [FromRoute]
        public string ComponentId { get; set; }

        [FromBody]
        public DocumentComponentOutputFormatBody Body { get; set; }

        #endregion

        #region Nested Types, Enums, Delegates

        public class DocumentComponentOutputFormatBody
        {
            #region Properties

            public string Reason { get; set; }

            #endregion
        }

        #endregion
    }
}