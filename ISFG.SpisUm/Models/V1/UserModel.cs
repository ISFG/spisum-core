namespace ISFG.SpisUm.Models.V1
{
    public class UserPasswordModel
    {
        #region Properties

        /// <summary>
        /// Old password
        /// </summary>
        public string OldPassword { get; set; }

        /// <summary>
        /// New password
        /// </summary>
        public string NewPassword { get; set; }

        #endregion
    }
}
