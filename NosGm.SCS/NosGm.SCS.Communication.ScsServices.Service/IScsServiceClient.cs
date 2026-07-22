using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.EndPoints;
using System;

namespace NosGm.SCS.Communication.ScsServices.Service
{
    public interface IScsServiceClient
    {
        event EventHandler Disconnected;

        long ClientId { get; }

        ScsEndPoint RemoteEndPoint { get; }

        CommunicationStates CommunicationState { get; }

        void Disconnect();

        T GetClientProxy<T>() where T : class;
    }
}
