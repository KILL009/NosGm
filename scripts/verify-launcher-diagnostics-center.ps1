param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required launcher diagnostics file was not found: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Require([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Launcher diagnostics contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

function Forbid([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Launcher diagnostics contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

$service = Read-Source "Launcher/src/NosGM.Launcher/LauncherDiagnostics.cs"
$window = Read-Source "Launcher/src/NosGM.Launcher/LauncherDiagnosticsWindow.cs"
$integration = Read-Source "Launcher/src/NosGM.Launcher/MainWindow.Diagnostics.cs"
$documentation = Read-Source "docs/launcher-diagnostics-center.md"

Require $service 'MaximumPortalResponseBytes = 256 * 1024' `
    "Portal diagnostic responses are bounded"
Require $service 'AllowAutoRedirect = false' `
    "Diagnostics reject portal redirects"
Require $service 'UseCookies = false' `
    "Diagnostics do not retain portal cookies"
Require $service 'CheckCertificateRevocationList = true' `
    "Diagnostics validate certificate revocation"
Require $service 'SafePaths.ResolveManagedPath' `
    "Game executable checks preserve updater path safety"
Require $service 'FileOptions.DeleteOnClose' `
    "Write-permission probes clean themselves"
Require $service 'LowDiskWarningBytes' `
    "Low disk space is reported"
Require $service 'CriticalDiskBytes' `
    "Critical disk space is reported"
Require $service 'api/v1/public/status' `
    "Public portal status is checked without authentication"
Require $service '4545' `
    "Master TCP reachability is checked"
Require $service '1337' `
    "World TCP reachability is checked"
Require $service '4005' `
    "Spanish Login TCP reachability is checked"
Require $service 'ZipFile.CreateFromDirectory' `
    "Support evidence is exported as a ZIP"
Require $service 'BuildSafeSettingsSummary' `
    "Support bundle uses a dedicated safe settings summary"
Require $service 'SHA256.HashDataAsync' `
    "Client fingerprint uses SHA-256"
Require $service 'C:\\Users\\<user>' `
    "Windows profile paths are sanitized"
Require $service 'intentionally excludes account names' `
    "Support bundle documents its privacy boundary"
Require $service 'settings.DiscordRichPresenceEnabled' `
    "Discord diagnostics include only preference state"
Forbid $service 'settings.AccountName' `
    "Diagnostics never read the saved account name"
Forbid $service 'Environment.GetEnvironmentVariables' `
    "Diagnostics never dump process environment variables"
Forbid $service 'ProcessStartInfo.Environment' `
    "Diagnostics never inspect child-process environment blocks"
Forbid $service 'AuthorizationCode' `
    "Diagnostics contain no authorization-code field"
Forbid $service 'LauncherCredentials' `
    "Diagnostics contain no launcher credential object"

Require $window 'Centro de diagnóstico de NosGM' `
    "Launcher exposes a dedicated diagnostics window"
Require $window 'Exportar ZIP para soporte' `
    "Diagnostics can export a support bundle"
Require $window 'Privacidad: datos sensibles excluidos' `
    "Privacy boundary is visible before export"
Require $window 'MessageBoxButton.YesNo' `
    "Opening the exported bundle folder requires an explicit choice"
Require $window '_lifetime.Cancel()' `
    "Closing diagnostics cancels active work"

Require $integration 'supportButton.Click -= OpenExternalLink_Click' `
    "The former support link is safely replaced"
Require $integration 'supportButton.Click += OpenDiagnosticsCenter_Click' `
    "Support button opens the native diagnostics center"
Require $integration '🛠 Diagnóstico' `
    "Footer clearly names the diagnostics action"
Require $integration 'window.ShowDialog()' `
    "Only one modal diagnostics workflow opens per click"

Require $documentation 'never includes the saved account name' `
    "Documentation states account-name exclusion"
Require $documentation 'does not call the ticket endpoint' `
    "Documentation states authentication safety"
Require $documentation 'SHA-256' `
    "Documentation explains client fingerprinting"

Write-Host "NosGM launcher diagnostics security, privacy and lifecycle contracts passed."
