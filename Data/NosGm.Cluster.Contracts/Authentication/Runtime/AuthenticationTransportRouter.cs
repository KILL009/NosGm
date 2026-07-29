using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Cluster.Contracts.Authentication.Runtime
{
    public sealed class AuthenticationTransportRouter
        : IGameforgeAuthenticationTransport
    {
        private readonly IGameforgeAuthenticationTransport _selectedTransport;

        public AuthenticationTransportRouter(
            AuthenticationTransportMode mode,
            IGameforgeAuthenticationTransport scsTransport,
            IGameforgeAuthenticationTransport grpcTransport)
        {
            if (!Enum.IsDefined(typeof(AuthenticationTransportMode), mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            Mode = mode;
            _selectedTransport = mode == AuthenticationTransportMode.Scs
                ? scsTransport
                : grpcTransport;
            if (_selectedTransport == null)
            {
                throw new InvalidOperationException(
                    mode +
                    " authentication transport was selected but is unavailable.");
            }
        }

        public AuthenticationTransportMode Mode { get; }

        public Task<AuthenticationTransportResultCode> IssueAuthTicketAsync(
            string accountName,
            string authorizationCode,
            string installationId,
            uint countryId,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.IssueAuthTicketAsync(
                accountName,
                authorizationCode,
                installationId,
                countryId,
                cancellationToken);
        }

        public Task<AuthenticationTicketConsumptionResult>
            ConsumeAuthTicketAsync(
                string authorizationCode,
                string installationId,
                uint countryId,
                int proposedSessionId,
                CancellationToken cancellationToken)
        {
            return _selectedTransport.ConsumeAuthTicketAsync(
                authorizationCode,
                installationId,
                countryId,
                proposedSessionId,
                cancellationToken);
        }

        public Task<AuthenticationTransportResultCode> IssueWorldPermitAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.IssueWorldPermitAsync(
                accountId,
                sessionId,
                ipAddress,
                cancellationToken);
        }

        public Task<AuthenticationTransportResultCode> ConsumeWorldPermitAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.ConsumeWorldPermitAsync(
                accountId,
                sessionId,
                ipAddress,
                cancellationToken);
        }

        public Task<AuthenticationTransportResultCode> RevokeWorldPermitAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            return _selectedTransport.RevokeWorldPermitAsync(
                accountId,
                sessionId,
                cancellationToken);
        }
    }
}
