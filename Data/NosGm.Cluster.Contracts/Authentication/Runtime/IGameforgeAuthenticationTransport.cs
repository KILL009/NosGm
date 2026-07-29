using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Cluster.Contracts.Authentication.Runtime
{
    public interface IGameforgeAuthenticationTransport
    {
        Task<AuthenticationTransportResultCode> IssueAuthTicketAsync(
            string accountName,
            string authorizationCode,
            string installationId,
            uint countryId,
            CancellationToken cancellationToken);

        Task<AuthenticationTicketConsumptionResult> ConsumeAuthTicketAsync(
            string authorizationCode,
            string installationId,
            uint countryId,
            int proposedSessionId,
            CancellationToken cancellationToken);

        Task<AuthenticationTransportResultCode> IssueWorldPermitAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken);

        Task<AuthenticationTransportResultCode> ConsumeWorldPermitAsync(
            long accountId,
            int sessionId,
            string ipAddress,
            CancellationToken cancellationToken);

        Task<AuthenticationTransportResultCode> RevokeWorldPermitAsync(
            long accountId,
            int sessionId,
            CancellationToken cancellationToken);
    }
}
