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
$trustedChannel = Read-Source "Launcher/src/NosGM.Launcher/TrustedChannel.cs"
$localChannel = Read-Source "Launcher/src/NosGM.Launcher/LocalDevelopmentRepairChannel.cs"
$bootstrap = Read-Source "Launcher/src/NosGM.Launcher/MainWindow.LocalRepairChannel.cs"
$contentSource = Read-Source "Launcher/src/NosGM.Updater.Core/ContentSources.cs"
$updater = Read-Source "Launcher/src/NosGM.Updater.Core/TransactionalUpdater.cs"
$portalEndpoints = Read-Source "Web/src/NosGM.Web/LocalUpdateEndpoints.cs"
$portalProgram = Read-Source "Web/src/NosGM.Web/Program.cs"
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

Require $trustedChannel 'UsesPlaceholderConfiguration' `
    "Local fallback is considered only for placeholder source builds"
Require $trustedChannel 'allowLoopbackHttp: true' `
    "Local fallback explicitly opts into loopback HTTP"
Require $trustedChannel 'allowLoopbackHttp: false' `
    "Compiled production channels remain HTTPS-only"
Require $trustedChannel 'LocalDevelopmentRepairChannel.TryReadConfiguration' `
    "Placeholder builds resolve only the bounded local channel file"
Require $trustedChannel 'IsLocalDevelopmentChannel' `
    "Launcher can distinguish the local development channel"
Forbid $trustedChannel 'Environment.GetEnvironmentVariable' `
    "Trusted update roots cannot be redirected through environment variables"

Require $localChannel 'ECCurve.NamedCurves.nistP256' `
    "Local manifests use ECDSA P-256"
Require $localChannel 'ManifestSecurity.Sign' `
    "Local manifests are signed before publication"
Require $localChannel 'ManifestIO.WriteAsync' `
    "Local manifests use validated atomic serialization"
Require $localChannel 'JsonSupport.WriteAtomicAsync' `
    "Local public-key configuration is written atomically"
Require $localChannel 'SafePaths.ResolveManagedPath' `
    "Local repair content remains path-sandboxed"
Require $localChannel 'FileOptions.WriteThrough' `
    "Local repair snapshot is flushed before activation"
Require $localChannel 'PRIVATE KEY' `
    "Local configuration rejects private-key material"
Require $localChannel 'ManifestRoute = "/local-update/release-manifest.json"' `
    "Local manifest route is fixed"
Require $localChannel 'ContentRoute = "/local-update/content/"' `
    "Local content route is fixed"
Require $localChannel 'candidate.IsLoopback' `
    "Local bootstrap accepts only a loopback portal"
Require $localChannel 'TryReadConfiguration(out _)' `
    "Existing signed local channels are reused instead of silently replaced"
Forbid $localChannel 'File.WriteAllText(privateKey' `
    "Ephemeral private signing keys are never written to disk"
Forbid $localChannel 'AccountName' `
    "Local channel bootstrap never reads account names"
Forbid $localChannel 'Password' `
    "Local channel bootstrap never reads passwords"

Require $bootstrap 'LocalDevelopmentRepairChannel.EnsureAsync' `
    "Launcher prepares the source-build channel before diagnostics"
Require $bootstrap '_languageSelectionReady' `
    "Local bootstrap waits for validated launcher settings"
Require $bootstrap '_lifetime.Token' `
    "Launcher shutdown cancels local channel preparation"

Require $contentSource 'baseUri.IsLoopback' `
    "HTTP content is accepted only for loopback"
Require $contentSource 'Uri.UriSchemeHttps' `
    "Remote content still requires HTTPS"
Require $contentSource 'AllowAutoRedirect = false' `
    "Repair downloads reject redirects"
Require $contentSource 'UseCookies = false' `
    "Repair downloads store no cookies"
Require $contentSource 'EnsureSameOriginAndBasePath' `
    "Repair files cannot escape the trusted origin or base path"

Require $portalEndpoints 'app.Environment.IsDevelopment()' `
    "Local repair endpoints are absent outside Development"
Require $portalEndpoints 'IPAddress.IsLoopback(remoteAddress)' `
    "Local repair endpoints reject remote clients"
Require $portalEndpoints 'IPAddress.IsLoopback(localAddress)' `
    "Local repair endpoints require a loopback listener"
Require $portalEndpoints 'relativePath.Contains(' `
    "Local content route rejects unsafe path characters"
Require $portalEndpoints 'Path.GetFullPath' `
    "Local content paths are canonicalized"
Require $portalEndpoints 'FileAttributes.ReparsePoint' `
    "Local portal rejects reparse points"
Require $portalEndpoints 'no-store,private' `
    "Local repair responses are not cached"
Require $portalProgram 'app.MapLocalUpdateEndpoints();' `
    "Development portal maps the local signed channel"

Require $documentation 'does not delete the complete NosTale installation' `
    "Documentation rejects destructive full-folder repair"
Require $documentation 'latest 25 entries' `
    "Documentation explains bounded repair history"
Require $documentation 'does not contain the account name' `
    "Documentation states repair history privacy"
Require $documentation 'automatically runs its checks again' `
    "Documentation explains post-repair verification"
Require $documentation 'source-built launcher' `
    "Documentation explains the local development channel"
Require $documentation 'private key exists only in memory' `
    "Documentation states the ephemeral signing-key boundary"

Write-Host "NosGM launcher smart repair security, privacy and lifecycle contracts passed."
