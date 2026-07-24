// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;
using NosGM.Web.Contracts;
using NosGM.Web.Services;

namespace NosGM.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class IndexModel(IPortalDataSource dataSource) : PortalPageModel
{
    public IReadOnlyList<PublicNewsItem> News { get; private set; } = Array.Empty<PublicNewsItem>();
    public PublicServerStatus? Status { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        News = await dataSource.GetNewsAsync(Language, 3, cancellationToken);
        Status = await dataSource.GetStatusAsync(cancellationToken);
    }
}
