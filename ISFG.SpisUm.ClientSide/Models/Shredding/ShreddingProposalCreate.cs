using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ISFG.SpisUm.ClientSide.Models.Shredding
{
    public class ShreddingProposalCreate
    {
        #region Properties

        [Required]
        public List<string> Ids { get; set; }

        [Required]
        public string Name { get; set; }

        #endregion
    }
}