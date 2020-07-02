using System;
using System.ServiceModel;
using ISFG.Common.Wcf.Interfaces;
using ISFG.Common.Wcf.Logger;

namespace ISFG.Common.Wcf
{
    public abstract class WcfBaseClient<T> where T : class
    {
        #region Constructors

        public WcfBaseClient(IChannelConfig<T> channelConfig)
        {
            var channelFactory = new ChannelFactory<T>(channelConfig.GetBinding(), channelConfig.GetEndpointAddress());

            channelConfig.SetCredentials(channelFactory.Credentials);

            channelFactory.Faulted += (sender, args) =>
            {
                channelFactory.Abort();
                try
                {
                    Channel = channelFactory.CreateChannel();
                }
                catch (Exception e)
                {
                    Channel = null;
                }

            };
            
            channelFactory.Endpoint.EndpointBehaviors.Add(new EndpointBehavior());

            try
            {
                Channel = channelFactory.CreateChannel();
            }
            catch (Exception e)
            {
                Channel = null;
            }
        }

        #endregion

        #region Properties

        protected T Channel { get; private set; }

        #endregion
    }
}