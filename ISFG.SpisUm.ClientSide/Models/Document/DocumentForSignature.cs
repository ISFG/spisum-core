using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Document
{
    public class DocumentForSignature
    {
        #region Properties

        [Required]
        [FromRoute] 
        public string NodeId { get; set; }

        [FromBody]
        public DocumentForSignatureBody Body { get; set; }

        #endregion

        #region Nested Types, Enums, Delegates

        public class DocumentForSignatureBody
        {
            #region Properties

            public string Group { get; set; }

            public string User { get; set; }

            #endregion
        }

        #endregion
    }
}