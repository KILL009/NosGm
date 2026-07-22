using NosGm.Core.Networking.Communication.Scs.Communication.Channels;
using NosGm.Core.Networking.Communication.Scs.Communication.Messengers;

namespace NosGm.Core.Networking.Communication.Scs.Client
{
    /// <summary>
    ///     Represents a client to connect to server.
    /// </summary>
    public interface IScsClient : IMessenger, IConnectableClient
    {
        #region Properties

        /// <summary>
        ///     Gets the communication channel for this client.
        /// </summary>
        /// <value>The communication channel.</value>
        ICommunicationChannel CommunicationChannel { get; }

        #endregion
    }
}