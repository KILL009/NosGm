using NosGm.SCS.Communication.Scs.Communication.EndPoints;
using NosGm.SCS.Communication.Scs.Communication.Messengers;
using System;

namespace NosGm.SCS.Communication.Scs.Communication.Channels
{
    internal interface ICommunicationChannel : IMessenger
    {
        event EventHandler Disconnected;

        ScsEndPoint RemoteEndPoint { get; }

        CommunicationStates CommunicationState { get; }

        void Start();

        void Disconnect();
    }
}
