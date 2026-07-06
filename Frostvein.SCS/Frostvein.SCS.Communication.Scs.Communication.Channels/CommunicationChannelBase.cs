using Frostvein.SCS.Communication.Scs.Communication.EndPoints;
using Frostvein.SCS.Communication.Scs.Communication.Messages;
using Frostvein.SCS.Communication.Scs.Communication.Messengers;
using Frostvein.SCS.Communication.Scs.Communication.Protocols;
using System;
using System.Runtime.CompilerServices;

namespace Frostvein.SCS.Communication.Scs.Communication.Channels
{
    internal abstract class CommunicationChannelBase : ICommunicationChannel, IMessenger
    {
        [CompilerGenerated]
        public event EventHandler<MessageEventArgs> MessageReceived;

        [CompilerGenerated]
        public event EventHandler<MessageEventArgs> MessageSent;

        [CompilerGenerated]
        public event EventHandler Disconnected;

        public abstract ScsEndPoint RemoteEndPoint { get; }

        public CommunicationStates CommunicationState { get; protected set; }

        public DateTime LastReceivedMessageTime { get; protected set; }

        public DateTime LastSentMessageTime { get; protected set; }

        public IScsWireProtocol WireProtocol { get; set; }

        protected CommunicationChannelBase()
        {
            this.CommunicationState = CommunicationStates.Disconnected;
            this.LastReceivedMessageTime = DateTime.MinValue;
            this.LastSentMessageTime = DateTime.MinValue;
        }

        public abstract void Disconnect();

        public void Start()
        {
            this.StartInternal();
            this.CommunicationState = CommunicationStates.Connected;
        }

        public void SendMessage(IScsMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            this.SendMessageInternal(message);
        }

        protected abstract void StartInternal();

        protected abstract void SendMessageInternal(IScsMessage message);

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
