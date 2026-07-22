using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.EndPoints;
using NosGm.SCS.Communication.Scs.Communication.Messengers;
using System;

namespace NosGm.SCS.Communication.Scs.Server
{
    public interface IScsServerClient : IMessenger
    {
        event EventHandler Disconnected;

        long ClientId { get; }

        ScsEndPoint RemoteEndPoint { get; }

        CommunicationStates CommunicationState { get; }

        void Disconnect();
    }
}
