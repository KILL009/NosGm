using Frostvein.SCS.Communication.Scs.Communication;
using System;

namespace Frostvein.SCS.Communication.Scs.Client
{
    public interface IConnectableClient : IDisposable
    {
        event EventHandler Connected;

        event EventHandler Disconnected;

        int ConnectTimeout { get; set; }

        CommunicationStates CommunicationState { get; }

        void Connect();

        void Disconnect();
    }
}
