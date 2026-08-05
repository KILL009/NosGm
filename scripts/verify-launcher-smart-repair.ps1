param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required smart repair file was not found: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Require([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Smart repair contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

function Forbid([string]$source, [string]$needle, [string]$description) {
    if ($source.IndexOf($needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Smart repair contract failed: $description"
    }

    Write-Host "[PASS] $description"
}

$service = Read-Source "Launcher/src/NosGM.Launcher/LauncherSmartRepair.cs"
$window = Read-Source "Launcher/src/NosGM.Launcher/LauncherDiagnosticsWindow.cs"
$controller = Read-Source "Launcher/src/NosGM.Launcher/LauncherController.cs"
$updater = Read-Source "Launcher/src/NosGM.Updater.Core/TransactionalUpdater.cs"
$documentation = Read-Source "docs/launcher-smart-repair.md"

Require $service 'TrustedChannel.IsConfigured' `
    "Smart repair requires a configured trusted release channel"
Require $service 'CheckAndApplyAsync' `
    "Smart repair delegates to the hardened launcher updater"
Require $service 'apply: true' `
    "Smart repair applies a verified update plan"
Require $service 'MaximumHistoryEntries = 25' `
    "Repair history is bounded"
Require $service 'JsonSupport.WriteAtomicAsync' `
    "Repair history is written atomically"
Require $service 'FailureType = Limit(exception.GetType().Name, 80)' `
    "Failure history stores only a bounded exception type"
Require $service 'CancellationToken.None' `
    "Optional history survives operation token disposal without blocking repair"
Require $service 'Repair must not fail only because optional local history could not be written' `
    "History failures cannot reverse a successful repair"
Forbid $service 'AccountName' `
    "Repair history never reads account names"
Forbid $service 'Password' `
    "Repair history never reads passwords"
Forbid $service 'AuthorizationCode' `
    "Repair history never reads authorization codes"
Forbid $service 'InstallRoot' `
    "Repair history does not persist installation paths"
Forbid $service 'ReleaseFile.Path' `
    "Repair history does not persist managed file names"

Require $window 'MessageBoxButton.YesNo' `
    "Managed file changes require explicit confirmation"
Require $window 'manifiesto firmado' `
    "The confirmation explains signed verification"
Require $window 'rollback automático' `
    "The confirmation explains automatic rollback"
Require $window 'LauncherSmartRepairService' `
    "Diagnostics owns a dedicated smart repair service"
Require $window 'Progress<UpdateProgress>' `
    "Repair progress is visible to the player"
Require $window 'await RunDiagnosticsAsync()' `
    "Diagnostics automatically rerun after repair"
Require $window 'SetActionButtons(false)' `
    "Conflicting actions are disabled during repair"
Require $window '_lifetime.Token' `
    "Closing diagnostics cancels repair work"
Require $window 'Verificar y reparar' `
    "The repair action is clearly named"

Require $controller 'ManifestSecurity.Verify' `
    "The delegated updater verifies signed manifests"
Require $controller 'CheckCertificateRevocationList = true' `
    "The delegated updater checks certificate revocation"
Require $updater 'TransactionRecovery.RecoverLockedAsync' `
    "Repair recovers interrupted transactions before applying"
Require $updater 'rollbackErrors' `
    "Repair retains transactional rollback behavior"
Require $updater 'InstallLock.Acquire' `
    "Repair serializes installation mutations"

Require $documentation 'does not delete the complete NosTale installation' `
    "Documentation rejects destructive full-folder repair"
Require $documentation 'latest 25 entries' `
    "Documentation explains bounded repair history"
Require $documentation 'does not contain the account name' `
    "Documentation states repair history privacy"
Require $documentation 'automatically runs its checks again' `
    "Documentation explains post-repair verification"

Write-Host "NosGM launcher smart repair security, privacy and lifecycle contracts passed."
