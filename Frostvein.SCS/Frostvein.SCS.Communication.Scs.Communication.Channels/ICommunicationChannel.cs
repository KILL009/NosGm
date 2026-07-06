using Frostvein.SCS.Communication.Scs.Communication.EndPoints;
using Frostvein.SCS.Communication.Scs.Communication.Messengers;
using System;

namespace Frostvein.SCS.Communication.Scs.Communication.Channels
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
