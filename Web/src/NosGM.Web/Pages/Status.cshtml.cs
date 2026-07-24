// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;
using NosGM.Web.Contracts;
using NosGM.Web.Services;

namespace NosGM.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class StatusModel(IPortalDataSource dataSource) : PortalPageModel
{
    public PublicServerStatus? Snapshot { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
        => Snapshot = await dataSource.GetStatusAsync(cancellationToken);
}
