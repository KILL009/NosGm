// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;
using NosGM.Web.Contracts;
using NosGM.Web.Services;

namespace NosGM.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class NewsModel(IPortalDataSource dataSource) : PortalPageModel
{
    public IReadOnlyList<PublicNewsItem> Items { get; private set; } = Array.Empty<PublicNewsItem>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
        => Items = await dataSource.GetNewsAsync(Language, 20, cancellationToken);
}
