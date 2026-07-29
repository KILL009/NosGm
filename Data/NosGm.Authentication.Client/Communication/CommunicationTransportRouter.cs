using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationTransportRouter
        : IClusterCommunicationTransport
    {
        private readonly IClusterCommunicationTransport _selectedTransport;

        public CommunicationTransportRouter(
            CommunicationTransportMode mode,
            IClusterCommunicationTransport scsTransport,
            IClusterCommunicationTransport grpcTransport)
        {
            Mode = mode;
            _selectedTransport = mode == CommunicationTransportMode.Scs
                ? scsTransport
                : grpcTransport;
            if (_selectedTransport == null)
            {
                throw new InvalidOperationException(
                    "The selected communication transport is not configured.");
            }
        }

        public CommunicationTransportMode Mode { get; }

        public Task<CommunicationTransportResultCode> RegisterAccountLoginAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.RegisterAccountLoginAsync(
                accountId,
                sessionId,
                ipAddress,
                cancellationToken);
        }

        public Task<CommunicationBooleanResult> IsAccountSessionRegisteredAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.IsAccountSessionRegisteredAsync(
                accountId,
                sessionId,
                cancellationToken);
        }

        public Task<CommunicationBooleanResult> IsLoginPermittedAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.IsLoginPermittedAsync(
                accountId,
                sessionId,
                cancellationToken);
        }

        public Task<CommunicationBooleanResult> IsAccountConnectedAsync(
            long accountId,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.IsAccountConnectedAsync(
                accountId,
                cancellationToken);
        }

        public Task<CommunicationTransportResultCode> ConnectAccountAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.ConnectAccountAsync(
                worldId,
                accountId,
                sessionId,
                cancellationToken);
        }

        public Task<CommunicationTransportResultCode> DisconnectAccountAsync(
            long accountId,
            int sessionId,
            bool preserveSessionRegistration,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.DisconnectAccountAsync(
                accountId,
                sessionId,
                preserveSessionRegistration,
                cancellationToken);
        }

        public Task<CommunicationTransportResultCode> PulseAccountAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.PulseAccountAsync(
                accountId,
                sessionId,
                cancellationToken);
        }

        public Task<CommunicationTransportResultCode> ConnectCharacterAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.ConnectCharacterAsync(
                worldId,
                accountId,
                sessionId,
                characterId,
                cancellationToken);
        }

        public Task<CommunicationTransportResultCode> DisconnectCharacterAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.DisconnectCharacterAsync(
                worldId,
                accountId,
                sessionId,
                characterId,
                cancellationToken);
        }

        public Task<CommunicationWorldRegistrationResult>
            RegisterWorldServerAsync(
                Guid worldId,
                string endpointIp,
                int endpointPort,
                int accountLimit,
                string worldGroup,
                CancellationToken cancellationToken)
        {
            return _selectedTransport.RegisterWorldServerAsync(
                worldId,
                endpointIp,
                endpointPort,
                accountLimit,
                worldGroup,
                cancellationToken);
        }

        public Task<CommunicationTransportResultCode> UnregisterWorldServerAsync(
            Guid worldId,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.UnregisterWorldServerAsync(
                worldId,
                cancellationToken);
        }

        public Task<CommunicationWorldListResult> ListWorldServersAsync(
            CancellationToken cancellationToken)
        {
            return _selectedTransport.ListWorldServersAsync(cancellationToken);
        }
    }
}
