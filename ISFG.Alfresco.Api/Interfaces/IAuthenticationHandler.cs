using System.Threading.Tasks;
using RestSharp;

namespace ISFG.Alfresco.Api.Interfaces
{
    public interface IAuthenticationHandler
    {
        #region Public Methods

        void AuthenticateRequest(IRestRequest request);
        Task<bool> HandleNotAuthenticated();

        #endregion
    }
}