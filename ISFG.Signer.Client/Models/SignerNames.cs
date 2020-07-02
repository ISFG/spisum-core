namespace ISFG.Signer.Client.Models
{
    public static class SignerNames
    {
        #region Fields

        public const string Ok = "OK";
        public const string Warning = "WARNING";
        public const string Error = "ERROR";

        public const string Expired = "EXPIRED";
        public const string Revoked = "REVOKED";
        public const string FormatFailure = "FORMAT_FAILURE";
        public const string NoCertificateChainFound = "NO_CERTIFICATE_CHAIN_FOUND";

        public const string Valid = "VALID";
        public const string Indeterminate = "INDETERMINATE";
        public const string Invalid = "INVALID";

        public const string Qualified = "QUALIFIED";
        public const string Commercial = "COMMERCIAL";
        public const string InternalStorage = "INTERNAL_STORAGE";
        public const string Unknown = "UNKNOWN";

        public const string ESign = "ESIGN";
        public const string ESeal = "ESEAL";

        #endregion
    }
}