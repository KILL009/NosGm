using Frostvein.SCS.Collections;
using Frostvein.SCS.Communication.Scs.Communication.Protocols;
using System;

namespace Frostvein.SCS.Communication.Scs.Server
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
