using NosGm.Core.Networking.Communication.Scs.Communication;
using NosGm.Core.Networking.Communication.Scs.Communication.EndPoints;
using NosGm.Core.Networking.Communication.Scs.Communication.Messengers;
using System;

namespace NosGm.Core.Networking.Communication.Scs.Server
{
    /// <summary>
    ///     Represents a client from a perspective of a server.
    /// </summary>
    public interface IScsServerClient : IMessenger
    {
        #region Events

        /// <summary>
        ///     This event is raised when client disconnected from server.
        /// </summary>
        event EventHandler Disconnected;

        #endregion

        #region Methods

        /// <summary>
        ///     Disconnects from server.
        /// </summary>
        void Disconnect();

        #endregion

        #region Properties

        /// <summary>
        ///     Unique identifier for this client in server.
        /// </summary>
        long ClientId { get; }

        /// <summary>
        ///     Gets the current communication state.
        /// </summary>
        CommunicationStates CommunicationState { get; }

        /// <summary>
        ///     Gets endpoint of remote application.
        /// </summary>
        ScsEndPoint RemoteEndPoint { get; }

        #endregion
    }
}