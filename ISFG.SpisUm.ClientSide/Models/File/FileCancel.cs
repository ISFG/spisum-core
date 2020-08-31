using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileCancel
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public FileCancelBody Body { get; set; }

        #endregion

        #region Nested Types, Enums, Delegates

        public class FileCancelBody
        {
            #region Properties

            public string Reason { get; set; }

            #endregion
        }

        #endregion
    }

}
