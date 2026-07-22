using System;
using System.Runtime.CompilerServices;

namespace NosGm.SCS.Communication.Scs.Communication.Channels
{
    internal abstract class ConnectionListenerBase : IConnectionListener
    {
        [CompilerGenerated]
        public event EventHandler<CommunicationChannelEventArgs> CommunicationChannelConnected;

        public abstract void Start();

        public abstract void Stop();

        protected virtual void OnCommunicationChannelConnected(ICommunicationChannel client)
        {
            EventHandler<CommunicationChannelEventArgs> channelConnected = this.CommunicationChannelConnected;
            if (channelConnected == null)
                return;
            channelConnected((object)this, new CommunicationChannelEventArgs(client));
        }
    }
}
