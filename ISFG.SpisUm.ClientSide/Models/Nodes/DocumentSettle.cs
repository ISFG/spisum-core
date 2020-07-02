using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.Nodes
{
    public class DocumentSettle
    {
        #region Properties

        [Required]
        [FromRoute] 
        public string NodeId { get; set; }

        [FromBody]
        public DocumentSettleBody Body { get; set; }

        #endregion

        #region Nested Types, Enums, Delegates

        public class DocumentSettleBody
        {
            #region Properties

            [Required]
            public string SettleMethod { get; set; }

            [Required]
            public DateTime? SettleDate { get; set; }

            public string CustomSettleMethod { get; set; }

            public string SettleReason { get; set; }

            #endregion
        }

        #endregion
    }
}