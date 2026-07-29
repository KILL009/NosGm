using NosGm.Communication.Client;
using NosGm.Master.Library.Data;
using NosGm.Master.Library.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Master.Library.Client
{
    internal sealed class ScsClusterCommunicationTransport
        : IClusterCommunicationTransport
    {
        private readonly Func<ICommunicationService> _serviceProxy;

        public ScsClusterCommunicationTransport(
            Func<ICommunicationService> serviceProxy)
        {
            _serviceProxy = serviceProxy ??
                throw new ArgumentNullException(nameof(serviceProxy));
        }

        public Task<CommunicationTransportResultCode> RegisterAccountLoginAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _serviceProxy().RegisterAccountLogin(
                accountId,
                sessionId,
                ipAddress);
            return Success();
        }

        public Task<CommunicationBooleanResult>
            IsAccountSessionRegisteredAsync(
                long accountId,
                int sessionId,
                CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return BooleanResult(
                _serviceProxy().IsAccountSessionRegistered(
                    accountId,
                    sessionId));
        }

        public Task<CommunicationBooleanResult> IsLoginPermittedAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return BooleanResult(
                _serviceProxy().IsLoginPermitted(accountId, sessionId));
        }

        public Task<CommunicationBooleanResult> IsAccountConnectedAsync(
            long accountId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return BooleanResult(
                _serviceProxy().IsAccountConnected(accountId));
        }

        public Task<CommunicationTransportResultCode> ConnectAccountAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool connected = _serviceProxy().ConnectAccount(
                worldId,
                accountId,
                sessionId);
            return Task.FromResult(
                connected
                    ? CommunicationTransportResultCode.Success
                    : CommunicationTransportResultCode.NotFound);
        }

        public Task<CommunicationTransportResultCode> DisconnectAccountAsync(
            long accountId,
            int sessionId,
            bool preserveSessionRegistration,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _serviceProxy().DisconnectAccount(
                accountId,
                sessionId,
                preserveSessionRegistration);
            return Success();
        }

        public Task<CommunicationTransportResultCode> PulseAccountAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _serviceProxy().PulseAccount(accountId);
            return Success();
        }

        public Task<CommunicationTransportResultCode> ConnectCharacterAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool connected = _serviceProxy().ConnectCharacter(
                worldId,
                characterId);
            return Task.FromResult(
                connected
                    ? CommunicationTransportResultCode.Success
                    : CommunicationTransportResultCode.NotFound);
        }

        public Task<CommunicationTransportResultCode> DisconnectCharacterAsync(
            Guid worldId,
            long accountId,
            int sessionId,
            long characterId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _serviceProxy().DisconnectCharacter(worldId, characterId);
            return Success();
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
            cancellationToken.ThrowIfCancellationRequested();
            int? channelId = _serviceProxy().RegisterWorldServer(
                new SerializableWorldServer(
                    worldId,
                    endpointIp,
                    endpointPort,
                    accountLimit,
                    worldGroup));
            return Task.FromResult(
                new CommunicationWorldRegistrationResult
                {
                    Result = channelId.HasValue
                        ? CommunicationTransportResultCode.Success
                        : CommunicationTransportResultCode.Unavailable,
                    ChannelId = channelId.GetValueOrDefault()
                });
        }

        public Task<CommunicationTransportResultCode>
            UnregisterWorldServerAsync(
                Guid worldId,
                CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _serviceProxy().UnregisterWorldServer(worldId);
            return Success();
        }

        public Task<CommunicationWorldListResult> ListWorldServersAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException(
                "Legacy SCS returns a rendered NsTeST packet. The typed World list must not parse or transport that client packet.");
        }

        private static Task<CommunicationBooleanResult> BooleanResult(bool value)
        {
            return Task.FromResult(
                new CommunicationBooleanResult
                {
                    Result = CommunicationTransportResultCode.Success,
                    Value = value
                });
        }

        private static Task<CommunicationTransportResultCode> Success()
        {
            return Task.FromResult(
                CommunicationTransportResultCode.Success);
        }
    }
}
