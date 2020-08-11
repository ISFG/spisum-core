using System.Threading.Tasks;
using ISFG.SpisUm.ClientSide.Models.Signer;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface ISignerService
    {
        #region Public Methods

        Task<SignerCreateResponse> CreateXml(string baseUrl, string documentId, string[] componentId, bool visual);
        Task<string> GenerateBatch(string baseUrl, string documentId, string[] componentId, bool visual);
        Task<bool> CheckAndUpdateComponent(string componentId, byte[] component);
        Task UploadFile(string documentId, string componentId, byte[] newComponent, bool visual);

        #endregion
    }
}