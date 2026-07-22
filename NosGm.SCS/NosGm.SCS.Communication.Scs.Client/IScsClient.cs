using NosGm.SCS.Communication.Scs.Communication.Messengers;
using System;

namespace NosGm.SCS.Communication.Scs.Client
{
    public interface IScsClient : IMessenger, IConnectableClient, IDisposable
    {
    }
}
