using NosGm.SCS.Communication.Scs.Communication.Messages;
using NosGm.SCS.Communication.Scs.Communication.Protocols;
using System;

namespace NosGm.SCS.Communication.Scs.Communication.Messengers
{
    public interface IMessenger
    {
        event EventHandler<MessageEventArgs> MessageReceived;

        event EventHandler<MessageEventArgs> MessageSent;

        IScsWireProtocol WireProtocol { get; set; }

        DateTime LastReceivedMessageTime { get; }

        DateTime LastSentMessageTime { get; }

        void SendMessage(IScsMessage message);
    }
}
