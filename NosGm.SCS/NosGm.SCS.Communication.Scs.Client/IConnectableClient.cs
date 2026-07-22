using NosGm.SCS.Communication.Scs.Communication;
using System;

namespace NosGm.SCS.Communication.Scs.Client
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
