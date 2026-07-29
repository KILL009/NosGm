using System.Threading;
using System.Threading.Tasks;
using NosGm.Cluster.Contracts.Authentication.Runtime;
using NosGm.Master.Library.Interface;

namespace NosGm.Master.Library.Client
{
    internal sealed class ScsGameforgeAuthenticationTransport
        : IGameforgeAuthenticationTransport
    {
        private readonly IAuthentificationService _service;

        public ScsGameforgeAuthenticationTransport(
            IAuthentificationService service)
        {
            _service = service;
        }

        public Task<AuthenticationTransportResultCode> IssueAuthTicketAsync(
            string accountName,
            string authorizationCode,
            string installationId,
            uint countryId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool success = countryId <= byte.MaxValue &&
                _service.RegisterGameforgeAuthTicket(
                    accountName,
                    authorizationCode,
                    installationId,
                    (byte)countryId);
            return Task.FromResult(
                success
                    ? AuthenticationTransportResultCode.Success
                    : AuthenticationTransportResultCode.InvalidRequest);
        }

        public Task<AuthenticationTicketConsumptionResult>
            ConsumeAuthTicketAsync(
                string authorizationCode,
                string installationId,
                uint countryId,
                int proposedSessionId,
                CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GameforgeAuthTicketConsumption consumption =
                countryId <= byte.MaxValue
                    ? _service.ConsumeGameforgeAuthTicket(
                        authorizationCode,
                        installationId,
                        (byte)countryId,
                        proposedSessionId)
                    : null;
            return Task.FromResult(
                consumption == null
                    ? new AuthenticationTicketConsumptionResult
                    {
                        Result = AuthenticationTransportResultCode
                            .NotFoundOrExpired
                    }
                    : new AuthenticationTicketConsumptionResult
                    {
                        Result = AuthenticationTransportResultCode.Success,
                        AccountName = consumption.AccountName,
                        ConsumptionNumber = consumption.ConsumptionNumber,
                        SessionId = consumption.SessionId
                    });
        }

        public Task<AuthenticationTransportResultCode> IssueWorldPermitAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                _service.RegisterGameforgeWorldPermit(
                    accountId,
                    sessionId,
                    ipAddress)
                    ? AuthenticationTransportResultCode.Success
                    : AuthenticationTransportResultCode.InvalidRequest);
        }

        public Task<AuthenticationTransportResultCode> ConsumeWorldPermitAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                _service.ConsumeGameforgeWorldPermit(
                    accountId,
                    sessionId,
                    ipAddress)
                    ? AuthenticationTransportResultCode.Success
                    : AuthenticationTransportResultCode.NotFoundOrExpired);
        }

        public Task<AuthenticationTransportResultCode> RevokeWorldPermitAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _service.RevokeGameforgeWorldPermit(accountId, sessionId);
            return Task.FromResult(AuthenticationTransportResultCode.Success);
        }
    }
}
