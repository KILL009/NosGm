using System;

namespace NosGm.Core.Networking.Communication.Scs.Communication.Messages
{
    /// <summary>
    ///     This message is used to send/receive a raw byte array as message data.
    /// </summary>
    [Serializable]
    public class ScsRawDataMessage : ScsMessage, IComparable
    {
        #region Instantiation

        /// <summary>
        ///     Default empty constructor.
        /// </summary>
        public ScsRawDataMessage()
        {
        }

        /// <summary>
        ///     Creates a new ScsRawDataMessage object with MessageData property.
        /// </summary>
        /// <param name="messageData">Message data that is being transmitted</param>
        public ScsRawDataMessage(byte[] messageData)
        {
            MessageData = messageData;
        }

        /// <summary>
        ///     Creates a new reply ScsRawDataMessage object with MessageData property.
        /// </summary>
        /// <param name="messageData">Message data that is being transmitted</param>
        /// <param name="repliedMessageId">Replied message id if this is a reply for a message.</param>
        public ScsRawDataMessage(byte[] messageData, string repliedMessageId) : this(messageData)
        {
            RepliedMessageId = repliedMessageId;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Message data that is being transmitted.
        /// </summary>
        public byte[] MessageData { get; set; }

        public int Priority { get; set; }

        /// <summary>
        /// Marks visual state that may safely be replaced by a newer pending state.
        /// This is transport-local metadata and is never part of serialized SCS contracts.
        /// </summary>
        [field: NonSerialized]
        public bool IsTransient { get; set; }

        /// <summary>
        /// Stable source key used to coalesce replaceable state before the next send tick.
        /// </summary>
        [field: NonSerialized]
        public long TransientKey { get; set; }

        #endregion

        #region Methods

        public static bool operator !=(ScsRawDataMessage left, ScsRawDataMessage right)
        {
            return !(left == right);
        }

        public static bool operator <(ScsRawDataMessage left, ScsRawDataMessage right)
        {
            return left is null ? !(right is null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(ScsRawDataMessage left, ScsRawDataMessage right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator ==(ScsRawDataMessage left, ScsRawDataMessage right)
        {
            if (left is null) return right is null;

            return left.Equals(right);
        }

        public static bool operator >(ScsRawDataMessage left, ScsRawDataMessage right)
        {
            return !(left is null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(ScsRawDataMessage left, ScsRawDataMessage right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        public int CompareTo(object obj)
        {
            return CompareTo((ScsRawDataMessage)obj);
        }

        public int CompareTo(ScsRawDataMessage other)
        {
            return Priority.CompareTo(other.Priority);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            if (obj is ScsRawDataMessage other)
            {
                return MessageId == other.MessageId;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return MessageId != null ? MessageId.GetHashCode() : base.GetHashCode();
        }

        /// <summary>
        ///     Creates a string to represents this object.
        /// </summary>
        /// <returns>A string to represents this object</returns>
        public override string ToString()
        {
            var messageLength = MessageData?.Length ?? 0;
            return string.IsNullOrEmpty(RepliedMessageId)
                ? $"ScsRawDataMessage [{MessageId}]: {messageLength} bytes"
                : $"ScsRawDataMessage [{MessageId}] Replied To [{RepliedMessageId}]: {messageLength} bytes";
        }

        #endregion
    }
}