[CmdletBinding()]
param(
    [string]$EntryPointPath = "scripts/start-modern-login-local.ps1",
    [string]$CorePath = "scripts/start-modern-login-core-local.ps1",
    [string]$StopPath = "scripts/stop-modern-login-local.ps1",
    [string]$PublisherPath = "Data/NosGm.GameObject/Plugin/Event/Handler/Handler/RankingEvent.cs",
    [string]$PortalProgramPath = "Web/src/NosGM.Web/Program.cs",
    [string]$LauncherSettingsPath = "Launcher/src/NosGM.Launcher/LauncherSettings.cs",
    [string]$DocumentationPath = "docs/modern-login-live-portal.md"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Required([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing integrated live-stack file: $Path"
    }
    return Get-Content -LiteralPath $Path -Raw
}

function Require([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Integrated live-stack contract failed: $Description"
    }
}

function Forbid([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Integrated live-stack contract failed: $Description"
    }
}

function Assert-PowerShellParses([string]$Path) {
    $tokens = $null
    $parseErrors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile(
        (Resolve-Path -LiteralPath $Path),
        [ref]$tokens,
        [ref]$parseErrors)
    if (@($parseErrors).Count -gt 0) {
        $messages = $parseErrors | ForEach-Object {
            "$($_.Extent.StartLineNumber): $($_.Message)"
        }
        throw "PowerShell parse errors in ${Path}:`n$($messages -join "`n")"
    }
}

$entryPoint = Read-Required $EntryPointPath
$core = Read-Required $CorePath
$stop = Read-Required $StopPath
$publisher = Read-Required $PublisherPath
$portalProgram = Read-Required $PortalProgramPath
$launcherSettings = Read-Required $LauncherSettingsPath
$documentation = Read-Required $DocumentationPath

Assert-PowerShellParses $EntryPointPath
Assert-PowerShellParses $CorePath
Assert-PowerShellParses $StopPath

Require $entryPoint 'start-modern-login-core-local.ps1' 'The public entrypoint must delegate the proven server startup to the isolated core script.'
Require $entryPoint '[switch]$SkipPortalBuild' 'Local operators need a portal-build skip for repeated acceptance tests.'
Require $entryPoint '[int]$PortalPort = 5080' 'The public local portal must have an explicit bounded port.'
Require $entryPoint 'Web\src\NosGM.Web\NosGM.Web.csproj' 'The entrypoint must publish the official NosGM portal project.'
Require $entryPoint 'NosGM.Web.dll' 'The entrypoint must preflight the published portal assembly.'
Require $entryPoint 'health/live' 'Portal process startup must use a liveness gate.'
Require $entryPoint 'health/ready' 'Signed snapshot availability must use the readiness gate.'
Require $entryPoint 'NOSGM_PORTAL_BASE_URI' 'The launcher must receive the local portal endpoint through a process-scoped override.'

foreach ($variableName in @(
    'NOSGM_PUBLIC_SNAPSHOT_DIRECTORY',
    'NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64',
    'NOSGM_PUBLIC_SNAPSHOT_KEY_ID',
    'NOSGM_PUBLIC_SNAPSHOT_INTERVAL_SECONDS',
    'NOSGM_PUBLIC_SNAPSHOT_LEADER_CHANNEL',
    'NOSGM_PUBLIC_NEWS_FILE',
    'NOSGM_PUBLIC_LOGIN_HOST',
    'NOSGM_PUBLIC_LOGIN_PORT',
    'NOSGM_PUBLIC_SERVER_NAME',
    'PublicData__SnapshotPath',
    'PublicData__KeyId',
    'PublicData__HmacKeyBase64',
    'PublicData__MaximumAgeSeconds',
    'PublicData__MaximumSnapshotBytes'
)) {
    Require $entryPoint $variableName "The integrated entrypoint does not configure $variableName."
}

Require $entryPoint 'RandomNumberGenerator]::Create()' 'The snapshot signing key must be cryptographically random.'
Require $entryPoint 'New-Object byte[] 32' 'The local signing key must contain at least 256 bits.'
Require $entryPoint '[Array]::Clear($bytes, 0, $bytes.Length)' 'Temporary signing-key bytes must be cleared.'
Require $entryPoint '[Environment]::SetEnvironmentVariable(' 'The key and endpoints must be inherited through the process environment.'
Require $entryPoint '$previousEnvironment[[string]$name]' 'The parent shell environment must be restored.'
Require $entryPoint '$publicSnapshotKey = $null' 'The parent script must release its final signing-key reference.'
Forbid $entryPoint 'EnvironmentVariableTarget.User' 'Snapshot secrets must never be written to the user environment.'
Forbid $entryPoint 'EnvironmentVariableTarget.Machine' 'Snapshot secrets must never be written to the machine environment.'
Forbid $entryPoint 'PublicSnapshotKey -NoteProperty' 'The process-state JSON must not persist the signing key.'
Forbid $entryPoint 'HmacKeyBase64 -NoteProperty' 'The process-state JSON must not persist the portal HMAC key.'

$portalStartIndex = $entryPoint.IndexOf(
    '$portalProcess = Start-Process',
    [StringComparison]::Ordinal)
$coreStartIndex = $entryPoint.IndexOf(
    '& $coreScript @coreParameters',
    [StringComparison]::Ordinal)
$stateIndex = $entryPoint.IndexOf(
    'Add-PortalProcessToState -Process $portalProcess',
    [StringComparison]::Ordinal)
if ($portalStartIndex -lt 0 -or
    $coreStartIndex -le $portalStartIndex -or
    $stateIndex -le $coreStartIndex) {
    throw 'Integrated live-stack contract failed: Portal must start before the launcher stack and be recorded only after core startup succeeds.'
}

Require $entryPoint 'Name = "Portal"' 'Shutdown state must identify the portal process explicitly.'
Require $entryPoint 'PortalBaseUri' 'Runtime state must expose the non-secret public portal URI.'
Require $entryPoint 'PublicSnapshotPath' 'Runtime state must expose the non-secret snapshot path for diagnostics.'
Require $entryPoint '$state.Processes = $records.ToArray()' 'Portal process state must serialize safely on Windows PowerShell 5.1.'
Require $entryPoint '& $stopScript' 'A partial integrated startup must roll back the proven core process set.'
Require $entryPoint 'Stop-PortalBestEffort' 'A separately started portal must be included in rollback.'

Require $core 'NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN' 'The moved core script must retain modern Login activation.'
Require $core 'Start-TrackedProcess' 'The moved core script must retain tracked process startup.'
Require $core 'Processes = $startedProcesses.ToArray()' 'The moved core script must retain safe process-state serialization.'
Require $core 'test-modern-login-readiness.ps1' 'The moved core script must retain the readiness inspector.'
Require $core 'collect-modern-login-diagnostics.ps1' 'The moved core script must retain sanitized diagnostics guidance.'

Require $publisher 'NOSGM_PUBLIC_SNAPSHOT_DIRECTORY' 'World must support the private snapshot directory override.'
Require $publisher 'NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64' 'World must consume the inherited signing key.'
Require $publisher 'HMACSHA256' 'World snapshots must be authenticated with HMAC-SHA256.'
Require $publisher 'WriteAtomic' 'World must replace public snapshots atomically.'
Require $publisher 'PublicSnapshotPublisher.Start();' 'Ranking refresh must activate the snapshot publisher.'

Require $portalProgram 'MapGroup("/api/v1/public")' 'The portal must expose the versioned public API used by the launcher.'
Require $portalProgram 'AddOptions<PublicDataOptions>()' 'Portal snapshot configuration must stay strongly typed and validated.'
Require $launcherSettings 'PortalBaseUriEnvironmentVariable = "NOSGM_PORTAL_BASE_URI"' 'Launcher settings must consume the process-scoped portal URI.'
Require $launcherSettings 'EnvironmentVariableTarget.Process' 'Launcher portal overrides must stay process-scoped.'

Require $stop '[Array]::Reverse($records)' 'Shutdown must stop the appended portal and server processes in reverse order.'
Require $stop '$process.ProcessName -ne [string]$record.ProcessName' 'Shutdown must defend against PID reuse.'
Require $stop 'Stop-Process -Id $process.Id -Force' 'Shutdown must stop only validated process records.'

Require $documentation './scripts/start-modern-login-local.ps1' 'Documentation must preserve the one-command entrypoint.'
Require $documentation './scripts/stop-modern-login-local.ps1' 'Documentation must preserve safe one-command shutdown.'
Require $documentation 'NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64' 'Documentation must explain the inherited signing-key boundary.'
Require $documentation 'never written to `processes.json`' 'Documentation must state that the signing key is not persisted in runtime state.'

Write-Host 'Integrated Portal, signed public snapshot, launcher endpoint, rollback and safe shutdown contracts verified.'
