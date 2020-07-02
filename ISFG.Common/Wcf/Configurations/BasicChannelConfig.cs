using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using ISFG.Common.Wcf.Interfaces;

namespace ISFG.Common.Wcf.Configurations
{
    public class BasicChannelConfig<T> : IChannelConfig<T>
    {
        #region Fields

        private readonly string _url;

        #endregion

        #region Constructors

        public BasicChannelConfig(string url) => _url = url;

        #endregion

        #region Implementation of IChannelConfig<T>

        public Binding GetBinding() => new BasicHttpBinding
        {
            Security = {Mode = BasicHttpSecurityMode.None},
            MaxBufferSize = int.MaxValue,
            ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
            MaxReceivedMessageSize = int.MaxValue,
            AllowCookies = true
        };

        public EndpointAddress GetEndpointAddress()
        {
            try
            {
                return new EndpointAddress(_url);
            }
            catch
            {
                return null;
            }
        }

        public void SetCredentials(ClientCredentials credentials)
        {
        }

        #endregion
    }
}