using System;

namespace NosGm.Core.Networking.Communication.Scs.Communication.Messages
{
    /// <summary>
    ///     Stores message to be used by an event.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        #region Instantiation

        /// <summary>
        ///     Creates a new MessageEventArgs object.
        /// </summary>
        /// <param name="message">Message object that is associated with this event</param>
        /// <param name="receivedTimestamp">Message reception timestamp</param>
        public MessageEventArgs(IScsMessage message, DateTime receivedTimestamp)
        {
            Message = message;
            ReceivedTimestamp = receivedTimestamp;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Message object that is associated with this event.
        /// </summary>
        public IScsMessage Message { get; }

        public DateTime ReceivedTimestamp { get; set; }

        #endregion
    }
}