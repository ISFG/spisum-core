using System.Threading.Tasks;
using ISFG.Signer.Client.Generated.Signer;

namespace ISFG.Signer.Client.Interfaces
{
    public interface ISignerClient
    {
        #region Public Methods

        Task<SealResponse> Seal(byte[] fileData);
        Task<ValidateResponse> Validate(byte[] fileData);
        Task<ValidateCertificateResponse> ValidateCertificate(byte[] certificate);

        #endregion
    }
}