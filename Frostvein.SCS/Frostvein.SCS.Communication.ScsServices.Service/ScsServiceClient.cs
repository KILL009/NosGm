using Frostvein.SCS.Communication.Scs.Communication;
using Frostvein.SCS.Communication.Scs.Communication.EndPoints;
using Frostvein.SCS.Communication.Scs.Communication.Messengers;
using Frostvein.SCS.Communication.Scs.Server;
using Frostvein.SCS.Communication.ScsServices.Communication;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Proxies;

namespace Frostvein.SCS.Communication.ScsServices.Service
{
    internal class ScsServiceClient : IScsServiceClient
    {
        private readonly IScsServerClient _serverClient;
        private readonly RequestReplyMessenger<IScsServerClient> _requestReplyMessenger;
        private RealProxy _realProxy;

        [CompilerGenerated]
        public event EventHandler Disconnected;

        public long ClientId => this._serverClient.ClientId;

        public ScsEndPoint RemoteEndPoint => this._serverClient.RemoteEndPoint;

        public CommunicationStates CommunicationState => this._serverClient.CommunicationState;

        public ScsServiceClient(
          IScsServerClient serverClient,
          RequestReplyMessenger<IScsServerClient> requestReplyMessenger)
        {
            this._serverClient = serverClient;
            this._serverClient.Disconnected += new EventHandler(this.Client_Disconnected);
            this._requestReplyMessenger = requestReplyMessenger;
        }

        public void Disconnect() => this._serverClient.Disconnect();

        public T GetClientProxy<T>() where T : class
        {
            this._realProxy = (RealProxy)new RemoteInvokeProxy<T, IScsServerClient>(this._requestReplyMessenger);
            return (T)this._realProxy.GetTransparentProxy();
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            this._requestReplyMessenger.Stop();
            this.OnDisconnected();
        }

        private void OnDisconnected()
        {
            EventHandler disconnected = this.Disconnected;
            if (disconnected == null)
                return;
            disconnected((object)this, EventArgs.Empty);
        }
    }
}
