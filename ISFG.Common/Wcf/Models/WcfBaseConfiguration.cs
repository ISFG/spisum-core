using System;
using System.ServiceModel;

namespace ISFG.Common.Wcf.Models
{
    public class WcfBaseConfiguration
    {
        #region Properties

        public string Uri { get; set; }

        public BasicHttpSecurityMode SecurityMode { get; set; } = BasicHttpSecurityMode.None;

        public HttpClientCredentialType? ClientCredentialType { get; set; }

        public int MaxBufferSize { get; set; } = int.MaxValue;

        public int MaxBufferPoolSize { get; set; } = int.MaxValue;

        public int MaxReceivedMessageSize { get; set; } = int.MaxValue;

        public bool AllowCookies { get; set; } = true;

        public TimeSpan OpenTimeout { get; set; } = new TimeSpan(0, 0, 1, 0);

        public TimeSpan CloseTimeout { get; set; } = new TimeSpan(0, 0, 1, 0);

        public TimeSpan SendTimeout { get; set; } = new TimeSpan(0, 0, 1, 0);

        public TimeSpan ReceiveTimeout { get; set; } = new TimeSpan(0, 0, 10, 0);

        public bool BypassProxyOnLocal { get; set; } = false;

        public bool UseDefaultWebProxy { get; set; } = true;

        public UserNamePasswordCredentials UserNamePasswordCredentials { get; set; }

        #endregion
    }
}