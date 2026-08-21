// SPDX-License-Identifier: MIT

using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
Assert(
    PortalText.Languages.All(language => PortalText.Get(language, "weekly.title") != "weekly.title"),
    "Weekly progress copy must be available in every language.");
Assert(WebSecurityPolicy.ContentSecurityPolicy.Contains("default-src 'none'", StringComparison.Ordinal), "CSP default deny missing.");
Assert(WebSecurityPolicy.ContentSecurityPolicy.Contains("frame-ancestors 'none'", StringComparison.Ordinal), "Frame denial missing.");
Assert(PortalOptions.IsSafe(new PortalOptions { LauncherDownloadUrl = string.Empty }), "Empty launcher URL should be allowed.");
Assert(!PortalOptions.IsSafe(new PortalOptions { LauncherDownloadUrl = "http://example.invalid/file" }), "HTTP launcher URL must be rejected.");

var temporaryRoot = Path.Combine(Path.GetTempPath(), $"nosgm-web-selftest-{Guid.NewGuid():N}");
Directory.CreateDirectory(temporaryRoot);
try
{
    var key = Encoding.UTF8.GetBytes("NosGM-self-test-key-that-is-at-least-thirty-two-bytes-long");
    var observedAt = DateTimeOffset.UtcNow;
    var payloadJson = """
        {"serverName":"NosGM","observedAt":"__OBSERVED__","news":[{"id":"launch-1","slug":"server-launch","title":"Servidor listo","summary":"Datos reales publicados desde la red privada.","publishedAt":"__PUBLISHED__","language":"es"},{"id":"launch-1-en","slug":"server-launch","title":"Server ready","summary":"Live data published from the private network.","publishedAt":"__PUBLISHED__","language":"en"}],"services":[{"id":"login","name":"Login","health":"Online","onlinePlayers":0},{"id":"world","name":"World","health":"Online","onlinePlayers":0},{"id":"channel-1","name":"Channel 1","health":"Online","onlinePlayers":17}],"rankings":{"combat":[{"position":1,"characterName":"Blade","level":99,"heroLevel":80,"reputation":900000,"score":42,"metric":"duelWins"}],"reputation":[{"position":1,"characterName":"Nova","level":99,"heroLevel":80,"reputation":1200000,"score":1200000,"metric":"reputation"}],"hero":[{"position":1,"characterName":"Astra","level":99,"heroLevel":90,"reputation":800000,"score":123456789,"metric":"heroXp"}]}}
        """
        .Replace("__OBSERVED__", observedAt.ToString("O"), StringComparison.Ordinal)
        .Replace("__PUBLISHED__", observedAt.AddMinutes(-1).ToString("O"), StringComparison.Ordinal);
    var signature = SignedSnapshotPortalDataSource.ComputeSignatureBase64(
        SignedSnapshotPortalDataSource.SupportedSchemaVersion,
        "nosgm-live-v1",
        payloadJson,
        key);
    var envelopeJson = $"{{\"schemaVersion\":1,\"keyId\":\"nosgm-live-v1\",\"payload\":{payloadJson},\"signature\":\"{signature}\"}}";
    var snapshotPath = Path.Combine(temporaryRoot, "public-snapshot.json");
    await File.WriteAllTextAsync(snapshotPath, envelopeJson, Encoding.UTF8);

    var dataOptions = new PublicDataOptions
    {
        SnapshotPath = snapshotPath,
        KeyId = "nosgm-live-v1",
        HmacKeyBase64 = Convert.ToBase64String(key),
        MaximumAgeSeconds = 3600
    };
    Assert(PublicDataOptions.IsSafe(dataOptions), "Signed snapshot options should be accepted.");

    var source = new SignedSnapshotPortalDataSource(
        Options.Create(dataOptions),
        Options.Create(new PortalOptions()),
        new TestHostEnvironment(temporaryRoot),
        NullLogger<SignedSnapshotPortalDataSource>.Instance);

    var news = await source.GetNewsAsync("es", 20, CancellationToken.None);
    var fallbackNews = await source.GetNewsAsync("de", 20, CancellationToken.None);
    var status = await source.GetStatusAsync(CancellationToken.None);
    var combat = await source.GetRankingsAsync(RankingKind.Combat, 50, CancellationToken.None);
    var hero = await source.GetRankingsAsync(RankingKind.Hero, 50, CancellationToken.None);

    Assert(news.Count == 1 && news[0].Language == "es", "Localized live news failed.");
    Assert(fallbackNews.Count == 1 && fallbackNews[0].Language == "en", "English news fallback failed.");
    Assert(status.Services.Count == 3, "Live status services missing.");
    Assert(status.OnlinePlayers == 17, "Online player total must count channels only.");
    Assert(!status.IsStale, "Fresh signed snapshot was marked stale.");
    Assert(combat.Count == 1 && combat[0].Metric == "duelWins", "Combat ranking failed.");
    Assert(hero.Count == 1 && hero[0].HeroLevel == 90, "Hero ranking failed.");
    Assert(source.IsReady, "Valid signed snapshot should be ready.");

    var tampered = envelopeJson.Replace("\"onlinePlayers\":17", "\"onlinePlayers\":99", StringComparison.Ordinal);
    await File.WriteAllTextAsync(snapshotPath, tampered, Encoding.UTF8);
    File.SetLastWriteTimeUtc(snapshotPath, DateTime.UtcNow.AddSeconds(1));
    var tamperedSource = new SignedSnapshotPortalDataSource(
        Options.Create(dataOptions),
        Options.Create(new PortalOptions()),
        new TestHostEnvironment(temporaryRoot),
        NullLogger<SignedSnapshotPortalDataSource>.Instance);
    var tamperedStatus = await tamperedSource.GetStatusAsync(CancellationToken.None);
    Assert(!tamperedSource.IsReady, "Tampered snapshot must not become ready.");
    Assert(tamperedStatus.OverallHealth == ServiceHealth.Offline, "Tampered snapshot must fail closed.");
}
finally
{
    Directory.Delete(temporaryRoot, recursive: true);
}

Console.WriteLine("NosGM.Web self-test passed.");
return;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

file sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";

    public string ApplicationName { get; set; } = "NosGM.Web.SelfTest";

    public string ContentRootPath { get; set; } = contentRootPath;

    public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRootPath);
}
