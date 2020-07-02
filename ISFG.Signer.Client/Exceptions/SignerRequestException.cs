using System;

namespace ISFG.Signer.Client.Exceptions
{
    public class SignerRequestException : Exception
    {
        #region Constructors

        public SignerRequestException(string message, Exception ex) : base(message, ex)
        {
        }

        #endregion
    }
}