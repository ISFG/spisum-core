using ISFG.Common.Wcf.Models;

namespace ISFG.Signer.Client.Interfaces
{
    public interface ISignerConfiguration
    {
        #region Properties

        WcfBaseConfiguration Base { get; set; }
        string Url { get; set; }

        #endregion
    }
}