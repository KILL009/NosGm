using NosGm.SCS.Communication.Scs.Communication.Messages;
using NosGm.SCS.Communication.Scs.Communication.Protocols;
using NosGm.SCS.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace NosGm.SCS.Communication.Scs.Communication.Messengers
{
    public class RequestReplyMessenger<T> : IMessenger, IDisposable where T : IMessenger
    {
        private readonly object _syncObj = new object();
        private const int DefaultTimeout = 60000;
        private readonly SortedList<string, RequestReplyMessenger<T>.WaitingMessage> _waitingMessages;
        private readonly SequentialItemProcessor<IScsMessage> _incomingMessageProcessor;

        [CompilerGenerated]
        public event EventHandler<MessageEventArgs> MessageReceived;

        [CompilerGenerated]
        public event EventHandler<MessageEventArgs> MessageSent;

        public IScsWireProtocol WireProtocol
        {
            get => this.Messenger.WireProtocol;
            set => this.Messenger.WireProtocol = value;
        }

        public DateTime LastReceivedMessageTime => this.Messenger.LastReceivedMessageTime;

        public DateTime LastSentMessageTime => this.Messenger.LastSentMessageTime;

        public T Messenger { get; private set; }

        public int Timeout { get; set; }

        public RequestReplyMessenger(T messenger)
            : this(messenger, 1)
        {
        }

        public RequestReplyMessenger(T messenger, int maximumInboundConcurrency)
        {
            this.Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            messenger.MessageReceived += new EventHandler<MessageEventArgs>(this.Messenger_MessageReceived);
            messenger.MessageSent += new EventHandler<MessageEventArgs>(this.Messenger_MessageSent);
            this._incomingMessageProcessor =
                new SequentialItemProcessor<IScsMessage>(
                    new Action<IScsMessage>(this.OnMessageReceived),
                    maximumInboundConcurrency);
            this._waitingMessages = new SortedList<string, RequestReplyMessenger<T>.WaitingMessage>();
            this.Timeout = DefaultTimeout;
        }

        public virtual void Start() => this._incomingMessageProcessor.Start();

        public virtual void Stop()
        {
            this._incomingMessageProcessor.Stop();
            lock (this._syncObj)
            {
                foreach (RequestReplyMessenger<T>.WaitingMessage waitingMessage in (IEnumerable<RequestReplyMessenger<T>.WaitingMessage>)this._waitingMessages.Values)
                {
                    waitingMessage.State = RequestReplyMessenger<T>.WaitingMessageStates.Cancelled;
                    waitingMessage.WaitEvent.Set();
                }
                this._waitingMessages.Clear();
            }
        }

        public void Dispose() => this.Stop();

        public void SendMessage(IScsMessage message) => this.Messenger.SendMessage(message);

        public IScsMessage SendMessageAndWaitForResponse(IScsMessage message)
        {
            return this.SendMessageAndWaitForResponse(message, this.Timeout);
        }

        public IScsMessage SendMessageAndWaitForResponse(IScsMessage message, int timeoutMilliseconds)
        {
            RequestReplyMessenger<T>.WaitingMessage waitingMessage = new RequestReplyMessenger<T>.WaitingMessage();
            lock (this._syncObj)
                this._waitingMessages[message.MessageId] = waitingMessage;
            try
            {
                this.Messenger.SendMessage(message);
                waitingMessage.WaitEvent.Wait(timeoutMilliseconds);
                switch (waitingMessage.State)
                {
                    case RequestReplyMessenger<T>.WaitingMessageStates.WaitingForResponse:
                        throw new TimeoutException("Timeout occured. Can not received response.");
                    case RequestReplyMessenger<T>.WaitingMessageStates.Cancelled:
                        throw new CommunicationException("Disconnected before response received.");
                    default:
                        return waitingMessage.ResponseMessage;
                }
            }
            finally
            {
                lock (this._syncObj)
                {
                    if (this._waitingMessages.ContainsKey(message.MessageId))
                        this._waitingMessages.Remove(message.MessageId);
                }
            }
        }

        private void Messenger_MessageReceived(object sender, MessageEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message.RepliedMessageId))
            {
                RequestReplyMessenger<T>.WaitingMessage waitingMessage = (RequestReplyMessenger<T>.WaitingMessage)null;
                lock (this._syncObj)
                {
                    if (this._waitingMessages.ContainsKey(e.Message.RepliedMessageId))
                        waitingMessage = this._waitingMessages[e.Message.RepliedMessageId];
                }
                if (waitingMessage != null)
                {
                    waitingMessage.ResponseMessage = e.Message;
                    waitingMessage.State = RequestReplyMessenger<T>.WaitingMessageStates.ResponseReceived;
                    waitingMessage.WaitEvent.Set();
                    return;
                }
            }
            this._incomingMessageProcessor.EnqueueMessage(e.Message);
        }

        private void Messenger_MessageSent(object sender, MessageEventArgs e)
        {
            this.OnMessageSent(e.Message);
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

        private sealed class WaitingMessage
        {
            public IScsMessage ResponseMessage { get; set; }

            public ManualResetEventSlim WaitEvent { get; private set; }

            public RequestReplyMessenger<T>.WaitingMessageStates State { get; set; }

            public WaitingMessage()
            {
                this.WaitEvent = new ManualResetEventSlim(false);
                this.State = RequestReplyMessenger<T>.WaitingMessageStates.WaitingForResponse;
            }
        }

        private enum WaitingMessageStates
        {
            WaitingForResponse,
            Cancelled,
            ResponseReceived,
        }
    }
}
