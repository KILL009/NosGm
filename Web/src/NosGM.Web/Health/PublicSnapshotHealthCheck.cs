// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Diagnostics.HealthChecks;
using NosGM.Web.Services;

namespace NosGM.Web.Health;

public sealed class PublicSnapshotHealthCheck(
    IPortalDataSource dataSource,
    IPublicDataHealth health) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await dataSource.GetStatusAsync(cancellationToken).ConfigureAwait(false);
        if (health.IsReady)
        {
            return HealthCheckResult.Healthy(
                health.ObservedAt is null
                    ? "Public snapshot is ready."
                    : $"Public snapshot observed at {health.ObservedAt:O}.");
        }

        return HealthCheckResult.Unhealthy(
            health.LastError ?? "Public snapshot is missing or stale.");
    }
}
