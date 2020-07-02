using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using ISFG.Common.Wcf.Interfaces;
using ISFG.Common.Wcf.Models;

namespace ISFG.Common.Wcf.Configurations
{
    public class CredentialChannelConfig<T> : IChannelConfig<T>
    {
        #region Fields

        private readonly WcfBaseConfiguration _configuration;

        #endregion

        #region Constructors

        public CredentialChannelConfig(WcfBaseConfiguration configuration) => _configuration = configuration;

        #endregion

        #region Implementation of IChannelConfig<T>

        public Binding GetBinding()  => new BasicHttpBinding
        {
            Security =
            {
                Mode = _configuration.SecurityMode,
                Transport = _configuration.ClientCredentialType.HasValue
                    ? new HttpTransportSecurity
                    {
                        ClientCredentialType = _configuration.ClientCredentialType.Value
                    }
                    : null
            },
            MaxBufferSize = _configuration.MaxBufferSize,
            ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
            MaxReceivedMessageSize = _configuration.MaxReceivedMessageSize,
            AllowCookies = _configuration.AllowCookies,
            OpenTimeout = _configuration.OpenTimeout,
            CloseTimeout = _configuration.CloseTimeout,
            SendTimeout = _configuration.SendTimeout,
            ReceiveTimeout = _configuration.ReceiveTimeout,
            BypassProxyOnLocal = _configuration.BypassProxyOnLocal,
            UseDefaultWebProxy = _configuration.UseDefaultWebProxy
        };

        public EndpointAddress GetEndpointAddress() => new EndpointAddress(_configuration.Uri);

        public void SetCredentials(ClientCredentials credentials)
        {
            if (_configuration.UserNamePasswordCredentials == null) 
                return;
            
            credentials.UserName.UserName = _configuration.UserNamePasswordCredentials.UserName;
            credentials.UserName.Password = _configuration.UserNamePasswordCredentials.Password;
        }

        #endregion
    }
}