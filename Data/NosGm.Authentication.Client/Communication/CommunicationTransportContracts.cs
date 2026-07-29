using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Communication.Client
{
    public enum CommunicationTransportResultCode
    {
        Unspecified = 0,
        Success = 1,
        InvalidRequest = 2,
        Unauthorized = 3,
        NotFound = 4,
        Conflict = 5,
        CapacityExceeded = 6,
        Unavailable = 7
    }

    public sealed class CommunicationBooleanResult
    {
        public CommunicationTransportResultCode Result { get; set; }

        public bool Value { get; set; }
    }

    public sealed class CommunicationWorldRegistrationResult
    {
        public CommunicationTransportResultCode Result { get; set; }

        public int ChannelId { get; set; }
    }

    public sealed class CommunicationWorldSnapshot
    {
        public Guid WorldId { get; set; }

        public string EndpointIp { get; set; }

        public int EndpointPort { get; set; }

        public int AccountLimit { get; set; }

        public int ConnectedAccounts { get; set; }

        public int ChannelId { get; set; }

        public string WorldGroup { get; set; }
    }

    public sealed class CommunicationWorldListResult
    {
        public CommunicationTransportResultCode Result { get; set; }

        public IReadOnlyList<CommunicationWorldSnapshot> Worlds { get; set; } =
            Array.Empty<CommunicationWorldSnapshot>();
    }

    public interface IClusterCommunicationTransport
    {
        Task<CommunicationTransportResultCode> RegisterAccountLoginAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken);

        Task<CommunicationBooleanResult> IsAccountSessionRegisteredAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken);

        Task<CommunicationBooleanResult> IsLoginPermittedAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken);

        Task<CommunicationBooleanResult> IsAccountConnectedAsync(
            long accountId,
            CancellationToken cancellationToken);

        Task<CommunicationTransportResultCode> ConnectAccountAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            CancellationToken cancellationToken);

        Task<CommunicationTransportResultCode> DisconnectAccountAsync(
            long accountId,
            int sessionId,
            bool preserveSessionRegistration,
            CancellationToken cancellationToken);

        Task<CommunicationTransportResultCode> PulseAccountAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken);

        Task<CommunicationTransportResultCode> ConnectCharacterAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId,
            CancellationToken cancellationToken);

        Task<CommunicationTransportResultCode> DisconnectCharacterAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId,
            CancellationToken cancellationToken);

        Task<CommunicationWorldRegistrationResult> RegisterWorldServerAsync(
            Guid worldId,
            string endpointIp,
            int endpointPort,
            int accountLimit,
            string worldGroup,
            CancellationToken cancellationToken);

        Task<CommunicationTransportResultCode> UnregisterWorldServerAsync(
            Guid worldId,
            CancellationToken cancellationToken);

        Task<CommunicationWorldListResult> ListWorldServersAsync(
            CancellationToken cancellationToken);
    }
}
