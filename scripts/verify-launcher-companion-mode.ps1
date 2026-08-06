param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required companion mode file was not found: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Require([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Launcher companion contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

function Forbid([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Launcher companion contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

$settings = Read-Source "Launcher/src/NosGM.Launcher/LauncherSettings.cs"
$tray = Read-Source "Launcher/src/NosGM.Launcher/LauncherTrayIcon.cs"
$window = Read-Source "Launcher/src/NosGM.Launcher/LauncherCompanionSettingsWindow.cs"
$state = Read-Source "Launcher/src/NosGM.Launcher/LauncherCompanionAlertState.cs"
$integration = Read-Source "Launcher/src/NosGM.Launcher/MainWindow.Companion.cs"
$documentation = Read-Source "docs/launcher-companion-mode.md"

Require $settings 'CompanionModeEnabled' `
    "Companion mode has an explicit persisted preference"
Require $settings 'CompanionRestoreAfterGame' `
    "Launcher restore behavior is configurable"
Require $settings 'EventAlertsEnabled' `
    "Public event alerts are configurable"
Require $settings 'MaintenanceAlertsEnabled' `
    "Maintenance alerts are configurable"
Require $settings 'minutes is 5 or 10 or 15 or 30 or 60' `
    "Reminder windows are allow-listed"

Require $tray 'Shell_NotifyIconW' `
    "Tray integration uses the Windows shell API directly"
Require $tray 'NotifyDelete' `
    "Tray icon is removed during shutdown"
Require $tray 'ExitRequested' `
    "Tray menu offers explicit full exit"
Require $tray 'OpenRequested' `
    "Tray icon can restore the launcher"
Require $tray 'Info = Limit(message, 255)' `
    "Windows notification messages are bounded"
Forbid $tray 'System.Windows.Forms' `
    "No WinForms or third-party tray dependency is introduced"
Forbid $tray 'Process.Start' `
    "Tray callbacks cannot launch arbitrary processes"

Require $state 'MaximumDeliveredKeys = 200' `
    "Delivered notification history is bounded"
Require $state 'MaximumStateAge = TimeSpan.FromDays(14)' `
    "Delivered notification history expires"
Require $state 'JsonSupport.WriteAtomicAsync' `
    "Alert history is written atomically"
Require $state 'event-alert-state.json' `
    "Alert history uses a dedicated local file"
Forbid $state 'AccountName' `
    "Alert state never reads account names"
Forbid $state 'Password' `
    "Alert state never reads passwords"
Forbid $state 'AuthorizationCode' `
    "Alert state never handles authorization codes"
Forbid $state 'CharacterName' `
    "Alert state never persists character names"

Require $window 'Password' `
    "Privacy text explicitly states that passwords are excluded"
Require $window 'LauncherSettingsStore' `
    "Companion preferences remain part of validated launcher settings"
Forbid $window 'PasswordBox' `
    "Companion settings cannot collect passwords"
Forbid $window 'LauncherAuthenticationClient' `
    "Companion settings cannot authenticate"
Forbid $window 'ModernGameLauncher.LaunchAsync' `
    "Companion settings cannot start the game"

Require $integration 'LauncherLiveOperationsClient' `
    "Companion reuses the bounded public operations client"
Require $integration 'ModernGameLauncher.GameLaunched +=' `
    "Companion follows successful game process startup"
Require $integration 'process.Exited += CompanionGameProcess_Exited' `
    "Companion follows game process exit"
Require $integration 'e.Cancel = true' `
    "Closing during an active game hides instead of terminating"
Require $integration '_companionExitRequested = true' `
    "Explicit tray exit bypasses hide-on-close"
Require $integration 'LauncherCompanionAlertStateStore.WasDelivered' `
    "Duplicate public alerts are suppressed"
Require $integration 'Maximum' `
    "Event countdown text is bounded away from negative values"
Require $integration '_companionLifetime.Cancel()' `
    "Launcher shutdown cancels companion work"
Require $integration '_companionTrayIcon.Dispose()' `
    "Launcher shutdown removes native tray resources"
Forbid $integration 'LauncherAuthenticationClient' `
    "Companion never requests authentication tickets"
Forbid $integration 'AuthorizationCode' `
    "Companion never reads authorization codes"
Forbid $integration 'Password' `
    "Companion never reads passwords"
Forbid $integration 'AccountName' `
    "Companion never reads stored account names"
Forbid $integration 'Environment.GetEnvironmentVariable' `
    "Companion never reads process environment secrets"

Require $documentation 'Shell_NotifyIconW' `
    "Documentation explains the dependency-free tray boundary"
Require $documentation 'GET /api/v1/public/operations' `
    "Documentation states the public event source"
Require $documentation 'never reads or stores' `
    "Documentation states the private-data exclusion boundary"
Require $documentation 'at most 200 keys' `
    "Documentation states bounded alert history"
Require $documentation 'Salir completamente' `
    "Documentation explains explicit full exit"

Write-Host "NosGM companion mode security, privacy and lifecycle contracts passed."
