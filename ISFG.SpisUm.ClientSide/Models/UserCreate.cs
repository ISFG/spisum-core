using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ISFG.SpisUm.ClientSide.Models
{
    public class UserProperties
    {
        #region Properties

        /// <summary>
        /// Email
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// First name
        /// </summary>
        [Required]
        public string FirstName { get; set; }

        /// <summary>
        /// Groupse list
        /// </summary>
        [Required]
        public List<string> Groups { get; set; }

        /// <summary>
        /// Last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Main group
        /// </summary>
        [Required]
        public string MainGroup { get; set; }

        /// <summary>
        /// Group for signature
        /// </summary>
        public List<string> SignGroups { get; set; }

        /// <summary>
        /// Internal id of user
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Internal job of user
        /// </summary>
        public string UserJob { get; set; }

        /// <summary>
        /// Organization address
        /// </summary>
        public string UserOrgAddress { get; set; }

        /// <summary>
        /// Organization id
        /// </summary>
        public string UserOrgId { get; set; }

        /// <summary>
        /// Organization name
        /// </summary>
        public string UserOrgName { get; set; }

        /// <summary>
        /// Organization unit of user
        /// </summary>
        public string UserOrgUnit { get; set; }

        #endregion
    }

    public class UserCreate : UserProperties
    {
        #region Properties

        /// <summary>
        /// User id
        /// </summary>
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [Required]
        [MinLength(5)]
        public string Password { get; set; }

        #endregion
    }

    public class UserUpdate : UserProperties
    {
        #region Properties

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }

        #endregion
    }
}