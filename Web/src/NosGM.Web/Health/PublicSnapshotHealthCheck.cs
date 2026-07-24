// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Diagnostics.HealthChecks;
using NosGM.Web.Services;

namespace NosGM.Web.Health;

public sealed class PublicSnapshotHealthCheck(IPublicDataHealth health) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (health.IsReady)
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                health.ObservedAt is null
                    ? "Public snapshot is ready."
                    : $"Public snapshot observed at {health.ObservedAt:O}."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy(
            health.LastError ?? "Public snapshot is missing or stale."));
    }
}
