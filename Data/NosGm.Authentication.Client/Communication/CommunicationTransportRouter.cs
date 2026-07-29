using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationTransportRouter
        : IClusterCommunicationTransport
    {
        private sealed class CharacterSessionBinding
        {
            public Guid WorldId { get; set; }

            public long AccountId { get; set; }

            public int SessionId { get; set; }
        }

        private readonly ConcurrentDictionary<long, CharacterSessionBinding>
            _characterBindings =
                new ConcurrentDictionary<long, CharacterSessionBinding>();
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

        public async Task<CommunicationTransportResultCode> DisconnectAccountAsync(
            long accountId,
            int sessionId,
            bool preserveSessionRegistration,
            CancellationToken cancellationToken)
        {
            CommunicationTransportResultCode result =
                await _selectedTransport.DisconnectAccountAsync(
                        accountId,
                        sessionId,
                        preserveSessionRegistration,
                        cancellationToken)
                    .ConfigureAwait(false);
            if (result == CommunicationTransportResultCode.Success)
            {
                RemoveAccountBindings(accountId, sessionId);
            }

            return result;
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

        public async Task<CommunicationTransportResultCode> ConnectCharacterAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId,
            CancellationToken cancellationToken)
        {
            ValidateCharacterTuple(
                worldId,
                accountId,
                sessionId,
                characterId,
                "connect");

            CommunicationTransportResultCode result =
                await _selectedTransport.ConnectCharacterAsync(
                        worldId,
                        accountId,
                        sessionId,
                        characterId,
                        cancellationToken)
                    .ConfigureAwait(false);
            if (result == CommunicationTransportResultCode.Success)
            {
                _characterBindings[characterId] =
                    new CharacterSessionBinding
                    {
                        WorldId = worldId,
                        AccountId = accountId,
                        SessionId = sessionId
                    };
            }

            return result;
        }

        public async Task<CommunicationTransportResultCode>
            DisconnectCharacterAsync(
                Guid worldId,
                long accountId,
                int sessionId,
                long characterId,
                CancellationToken cancellationToken)
        {
            if (accountId <= 0 || sessionId <= 0)
            {
                if (!_characterBindings.TryGetValue(
                        characterId,
                        out CharacterSessionBinding binding))
                {
                    throw new InvalidOperationException(
                        "Character disconnect requires the exact account/session binding created during character connection.");
                }

                if (binding.WorldId != worldId)
                {
                    throw new InvalidOperationException(
                        "Character disconnect World identity does not match its registered binding.");
                }

                accountId = binding.AccountId;
                sessionId = binding.SessionId;
            }

            ValidateCharacterTuple(
                worldId,
                accountId,
                sessionId,
                characterId,
                "disconnect");

            CommunicationTransportResultCode result =
                await _selectedTransport.DisconnectCharacterAsync(
                        worldId,
                        accountId,
                        sessionId,
                        characterId,
                        cancellationToken)
                    .ConfigureAwait(false);
            if (result == CommunicationTransportResultCode.Success)
            {
                _characterBindings.TryRemove(characterId, out _);
            }

            return result;
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

        private void RemoveAccountBindings(long accountId, int sessionId)
        {
            foreach (long characterId in _characterBindings
                         .Where(pair =>
                             pair.Value.AccountId == accountId &&
                             (sessionId <= 0 ||
                              pair.Value.SessionId == sessionId))
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                _characterBindings.TryRemove(characterId, out _);
            }
        }

        private static void ValidateCharacterTuple(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId,
            string operation)
        {
            if (worldId == Guid.Empty ||
                accountId <= 0 ||
                sessionId <= 0 ||
                characterId <= 0)
            {
                throw new InvalidOperationException(
                    "Character " + operation +
                    " requires a non-empty World ID and positive account, session, and character IDs.");
            }
        }
    }
}
