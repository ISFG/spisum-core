using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace ISFG.Common.Wcf.Interfaces
{
    public interface IChannelConfig<T>
    {
        #region Public Methods

        Binding GetBinding();
        EndpointAddress GetEndpointAddress();
        void SetCredentials(ClientCredentials credentials);

        #endregion
    }
}