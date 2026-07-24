// SPDX-License-Identifier: MIT

using NosGM.Web.Contracts;

namespace NosGM.Web.Services;

public interface IPortalDataSource
{
    ValueTask<IReadOnlyList<PublicNewsItem>> GetNewsAsync(
        string language,
        int limit,
        CancellationToken cancellationToken);

    ValueTask<PublicServerStatus> GetStatusAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<PublicRankingEntry>> GetRankingsAsync(
        RankingKind kind,
        int limit,
        CancellationToken cancellationToken);
}
