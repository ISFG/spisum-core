using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ISFG.SpisUm.ClientSide.Models.EmailDatabox
{
    public class DontRegister
    {
        #region Properties

        [Required]
        [FromRoute]
        public string NodeId { get; set; }

        [FromBody]
        public DontRegisterBody Body { get; set; }

        #endregion
    }
    public class DontRegisterBody
    {
        #region Properties

        [Required]
        public string Reason { get; set; }

        #endregion
    }
}
