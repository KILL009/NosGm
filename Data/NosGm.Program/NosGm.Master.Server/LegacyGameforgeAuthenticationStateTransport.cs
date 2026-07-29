using System;
using System.Threading;
using System.Threading.Tasks;
using NosGm.Cluster.Contracts.Authentication.Runtime;
using NosGm.Configuration;
using NosGm.Master.Library.Interface;

namespace NosGm.Master.Server
{
    internal sealed class LegacyGameforgeAuthenticationStateTransport
        : IGameforgeAuthenticationTransport
    {
        public Task<AuthenticationTransportResultCode> IssueAuthTicketAsync(
            string accountName,
            string authorizationCode,
            string installationId,
            uint countryId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int ttlSeconds = Math.Max(
                15,
                Math.Min(
                    600,
                    ServerConfiguration.GameforgeAuthTicketTtlSeconds));
            bool success =
                countryId <= byte.MaxValue &&
                Guid.TryParseExact(
                    installationId,
                    "D",
                    out Guid parsedInstallationId) &&
                GameforgeAuthTicketStore.Instance.TryIssue(
                    accountName,
                    authorizationCode,
                    parsedInstallationId,
                    (byte)countryId,
                    TimeSpan.FromSeconds(ttlSeconds));
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
            GameforgeAuthTicketConsumption consumption = null;
            bool success =
                countryId <= byte.MaxValue &&
                Guid.TryParseExact(
                    installationId,
                    "D",
                    out Guid parsedInstallationId) &&
                GameforgeAuthTicketStore.Instance.TryConsume(
                    authorizationCode,
                    parsedInstallationId,
                    (byte)countryId,
                    proposedSessionId,
                    out consumption);
            return Task.FromResult(
                success
                    ? new AuthenticationTicketConsumptionResult
                    {
                        Result = AuthenticationTransportResultCode.Success,
                        AccountName = consumption.AccountName,
                        ConsumptionNumber = consumption.ConsumptionNumber,
                        SessionId = consumption.SessionId
                    }
                    : new AuthenticationTicketConsumptionResult
                    {
                        Result = AuthenticationTransportResultCode
                            .NotFoundOrExpired
                    });
        }

        public Task<AuthenticationTransportResultCode> IssueWorldPermitAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int ttlSeconds = Math.Max(
                15,
                Math.Min(
                    600,
                    ServerConfiguration.GameforgeWorldPermitTtlSeconds));
            return Task.FromResult(
                GameforgeWorldPermitStore.Instance.TryIssue(
                    accountId,
                    sessionId,
                    ipAddress,
                    TimeSpan.FromSeconds(ttlSeconds))
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
                GameforgeWorldPermitStore.Instance.TryConsume(
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
            GameforgeWorldPermitStore.Instance.Revoke(accountId, sessionId);
            return Task.FromResult(AuthenticationTransportResultCode.Success);
        }
    }
}
