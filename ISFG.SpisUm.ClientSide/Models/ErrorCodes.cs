// ReSharper disable InconsistentNaming

namespace ISFG.SpisUm.ClientSide.Models
{
    /// <summary>S
    /// Used to return code for FrontEnd
    /// </summary>
    public static class ErrorCodes
    {
        #region Properties

        public static string V_EMAIL_NO_SENDER => nameof(V_EMAIL_NO_SENDER);
        public static string V_EMAIL_INVALID_SENDER => nameof(V_EMAIL_INVALID_SENDER);
        public static string V_EMAIL_NO_RECIPIENT => nameof(V_EMAIL_NO_RECIPIENT);
        public static string V_EMAIL_INVALID_RECIPIENT => nameof(V_EMAIL_INVALID_RECIPIENT);
        public static string V_EMAIL_NO_CONFIGURATION => nameof(V_EMAIL_NO_CONFIGURATION);
        public static string V_MIN_TEXT => nameof(V_MIN_TEXT);
        public static string D_MAX_CONTENT => nameof(D_MAX_CONTENT);
        public static string D_STUCK => nameof(D_STUCK);
        public static string V_FORBIDDEN_PATH => nameof(V_FORBIDDEN_PATH);
        public static string V_COMPONENT_TYPE => nameof(V_COMPONENT_TYPE);
        public static string F_DOCUMENT_ASSOCIATION => nameof(F_DOCUMENT_ASSOCIATION);
        public static string V_FILE_CANCEL_CHILDREN => nameof(V_FILE_CANCEL_CHILDREN);
        public static string V_MAX_SIZE => nameof(V_MAX_SIZE);

        #endregion
    }
}
