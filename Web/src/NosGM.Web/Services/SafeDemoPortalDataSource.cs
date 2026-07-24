// SPDX-License-Identifier: MIT

using NosGM.Web.Contracts;
using NosGM.Web.Localization;

namespace NosGM.Web.Services;

public sealed class SafeDemoPortalDataSource : IPortalDataSource
{
    private static readonly DateTimeOffset StableObservation =
        new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    public ValueTask<IReadOnlyList<PublicNewsItem>> GetNewsAsync(
        string language,
        int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = PortalCulture.Normalize(language);
        var title = PortalText.Get(normalized, "hero.eyebrow");
        var summary = PortalText.Get(normalized, "security.body");
        IReadOnlyList<PublicNewsItem> items =
        [
            new(
                "foundation-1",
                "secure-foundation",
                title,
                summary,
                StableObservation)
        ];
        return ValueTask.FromResult<IReadOnlyList<PublicNewsItem>>(items.Take(Math.Clamp(limit, 1, 20)).ToArray());
    }

    public ValueTask<PublicServerStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<PublicServiceStatus> services =
        [
            new("login", "Login", ServiceHealth.Online, 0),
            new("world", "World", ServiceHealth.Online, 0),
            new("channel-1", "Channel 1", ServiceHealth.Degraded, 0)
        ];
        return ValueTask.FromResult(
            new PublicServerStatus(
                "NosGM",
                ServiceHealth.Degraded,
                services.Sum(service => service.OnlinePlayers),
                services,
                StableObservation));
    }

    public ValueTask<IReadOnlyList<PublicRankingEntry>> GetRankingsAsync(
        RankingKind kind,
        int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var seed = kind switch
        {
            RankingKind.Combat => 100,
            RankingKind.Reputation => 200,
            RankingKind.Hero => 300,
            _ => 0
        };
        var entries = Enumerable.Range(1, Math.Clamp(limit, 1, 50))
            .Select(position => new PublicRankingEntry(
                position,
                $"Explorer{seed + position:000}",
                80 + position % 20,
                10 + position % 70,
                1_000_000L - position * 7_500L))
            .ToArray();
        return ValueTask.FromResult<IReadOnlyList<PublicRankingEntry>>(entries);
    }
}
