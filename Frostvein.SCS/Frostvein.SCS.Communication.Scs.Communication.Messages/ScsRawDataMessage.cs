using System;

namespace Frostvein.SCS.Communication.Scs.Communication.Messages
{
    [Serializable]
    public class ScsRawDataMessage : ScsMessage
    {
        public byte[] MessageData { get; set; }

        public ScsRawDataMessage()
        {
        }

        public ScsRawDataMessage(byte[] messageData) => this.MessageData = messageData;

        public ScsRawDataMessage(byte[] messageData, string repliedMessageId)
          : this(messageData)
        {
            this.RepliedMessageId = repliedMessageId;
        }

        public override string ToString()
        {
            int length = this.MessageData == null ? 0 : this.MessageData.Length;
            return !string.IsNullOrEmpty(this.RepliedMessageId) ? string.Format("ScsRawDataMessage [{0}] Replied To [{1}]: {2} bytes", (object)this.MessageId, (object)this.RepliedMessageId, (object)length) : string.Format("ScsRawDataMessage [{0}]: {1} bytes", (object)this.MessageId, (object)length);
        }
    }
}
