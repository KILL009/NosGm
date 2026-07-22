using System;

namespace NosGm.SCS.Communication.Scs.Communication.Messages
{
    [Serializable]
    public class ScsTextMessage : ScsMessage
    {
        public string Text { get; set; }

        public ScsTextMessage()
        {
        }

        public ScsTextMessage(string text) => this.Text = text;

        public ScsTextMessage(string text, string repliedMessageId)
          : this(text)
        {
            this.RepliedMessageId = repliedMessageId;
        }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(this.RepliedMessageId) ? string.Format("ScsTextMessage [{0}] Replied To [{1}]: {2}", (object)this.MessageId, (object)this.RepliedMessageId, (object)this.Text) : string.Format("ScsTextMessage [{0}]: {1}", (object)this.MessageId, (object)this.Text);
        }
    }
}
