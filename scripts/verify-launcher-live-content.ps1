param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required source file was not found: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Assert-Contains([string]$source, [string]$expected, [string]$name) {
    if (-not $source.Contains($expected)) {
        throw "$name failed. Missing source contract: $expected"
    }

    Write-Host "[PASS] $name"
}

function Assert-NotContains([string]$source, [string]$unexpected, [string]$name) {
    if ($source.Contains($unexpected)) {
        throw "$name failed. Forbidden source contract remains: $unexpected"
    }

    Write-Host "[PASS] $name"
}

$settings = Read-Source "Launcher/src/NosGM.Launcher/LauncherSettings.cs"
$client = Read-Source "Launcher/src/NosGM.Launcher/LauncherLiveContentClient.cs"
$window = Read-Source "Launcher/src/NosGM.Launcher/MainWindow.LiveContent.cs"
$portal = Read-Source "Web/src/NosGM.Web/Program.cs"

Assert-Contains $settings 'public string PortalBaseUri { get; init; } = "http://localhost:5080/";' `
    "Launcher has a local safe portal default"
Assert-Contains $settings 'NOSGM_PORTAL_BASE_URI' `
    "Portal base URI supports a process-only deployment override"
Assert-Contains $settings 'Uri.UriSchemeHttps' `
    "Remote portal endpoints require HTTPS"
Assert-Contains $settings 'Uri.UriSchemeHttp' `
    "Local portal development supports loopback HTTP"
Assert-Contains $settings 'uri.IsLoopback' `
    "Plain HTTP is restricted to loopback"
Assert-Contains $settings '!string.IsNullOrEmpty(uri.UserInfo)' `
    "Portal endpoints reject embedded credentials"
Assert-Contains $settings '!string.IsNullOrEmpty(uri.Query)' `
    "Portal base endpoints reject fixed query strings"
Assert-Contains $settings '!string.IsNullOrEmpty(uri.Fragment)' `
    "Portal base endpoints reject fragments"

Assert-Contains $client 'AllowAutoRedirect = false' `
    "Live content requests reject redirects"
Assert-Contains $client 'UseCookies = false' `
    "Live content requests do not create a portal session"
Assert-Contains $client 'CheckCertificateRevocationList = true' `
    "Live content HTTPS validates certificate revocation"
Assert-Contains $client 'MaximumResponseBytes = 256 * 1024' `
    "Portal responses are bounded"
Assert-Contains $client 'api/v1/public/metadata' `
    "Launcher consumes the versioned metadata endpoint"
Assert-Contains $client 'api/v1/public/news?lang=' `
    "Launcher consumes localized versioned news"
Assert-Contains $client 'api/v1/public/status' `
    "Launcher consumes versioned server health"
Assert-Contains $client 'UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow' `
    "Unknown live-content JSON fields fail closed"
Assert-Contains $client 'live-content-cache.json' `
    "Validated live content has an offline cache"
Assert-Contains $client 'MaximumCacheAge = TimeSpan.FromDays(7)' `
    "Stale cache entries expire"
Assert-Contains $client 'File.Move(temporaryPath, CachePath, overwrite: true);' `
    "Live content cache replacement is atomic"
Assert-NotContains $client 'Password' `
    "Live content models contain no password"
Assert-NotContains $client 'AuthorizationCode' `
    "Live content models contain no authorization code"
Assert-NotContains $client 'AccountName' `
    "Live content models contain no account identity"

Assert-Contains $window 'await LauncherLiveContentCache.LoadAsync' `
    "Launcher paints cached content before network refresh"
Assert-Contains $window 'Interval = TimeSpan.FromSeconds(30)' `
    "Live dashboard refresh is rate-limited"
Assert-Contains $window 'RefreshServerStatusAsync();' `
    "Local TCP probes remain as a fallback"
Assert-Contains $window 'snapshot.Status.OnlinePlayers' `
    "Launcher displays real online population"
Assert-Contains $window 'ApplyNews(snapshot.News);' `
    "Hardcoded dashboard news is replaced by live news"
Assert-Contains $window 'Portal no disponible' `
    "Portal failure is represented without crashing the launcher"
Assert-Contains $window 'for (var attempt = 0; attempt < 100' `
    "Live content waits for launcher settings initialization"

Assert-Contains $portal 'app.MapGroup("/api/v1/public")' `
    "Portal exposes the versioned public API"
Assert-Contains $portal 'publicApi.MapGet("/news"' `
    "Portal exposes bounded news"
Assert-Contains $portal 'publicApi.MapGet("/status"' `
    "Portal exposes server health"

Write-Host "Launcher live content, endpoint safety, cache and fallback contracts passed."
