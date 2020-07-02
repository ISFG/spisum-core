using System.Threading.Tasks;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface ISignerService
    {
        #region Public Methods

        Task<SignerCreateResponse> CreateXml(string baseUrl, string documentId, string[] componentId, bool visual);
        Task<byte[]> DownloadFile(string token, string componentId);
        Task<string> GenerateBatch(string baseUrl, string token, string documentId, string[] componentId, bool visual);
        Task<bool> CheckAndUpdateComponent(string componentId, byte[] component);
        Task UploadFile(string token, string documentId, string componentId, byte[] newComponent);

        #endregion
    }
}