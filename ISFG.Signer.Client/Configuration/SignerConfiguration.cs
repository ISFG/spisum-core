using ISFG.Common.Attributes;
using ISFG.Common.Wcf.Models;
using ISFG.Signer.Client.Interfaces;

namespace ISFG.Signer.Client.Configuration
{
    [Settings("Signer")]
    public class SignerConfiguration : ISignerConfiguration
    {
        #region Implementation of ISignerConfiguration

        public WcfBaseConfiguration Base { get; set; }
        public string Url { get; set; }

        #endregion
    }
}