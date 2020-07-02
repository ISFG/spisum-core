using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Serilog;
using Serilog.Events;

namespace ISFG.Common.Wcf.Logger
{
    public class ClientMessageInspector : IClientMessageInspector
    {
        #region Implementation of IClientMessageInspector

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            if (!Log.IsEnabled(LogEventLevel.Debug))
                return;
            
            Log.Debug($"Response: {reply}");
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            if (!Log.IsEnabled(LogEventLevel.Debug))
                return null;
            
            Log.Debug($"Url: {channel.RemoteAddress.Uri}, Request: {request}");
            return null;
        }

        #endregion
    }
}