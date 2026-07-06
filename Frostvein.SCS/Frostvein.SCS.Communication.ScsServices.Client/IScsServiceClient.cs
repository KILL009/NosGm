using Frostvein.SCS.Communication.Scs.Client;
using System;

namespace Frostvein.SCS.Communication.ScsServices.Client
{
    public interface IScsServiceClient<out T> : IConnectableClient, IDisposable where T : class
    {
        T ServiceProxy { get; }

        int Timeout { get; set; }
    }
}
