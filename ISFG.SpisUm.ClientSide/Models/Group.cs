using System.ComponentModel.DataAnnotations;

namespace ISFG.SpisUm.ClientSide.Models
{
    public class CreateGroup
    {
        #region Properties

        /// <summary>
        /// Group id
        /// </summary>
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Group name
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Group type
        /// </summary>
        public string Type { get; set; }

        #endregion
    }

    public class UpdateGroup
    {
        #region Properties

        /// <summary>
        /// Group name
        /// </summary>
        [Required]
        public string Name { get; set; }
        
        
        
        #endregion
    }
}
