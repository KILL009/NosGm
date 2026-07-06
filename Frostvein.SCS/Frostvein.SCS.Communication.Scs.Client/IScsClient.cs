using Frostvein.SCS.Communication.Scs.Communication.Messengers;
using System;

namespace Frostvein.SCS.Communication.Scs.Client
{
    public interface IScsClient : IMessenger, IConnectableClient, IDisposable
    {
    }
}
