using NosGm.SCS.Collections;
using NosGm.SCS.Communication.Scs.Communication.Protocols;
using System;

namespace NosGm.SCS.Communication.Scs.Server
{
    public interface IScsServer
    {
        event EventHandler<ServerClientEventArgs> ClientConnected;

        event EventHandler<ServerClientEventArgs> ClientDisconnected;

        IScsWireProtocolFactory WireProtocolFactory { get; set; }

        ThreadSafeSortedList<long, IScsServerClient> Clients { get; }

        void Start();

        void Stop();
    }
}
