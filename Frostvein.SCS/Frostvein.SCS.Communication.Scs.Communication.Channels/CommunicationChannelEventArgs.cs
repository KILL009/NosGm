using System;

namespace Frostvein.SCS.Communication.Scs.Communication.Channels
{
    internal class CommunicationChannelEventArgs : EventArgs
    {
        public ICommunicationChannel Channel { get; private set; }

        public CommunicationChannelEventArgs(ICommunicationChannel channel) => this.Channel = channel;
    }
}
