// SPDX-License-Identifier: MIT

using NosGM.Web;
using NosGM.Web.Contracts;
using NosGM.Web.Localization;
using NosGM.Web.Services;

PortalText.ValidateCatalogs();
Assert(PortalCulture.SupportedLanguages.Count == 10, "Ten languages are required.");
Assert(PortalCulture.Normalize("cz") == "cs", "Czech alias failed.");
Assert(PortalCulture.Normalize("jp") == "ja", "Japanese alias failed.");
Assert(PortalCulture.Normalize("cn") == "zh-CN", "Chinese alias failed.");
Assert(PortalCulture.Normalize("unknown") == "en", "Unknown language must fall back to English.");
Assert(WebSecurityPolicy.ContentSecurityPolicy.Contains("default-src 'none'", StringComparison.Ordinal), "CSP default deny missing.");
Assert(WebSecurityPolicy.ContentSecurityPolicy.Contains("frame-ancestors 'none'", StringComparison.Ordinal), "Frame denial missing.");
Assert(PortalOptions.IsSafe(new PortalOptions { LauncherDownloadUrl = string.Empty }), "Empty launcher URL should be allowed.");
Assert(!PortalOptions.IsSafe(new PortalOptions { LauncherDownloadUrl = "http://example.invalid/file" }), "HTTP launcher URL must be rejected.");

var source = new SafeDemoPortalDataSource();
var news = await source.GetNewsAsync("es", 20, CancellationToken.None);
var status = await source.GetStatusAsync(CancellationToken.None);
var ranking = await source.GetRankingsAsync(RankingKind.Combat, 50, CancellationToken.None);
Assert(news.Count is > 0 and <= 20, "News limit failed.");
Assert(status.Services.Count > 0, "Status services missing.");
Assert(status.OnlinePlayers == status.Services.Sum(item => item.OnlinePlayers), "Online total mismatch.");
Assert(ranking.Count == 50, "Ranking limit failed.");
Assert(ranking.Select(item => item.Position).Distinct().Count() == ranking.Count, "Ranking positions are not unique.");
Assert(ranking.All(item => item.CharacterName.StartsWith("Explorer", StringComparison.Ordinal)), "Synthetic names changed unexpectedly.");

Console.WriteLine("NosGM.Web self-test passed.");
return;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
