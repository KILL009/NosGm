// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;
using NosGM.Web.Contracts;
using NosGM.Web.Services;

namespace NosGM.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class RankingsModel(IPortalDataSource dataSource) : PortalPageModel
{
    [BindProperty(SupportsGet = true)]
    public string Kind { get; set; } = "combat";

    public RankingKind SelectedKind { get; private set; }
    public IReadOnlyList<PublicRankingEntry> Entries { get; private set; } = Array.Empty<PublicRankingEntry>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        SelectedKind = Kind.ToLowerInvariant() switch
        {
            "reputation" => RankingKind.Reputation,
            "hero" => RankingKind.Hero,
            _ => RankingKind.Combat
        };
        Kind = SelectedKind.ToString().ToLowerInvariant();
        Entries = await dataSource.GetRankingsAsync(SelectedKind, 20, cancellationToken);
    }
}
