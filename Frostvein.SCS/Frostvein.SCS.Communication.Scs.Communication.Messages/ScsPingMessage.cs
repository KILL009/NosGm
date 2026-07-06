using System;

namespace Frostvein.SCS.Communication.Scs.Communication.Messages
{
    [Serializable]
    public sealed class ScsPingMessage : ScsMessage
    {
        public ScsPingMessage()
        {
        }

        public ScsPingMessage(string repliedMessageId)
          : this()
        {
            this.RepliedMessageId = repliedMessageId;
        }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(this.RepliedMessageId) ? string.Format("ScsPingMessage [{0}] Replied To [{1}]", (object)this.MessageId, (object)this.RepliedMessageId) : string.Format("ScsPingMessage [{0}]", (object)this.MessageId);
        }
    }
}
