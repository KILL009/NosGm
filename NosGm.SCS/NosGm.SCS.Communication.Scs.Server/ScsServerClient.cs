using NosGm.SCS.Communication.Scs.Communication;
using NosGm.SCS.Communication.Scs.Communication.Channels;
using NosGm.SCS.Communication.Scs.Communication.EndPoints;
using NosGm.SCS.Communication.Scs.Communication.Messages;
using NosGm.SCS.Communication.Scs.Communication.Messengers;
using NosGm.SCS.Communication.Scs.Communication.Protocols;
using System;
using System.Runtime.CompilerServices;

namespace NosGm.SCS.Communication.Scs.Server
{
    internal class ScsServerClient : IScsServerClient, IMessenger
    {
        private readonly ICommunicationChannel _communicationChannel;

        [CompilerGenerated]
        public event EventHandler<MessageEventArgs> MessageReceived;

        [CompilerGenerated]
        public event EventHandler<MessageEventArgs> MessageSent;

        [CompilerGenerated]
        public event EventHandler Disconnected;

        public long ClientId { get; set; }

        public CommunicationStates CommunicationState => this._communicationChannel.CommunicationState;

        public IScsWireProtocol WireProtocol
        {
            get => this._communicationChannel.WireProtocol;
            set => this._communicationChannel.WireProtocol = value;
        }

        public ScsEndPoint RemoteEndPoint => this._communicationChannel.RemoteEndPoint;

        public DateTime LastReceivedMessageTime => this._communicationChannel.LastReceivedMessageTime;

        public DateTime LastSentMessageTime => this._communicationChannel.LastSentMessageTime;

        public ScsServerClient(ICommunicationChannel communicationChannel)
        {
            this._communicationChannel = communicationChannel;
            this._communicationChannel.MessageReceived += new EventHandler<MessageEventArgs>(this.CommunicationChannel_MessageReceived);
            this._communicationChannel.MessageSent += new EventHandler<MessageEventArgs>(this.CommunicationChannel_MessageSent);
            this._communicationChannel.Disconnected += new EventHandler(this.CommunicationChannel_Disconnected);
        }

        public void Disconnect() => this._communicationChannel.Disconnect();

        public void SendMessage(IScsMessage message) => this._communicationChannel.SendMessage(message);

        private void CommunicationChannel_Disconnected(object sender, EventArgs e)
        {
            this.OnDisconnected();
        }

        private void CommunicationChannel_MessageReceived(object sender, MessageEventArgs e)
        {
            IScsMessage message1 = e.Message;
            if (message1 is ScsPingMessage)
            {
                ICommunicationChannel communicationChannel = this._communicationChannel;
                ScsPingMessage scsPingMessage = new ScsPingMessage();
                scsPingMessage.RepliedMessageId = message1.MessageId;
                ScsPingMessage message2 = scsPingMessage;
                communicationChannel.SendMessage((IScsMessage)message2);
            }
            else
                this.OnMessageReceived(message1);
        }

        private void CommunicationChannel_MessageSent(object sender, MessageEventArgs e)
        {
            this.OnMessageSent(e.Message);
        }

        private void OnDisconnected()
        {
            EventHandler disconnected = this.Disconnected;
            if (disconnected == null)
                return;
            disconnected((object)this, EventArgs.Empty);
        }

        private void OnMessageReceived(IScsMessage message)
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
