param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required live operations file was not found: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Require([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Live operations contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

function Forbid([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Live operations contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

function Assert-PowerShellParses([string]$relativePath) {
    $path = Join-Path $root $relativePath
    $tokens = $null
    $errors = $null
    [void][Management.Automation.Language.Parser]::ParseFile(
        $path,
        [ref]$tokens,
        [ref]$errors)
    if (@($errors).Count -gt 0) {
        throw "PowerShell parse errors in ${relativePath}: $(@($errors).Message -join '; ')"
    }

    Write-Host "[PASS] $relativePath parses on Windows PowerShell"
}

$publisher = Read-Source "Data/NosGm.GameObject/Plugin/Event/PublicOperationsPublisher.cs"
$contracts = Read-Source "Web/src/NosGM.Web.Contracts/PublicOperationsModels.cs"
$page = Read-Source "Web/src/NosGM.Web/Pages/Api/V1/Public/Operations.cshtml.cs"
$client = Read-Source "Launcher/src/NosGM.Launcher/LauncherLiveOperationsClient.cs"
$window = Read-Source "Launcher/src/NosGM.Launcher/MainWindow.LiveOperations.cs"
$helperPath = "scripts/set-local-launcher-operations-test.ps1"
$helper = Read-Source $helperPath
$documentation = Read-Source "docs/launcher-live-operations.md"
$examplePath = Join-Path $root "Web/config/public-events.example.json"

Require $publisher '[ModuleInitializer]' `
    "World operations publisher starts without modifying gameplay handlers"
Require $publisher 'NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64' `
    "Operations share the existing in-memory signing boundary"
Require $publisher 'new HMACSHA256(_key)' `
    "World signs operations with HMAC-SHA256"
Require $publisher 'public-operations.json' `
    "World writes a separate bounded operations document"
Require $publisher 'public-events.json' `
    "World reads an operator-owned public calendar"
Require $publisher 'GameConfiguration.XPRate' `
    "Current EXP rate is published"
Require $publisher 'GameConfiguration.HeroXPRate' `
    "Current Hero EXP rate is published"
Require $publisher 'GameConfiguration.DropRate' `
    "Current drop rate is published"
Require $publisher 'GameConfiguration.FairyXPRate' `
    "Current fairy rate is published"
Require $publisher 'ServerConfiguration.MaintenanceMode' `
    "Server maintenance mode is authoritative"
Require $publisher 'MaximumEvents = 100' `
    "Event collection is bounded"
Require $publisher 'MaximumRate = 10000' `
    "Published multipliers are bounded"
Require $publisher 'WriteAtomic(_operationsPath, envelope);' `
    "Operations publication is atomic"
Forbid $publisher 'AccountName' `
    "World operations do not expose account names"
Forbid $publisher 'Password' `
    "World operations do not expose passwords"
Forbid $publisher 'CharacterId' `
    "World operations do not expose character identifiers"
Forbid $publisher 'MapX' `
    "World operations do not expose coordinates"
Forbid $publisher 'MapY' `
    "World operations do not expose coordinates"

Require $contracts 'PublicRateMultiplier' `
    "Public contracts contain rate multipliers"
Require $contracts 'PublicMaintenanceStatus' `
    "Public contracts contain maintenance state"
Require $contracts 'PublicCalendarEvent' `
    "Public contracts contain calendar events"
Require $contracts 'PublicOperationsSnapshot' `
    "Public contracts expose one launcher operations snapshot"

Require $page '[EnableRateLimiting("public-api")]' `
    "Operations endpoint uses the public API rate limiter"
Require $page 'MaximumOperationsBytes = 256 * 1024' `
    "Operations endpoint has a response ceiling"
Require $page 'UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow' `
    "Operations endpoint rejects unknown fields"
Require $page 'CryptographicOperations.FixedTimeEquals' `
    "Operations signatures use constant-time comparison"
Require $page 'CryptographicOperations.ZeroMemory(key)' `
    "Portal clears temporary signing key bytes"
Require $page 'Status503ServiceUnavailable' `
    "Invalid operations fail closed with service unavailable"
Forbid $page 'ConnectionString' `
    "Operations endpoint has no database connection string"
Forbid $page 'DbContext' `
    "Operations endpoint has no direct database access"

Require $client 'api/v1/public/operations' `
    "Launcher consumes the versioned operations endpoint"
Require $client 'api/v1/public/status' `
    "Launcher combines operations with channel population"
Require $client 'AllowAutoRedirect = false' `
    "Launcher rejects portal redirects"
Require $client 'CheckCertificateRevocationList = true' `
    "Launcher validates certificate revocation"
Require $client 'MaximumResponseBytes = 256 * 1024' `
    "Launcher bounds operations responses"
Require $client 'UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow' `
    "Launcher rejects unexpected operations fields"
Forbid $client 'password' `
    "Launcher operations contain no password field"
Forbid $client 'AuthorizationCode' `
    "Launcher operations contain no login ticket"

Require $window 'Interval = TimeSpan.FromSeconds(20)' `
    "Launcher refreshes operations at a bounded cadence"
Require $window 'Interval = TimeSpan.FromSeconds(1)' `
    "Countdown updates locally without polling every second"
Require $window 'Tasas:' `
    "Launcher renders active rate multipliers"
Require $window 'Canales:' `
    "Launcher renders per-channel population"
Require $window 'Próximo:' `
    "Launcher renders the next event countdown"
Require $window 'En curso:' `
    "Launcher renders active events"
Require $window 'maintenance.IsActive' `
    "Maintenance warnings override event countdowns"
Require $window 'MainWindow_OperationsClosed' `
    "Operations timers and clients are cleaned on close"

Assert-PowerShellParses $helperPath
Require $helper 'artifacts\modern-login-local\public-data' `
    "Local test helper writes only inside ignored runtime artifacts"
Require $helper 'Write-AtomicJson' `
    "Local test helper replaces calendar data atomically"
Require $helper '[DateTimeOffset]::Now' `
    "Local test helper produces timezone-aware relative dates"
Require $helper '[switch]$Maintenance' `
    "Local test helper can exercise maintenance priority"
Require $helper '[switch]$Clear' `
    "Local test helper can clear temporary operations data"
Forbid $helper 'NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64' `
    "Local test helper never reads the snapshot signing key"
Forbid $helper 'GetEnvironmentVariables' `
    "Local test helper never enumerates process secrets"

$example = Get-Content -LiteralPath $examplePath -Raw | ConvertFrom-Json
if ($null -eq $example.events -or @($example.events).Count -ne 0) {
    throw "Live operations contract failed: the committed event template must remain empty and non-misleading."
}
Write-Host "[PASS] Public event template is valid and contains no fabricated schedule"

Require $documentation 'HMAC-signed public-operations.json' `
    "Documentation explains the signed operations path"
Require $documentation 'public-events.json' `
    "Documentation explains the operator calendar"
Require $documentation 'does not contain accounts' `
    "Documentation states the privacy boundary"

Write-Host "NosGM launcher live operations contracts passed."
