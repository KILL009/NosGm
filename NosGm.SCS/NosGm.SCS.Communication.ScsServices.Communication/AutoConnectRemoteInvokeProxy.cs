using NosGm.SCS.Communication.Scs.Client;
using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.Messengers;
using System.Runtime.Remoting.Messaging;

namespace NosGm.SCS.Communication.ScsServices.Communication
{
    internal class AutoConnectRemoteInvokeProxy<TProxy, TMessenger> :
      RemoteInvokeProxy<TProxy, TMessenger>
      where TMessenger : IMessenger
    {
        private readonly IConnectableClient _client;

        public AutoConnectRemoteInvokeProxy(
          RequestReplyMessenger<TMessenger> clientMessenger,
          IConnectableClient client)
          : base(clientMessenger)
        {
            this._client = client;
        }

        public override IMessage Invoke(IMessage msg)
        {
            if (this._client.CommunicationState == CommunicationStates.Connected)
                return base.Invoke(msg);
            this._client.Connect();
            try
            {
                return base.Invoke(msg);
            }
            finally
            {
                this._client.Disconnect();
            }
        }
    }
}
