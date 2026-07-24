// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Options;

namespace NosGM.Web.Pages;

public sealed class DownloadModel(IOptions<PortalOptions> options) : PortalPageModel
{
    public PortalOptions Options { get; } = options.Value;
}
