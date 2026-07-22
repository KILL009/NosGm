using System;

namespace NosGm.SCS.Communication.Scs.Communication.Messages
{
    public class MessageEventArgs : EventArgs
    {
        public IScsMessage Message { get; private set; }

        public MessageEventArgs(IScsMessage message) => this.Message = message;
    }
}
