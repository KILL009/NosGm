param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required community hub file was not found: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Require([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Launcher community hub contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

function Forbid([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Launcher community hub contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

$client = Read-Source "Launcher/src/NosGM.Launcher/LauncherCommunityClient.cs"
$window = Read-Source "Launcher/src/NosGM.Launcher/LauncherCommunityHubWindow.cs"
$integration = Read-Source "Launcher/src/NosGM.Launcher/MainWindow.CommunityHub.cs"
$documentation = Read-Source "docs/launcher-community-hub.md"

Require $client 'MaximumResponseBytes = 256 * 1024' `
    "Every public community response is bounded"
Require $client 'MaximumNewsItems = 12' `
    "Community news has a fixed display limit"
Require $client 'MaximumRankingEntries = 20' `
    "Every ranking has a fixed display limit"
Require $client 'AllowAutoRedirect = false' `
    "Community requests reject redirects"
Require $client 'UseCookies = false' `
    "Community requests store no cookies"
Require $client 'CheckCertificateRevocationList = true' `
    "Remote TLS checks certificate revocation"
Require $client 'candidate.IsLoopback' `
    "HTTP is limited to loopback development"
Require $client 'Uri.UriSchemeHttps' `
    "Remote portal traffic requires HTTPS"
Require $client 'api/v1/public/status' `
    "Community status uses the versioned public API"
Require $client 'api/v1/public/operations' `
    "Community events use the versioned public API"
Require $client 'api/v1/public/news' `
    "Community news uses the versioned public API"
Require $client 'api/v1/public/rankings/combat' `
    "Combat ranking uses a fixed public endpoint"
Require $client 'api/v1/public/rankings/reputation' `
    "Reputation ranking uses a fixed public endpoint"
Require $client 'api/v1/public/rankings/hero' `
    "Hero ranking uses a fixed public endpoint"
Require $client 'community-cache.json' `
    "Community cache has its own public-data file"
Require $client 'MaximumCacheBytes = 1024 * 1024' `
    "Community cache is bounded to one MiB"
Require $client 'MaximumCacheAge = TimeSpan.FromDays(2)' `
    "Community cache has a short expiry"
Require $client 'File.Move(temporaryPath, CachePath, overwrite: true)' `
    "Community cache replacement is atomic"
Require $client 'UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow' `
    "Unknown public fields are rejected"
Forbid $client 'LauncherAuthenticationClient' `
    "Community client cannot authenticate"
Forbid $client 'LauncherCredentials' `
    "Community client never reads launcher credentials"
Forbid $client 'AuthorizationCode' `
    "Community client never handles authorization codes"
Forbid $client 'AccountName' `
    "Community client never reads account names"
Forbid $client 'Environment.GetEnvironmentVariable' `
    "Community client never reads process environment secrets"

Require $window 'LauncherCommunityCache.LoadAsync' `
    "Community window loads validated public cache"
Require $window 'LauncherCommunityCache.SaveAsync' `
    "Community window persists valid live public data"
Require $window 'LauncherCommunityValidator.Validate' `
    "Community data is validated before display"
Require $window 'CreateLinkedTokenSource(_lifetime.Token)' `
    "Window closure cancels public requests"
Require $window '_closed = true' `
    "Window closure is recorded before asynchronous cleanup"
Require $window '_lifetime.Cancel()' `
    "Window closure cancels cache and portal work"
Require $window 'DisposeResourcesAfterRefreshAsync' `
    "HTTP and synchronization resources wait for active refresh work"
Require $window 'await _refreshGate.WaitAsync().ConfigureAwait(false)' `
    "Cleanup waits for the active refresh to release its lease"
Require $window 'catch (OperationCanceledException) when (_closed || _lifetime.IsCancellationRequested)' `
    "Normal close cancellation is handled without an unobserved exception"
Require $window 'if (!_closed)' `
    "Completed requests do not update controls after close"
Require $window 'target.Port != baseUri.Port' `
    "Community links cannot escape the configured portal origin"
Require $window 'UseShellExecute = true' `
    "Validated portal links open through Windows"
Require $window 'Datos en caché' `
    "Cached data is visibly labeled"
Forbid $window 'PasswordBox' `
    "Community window cannot collect passwords"
Forbid $window 'ModernGameLauncher.LaunchAsync' `
    "Community window cannot start the game"
Forbid $window 'LauncherAuthenticationClient' `
    "Community window cannot request login tickets"
Forbid $window 'AuthorizationCode' `
    "Community window never accesses authorization codes"
Forbid $window 'AccountName' `
    "Community window never accesses account names"

Require $integration 'content.Contains("Foro"' `
    "The unused forum action is repurposed"
Require $integration '_communityHubButton.Click -= OpenExternalLink_Click' `
    "The old empty external-link handler is removed"
Require $integration 'LauncherCommunityHubWindow.Show' `
    "The footer opens the native community window"
Require $integration 'LanguageComboBox.SelectionChanged +=' `
    "The community label follows launcher language changes"
Require $integration '_communityHubButton.Click -= OpenCommunityHub_Click' `
    "Community handler is detached on launcher shutdown"

Require $documentation '/api/v1/public/rankings/combat' `
    "Documentation lists the fixed ranking boundary"
Require $documentation 'never reads or stores' `
    "Documentation states the private-data exclusion boundary"
Require $documentation 'community-cache.json' `
    "Documentation explains the public cache"
Require $documentation 'scheme, host and port' `
    "Documentation explains same-origin links"

Write-Host "NosGM launcher community hub security, privacy and lifecycle contracts passed."
