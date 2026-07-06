using Frostvein.SCS.Communication.Scs.Client;
using Frostvein.SCS.Communication.Scs.Communication;
using Frostvein.SCS.Communication.Scs.Communication.Messages;
using Frostvein.SCS.Communication.Scs.Communication.Messengers;
using Frostvein.SCS.Communication.ScsServices.Communication;
using Frostvein.SCS.Communication.ScsServices.Communication.Messages;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Frostvein.SCS.Communication.ScsServices.Client
{
    internal class ScsServiceClient<T> : IScsServiceClient<T>, IConnectableClient, IDisposable where T : class
    {
        private readonly IScsClient _client;
        private readonly RequestReplyMessenger<IScsClient> _requestReplyMessenger;
        private readonly AutoConnectRemoteInvokeProxy<T, IScsClient> _realServiceProxy;
        private readonly object _clientObject;

        [CompilerGenerated]
        public event EventHandler Connected;

        [CompilerGenerated]
        public event EventHandler Disconnected;

        public int ConnectTimeout
        {
            get => this._client.ConnectTimeout;
            set => this._client.ConnectTimeout = value;
        }

        public CommunicationStates CommunicationState => this._client.CommunicationState;

        public T ServiceProxy { get; private set; }

        public int Timeout
        {
            get => this._requestReplyMessenger.Timeout;
            set => this._requestReplyMessenger.Timeout = value;
        }

        public ScsServiceClient(IScsClient client, object clientObject)
        {
            this._client = client;
            this._clientObject = clientObject;
            this._client.Connected += new EventHandler(this.Client_Connected);
            this._client.Disconnected += new EventHandler(this.Client_Disconnected);
            this._requestReplyMessenger = new RequestReplyMessenger<IScsClient>(client);
            this._requestReplyMessenger.MessageReceived += new EventHandler<MessageEventArgs>(this.RequestReplyMessenger_MessageReceived);
            this._realServiceProxy = new AutoConnectRemoteInvokeProxy<T, IScsClient>(this._requestReplyMessenger, (IConnectableClient)this);
            this.ServiceProxy = (T)this._realServiceProxy.GetTransparentProxy();
        }

        public void Connect() => this._client.Connect();

        public void Disconnect() => this._client.Disconnect();

        public void Dispose() => this.Disconnect();

        private void RequestReplyMessenger_MessageReceived(object sender, MessageEventArgs e)
        {
            if (!(e.Message is ScsRemoteInvokeMessage message))
                return;
            if (this._clientObject == null)
            {
                this.SendInvokeResponse((IScsMessage)message, (object)null, new ScsRemoteException("Client does not wait for method invocations by server."));
            }
            else
            {
                object returnValue;
                try
                {
                    returnValue = this._clientObject.GetType().GetMethod(message.MethodName).Invoke(this._clientObject, message.Parameters);
                }
                catch (TargetInvocationException ex)
                {
                    Exception innerException = ex.InnerException;
                    this.SendInvokeResponse((IScsMessage)message, (object)null, new ScsRemoteException(innerException.Message, innerException));
                    return;
                }
                catch (Exception ex)
                {
                    this.SendInvokeResponse((IScsMessage)message, (object)null, new ScsRemoteException(ex.Message, ex));
                    return;
                }
                this.SendInvokeResponse((IScsMessage)message, returnValue, (ScsRemoteException)null);
            }
        }

        private void SendInvokeResponse(
          IScsMessage requestMessage,
          object returnValue,
          ScsRemoteException exception)
        {
            try
            {
                RequestReplyMessenger<IScsClient> requestReplyMessenger = this._requestReplyMessenger;
                ScsRemoteInvokeReturnMessage invokeReturnMessage = new ScsRemoteInvokeReturnMessage();
                invokeReturnMessage.RepliedMessageId = requestMessage.MessageId;
                invokeReturnMessage.ReturnValue = returnValue;
                invokeReturnMessage.RemoteException = exception;
                ScsRemoteInvokeReturnMessage message = invokeReturnMessage;
                requestReplyMessenger.SendMessage((IScsMessage)message);
            }
            catch
            {
            }
        }

        private void Client_Connected(object sender, EventArgs e)
        {
            this._requestReplyMessenger.Start();
            this.OnConnected();
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            this._requestReplyMessenger.Stop();
            this.OnDisconnected();
        }

        private void OnConnected()
        {
            EventHandler connected = this.Connected;
            if (connected == null)
                return;
            connected((object)this, EventArgs.Empty);
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
