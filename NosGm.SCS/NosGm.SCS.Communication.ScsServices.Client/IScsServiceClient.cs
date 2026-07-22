using NosGm.SCS.Communication.Scs.Client;
using System;

namespace NosGm.SCS.Communication.ScsServices.Client
{
    public interface IScsServiceClient<out T> : IConnectableClient, IDisposable where T : class
    {
        T ServiceProxy { get; }

        int Timeout { get; set; }
    }
}
