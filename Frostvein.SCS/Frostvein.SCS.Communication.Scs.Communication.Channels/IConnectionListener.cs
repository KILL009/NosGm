using System;

namespace Frostvein.SCS.Communication.Scs.Communication.Channels
{
    internal interface IConnectionListener
    {
        event EventHandler<CommunicationChannelEventArgs> CommunicationChannelConnected;

        void Start();

        void Stop();
    }
}
