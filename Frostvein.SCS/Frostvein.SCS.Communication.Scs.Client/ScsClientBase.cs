using Frostvein.SCS.Communication.Scs.Communication;
using Frostvein.SCS.Communication.Scs.Communication.Channels;
using Frostvein.SCS.Communication.Scs.Communication.Messages;
using Frostvein.SCS.Communication.Scs.Communication.Messengers;
using Frostvein.SCS.Communication.Scs.Communication.Protocols;
using Frostvein.SCS.Threading;
using System;
using System.Runtime.CompilerServices;

namespace Frostvein.SCS.Communication.Scs.Client
{
    internal abstract class ScsClientBase : IScsClient, IMessenger, IConnectableClient, IDisposable
    {
        private IScsWireProtocol _wireProtocol;
        private const int DefaultConnectionAttemptTimeout = 15000;
        private ICommunicationChannel _communicationChannel;
        private readonly Timer _pingTimer;

        [CompilerGenerated]
        public event EventHandler<MessageEventArgs> MessageReceived;

        [CompilerGenerated]
        public event EventHandler<MessageEventArgs> MessageSent;

        [CompilerGenerated]
        public event EventHandler Connected;

        [CompilerGenerated]
        public event EventHandler Disconnected;

        public int ConnectTimeout { get; set; }

        public IScsWireProtocol WireProtocol
        {
            get => this._wireProtocol;
            set
            {
                if (this.CommunicationState == CommunicationStates.Connected)
                    throw new ApplicationException("Wire protocol can not be changed while connected to server.");
                this._wireProtocol = value;
            }
        }

        public CommunicationStates CommunicationState
        {
            get
            {
                return this._communicationChannel == null ? CommunicationStates.Disconnected : this._communicationChannel.CommunicationState;
            }
        }

        public DateTime LastReceivedMessageTime
        {
            get
            {
                return this._communicationChannel == null ? DateTime.MinValue : this._communicationChannel.LastReceivedMessageTime;
            }
        }

        public DateTime LastSentMessageTime
        {
            get
            {
                return this._communicationChannel == null ? DateTime.MinValue : this._communicationChannel.LastSentMessageTime;
            }
        }

        protected ScsClientBase()
        {
            this._pingTimer = new Timer(30000);
            this._pingTimer.Elapsed += new EventHandler(this.PingTimer_Elapsed);
            this.ConnectTimeout = 15000;
            this.WireProtocol = WireProtocolManager.GetDefaultWireProtocol();
        }

        public void Connect()
        {
            this.WireProtocol.Reset();
            this._communicationChannel = this.CreateCommunicationChannel();
            this._communicationChannel.WireProtocol = this.WireProtocol;
            this._communicationChannel.Disconnected += new EventHandler(this.CommunicationChannel_Disconnected);
            this._communicationChannel.MessageReceived += new EventHandler<MessageEventArgs>(this.CommunicationChannel_MessageReceived);
            this._communicationChannel.MessageSent += new EventHandler<MessageEventArgs>(this.CommunicationChannel_MessageSent);
            this._communicationChannel.Start();
            this._pingTimer.Start();
            this.OnConnected();
        }

        public void Disconnect()
        {
            if (this.CommunicationState != CommunicationStates.Connected)
                return;
            this._communicationChannel.Disconnect();
        }

        public void Dispose() => this.Disconnect();

        public void SendMessage(IScsMessage message)
        {
            if (this.CommunicationState != CommunicationStates.Connected)
                throw new CommunicationStateException("Client is not connected to the server.");
            this._communicationChannel.SendMessage(message);
        }

        protected abstract ICommunicationChannel CreateCommunicationChannel();

        private void CommunicationChannel_MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message is ScsPingMessage)
                return;
            this.OnMessageReceived(e.Message);
        }

        private void CommunicationChannel_MessageSent(object sender, MessageEventArgs e)
        {
            this.OnMessageSent(e.Message);
        }

        private void CommunicationChannel_Disconnected(object sender, EventArgs e)
        {
            this._pingTimer.Stop();
            this.OnDisconnected();
        }

        private void PingTimer_Elapsed(object sender, EventArgs e)
        {
            if (this.CommunicationState != CommunicationStates.Connected)
                return;
            try
            {
                DateTime dateTime = DateTime.Now.AddMinutes(-1.0);
                if (this._communicationChannel.LastReceivedMessageTime > dateTime || this._communicationChannel.LastSentMessageTime > dateTime)
                    return;
                this._communicationChannel.SendMessage((IScsMessage)new ScsPingMessage());
            }
            catch
            {
            }
        }

        protected virtual void OnConnected()
        {
            EventHandler connected = this.Connected;
            if (connected == null)
                return;
            connected((object)this, EventArgs.Empty);
        }

        protected virtual void OnDisconnected()
        {
            EventHandler disconnected = this.Disconnected;
            if (disconnected == null)
                return;
            disconnected((object)this, EventArgs.Empty);
        }

        protected virtual void OnMessageReceived(IScsMessage message)
        {
            EventHandler<MessageEventArgs> messageReceived = this.MessageReceived;
            if (messageReceived == null)
                return;
            messageReceived((object)this, new MessageEventArgs(message));
        }

        protected virtual void OnMessageSent(IScsMessage message)
        {
            EventHandler<MessageEventArgs> messageSent = this.MessageSent;
            if (messageSent == null)
                return;
            messageSent((object)this, new MessageEventArgs(message));
        }
    }
}
