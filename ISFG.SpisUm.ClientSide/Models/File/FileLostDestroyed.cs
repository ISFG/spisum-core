using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.File
{
    public class FileLostDestroyed
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public FileLostDestroyedBody Body { get; set; }

        #endregion

        #region Nested Types, Enums, Delegates

        public class FileLostDestroyedBody
        {
            #region Properties

            public string Reason { get; set; }

            #endregion
        }

        #endregion
    }
}