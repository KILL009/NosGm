using System;

namespace NosGm.SCS.Communication.Scs.Communication.Channels
{
    internal interface IConnectionListener
    {
        event EventHandler<CommunicationChannelEventArgs> CommunicationChannelConnected;

        void Start();

        void Stop();
    }
}
