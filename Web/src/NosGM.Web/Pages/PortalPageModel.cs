// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc.RazorPages;
using NosGM.Web.Contracts;
using NosGM.Web.Localization;

namespace NosGM.Web.Pages;

public abstract class PortalPageModel : PageModel
{
    public string Language => PortalCulture.Current(HttpContext);

    public string T(string key) => PortalText.Get(Language, key);

    public string Health(ServiceHealth health) => health switch
    {
        ServiceHealth.Online => T("health.online"),
        ServiceHealth.Degraded => T("health.degraded"),
        _ => T("health.offline")
    };

    public string HealthClass(ServiceHealth health) => health switch
    {
        ServiceHealth.Online => "status-online",
        ServiceHealth.Degraded => "status-degraded",
        _ => "status-offline"
    };
}
