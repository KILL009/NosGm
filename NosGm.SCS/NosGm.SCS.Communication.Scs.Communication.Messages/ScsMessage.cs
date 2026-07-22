using System;

namespace NosGm.SCS.Communication.Scs.Communication.Messages
{
    [Serializable]
    public class ScsMessage : IScsMessage
    {
        public string MessageId { get; set; }

        public string RepliedMessageId { get; set; }

        public ScsMessage() => this.MessageId = Guid.NewGuid().ToString();

        public ScsMessage(string repliedMessageId)
          : this()
        {
            this.RepliedMessageId = repliedMessageId;
        }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(this.RepliedMessageId) ? string.Format("ScsMessage [{0}] Replied To [{1}]", (object)this.MessageId, (object)this.RepliedMessageId) : string.Format("ScsMessage [{0}]", (object)this.MessageId);
        }
    }
}
