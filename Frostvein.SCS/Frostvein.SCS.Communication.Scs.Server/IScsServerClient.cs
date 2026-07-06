using Frostvein.SCS.Communication.Scs.Communication;
using Frostvein.SCS.Communication.Scs.Communication.EndPoints;
using Frostvein.SCS.Communication.Scs.Communication.Messengers;
using System;

namespace Frostvein.SCS.Communication.Scs.Server
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
