using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Signer.Client.Generated.Signer;
using ISFG.SpisUm.ClientSide.Models.Signer;
using RestSharp;

namespace ISFG.SpisUm.ClientSide.Interfaces
{
    public interface ISignerService
    {
        #region Public Methods

        Task<SignerCreateResponse> CreateXml(string baseUrl, string documentId, string[] componentId, bool visual);
        Task<string> GenerateBatch(string baseUrl, string documentId, string[] componentId, bool visual);
        Task<bool> CheckAndUpdateComponent(string componentId, byte[] component);
        Task<NodeBodyUpdate> GetBodyCheckComponent(byte[] component, bool isSigned = false);        
        Task<ImmutableList<Parameter>> CheckComponentParameters(byte[] component, bool isSigned = false);
        Task<Dictionary<string, object>> CheckComponentProperties(byte[] component, bool isSigned = false);
        Task UploadFile(string documentId, string componentId, byte[] newComponent, bool visual);

        #endregion
    }
}