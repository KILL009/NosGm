param(
    [string]$BridgePath = "Data/NosGm.Program/NosGm.Master.Server/LauncherAuthBridge.cs",
    [string]$StartScriptPath = "scripts/start-modern-login-local.ps1",
    [string]$ReadinessScriptPath = "scripts/test-modern-login-readiness.ps1",
    [string]$CollectorScriptPath = "scripts/collect-modern-login-diagnostics.ps1",
    [string]$LoggerPath = "Data/NosGm.Core/Logger.cs",
    [string]$NetworkClientPath = "Data/NosGm.Core/Networking/NetworkClient.cs",
    [string]$ClientSessionPath = "Data/NosGm.GameObject/Networking/ClientSession.cs",
    [string]$SessionManagerPath = "Data/NosGm.GameObject/Networking/SessionManager.cs",
    [string]$EntryPointHandlerPath = "Data/NosGm.Handler/PacketHandler/CharScreen/EntryPointPacketHandler.cs",
    [string]$RunbookPath = "docs/modern-login-local-runbook.md",
    [string]$AcceptancePath = "docs/modern-login-acceptance-test.md"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Required([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing modern Login observability file: $Path"
    }
    return Get-Content -LiteralPath $Path -Raw
}

function Require([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Modern Login observability contract failed: $Description"
    }
}

function Forbid([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Modern Login observability contract failed: $Description"
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
        $messages = $parseErrors | ForEach-Object { "$($_.Extent.StartLineNumber): $($_.Message)" }
        throw "PowerShell parse errors in ${Path}:`n$($messages -join "`n")"
    }
}

$bridge = Read-Required $BridgePath
$startScript = Read-Required $StartScriptPath
$readiness = Read-Required $ReadinessScriptPath
$collector = Read-Required $CollectorScriptPath
$logger = Read-Required $LoggerPath
$networkClient = Read-Required $NetworkClientPath
$clientSession = Read-Required $ClientSessionPath
$sessionManager = Read-Required $SessionManagerPath
$entryPointHandler = Read-Required $EntryPointHandlerPath
$runbook = Read-Required $RunbookPath
$acceptance = Read-Required $AcceptancePath

Assert-PowerShellParses $StartScriptPath
Assert-PowerShellParses $ReadinessScriptPath
Assert-PowerShellParses $CollectorScriptPath

Require $bridge 'HealthPath = "/api/v1/launcher/health"' 'The AuthBridge must expose a stable local health path.'
Require $bridge 'private sealed class HealthResponse' 'The health response must have an explicit bounded contract.'
Require $bridge 'IsHealthRequest(context.Request)' 'Health requests must be routed before ticket processing.'
Require $bridge 'IsLoopbackRequest(context.Request)' 'The health endpoint must reject non-loopback callers.'
Require $bridge 'System.Net.IPAddress.IsLoopback(address)' 'Loopback validation must use the platform IP address check.'
Require $bridge 'Service = "NosGM.LauncherAuthBridge"' 'The health response must identify the expected service.'
Require $bridge 'ModernLoginEnabled = ServerConfiguration.EnableGameforgeTokenLogin' 'Health must report modern Login activation.'
Require $bridge 'BridgeEnabled = ServerConfiguration.EnableLauncherAuthBridge' 'Health must report bridge activation.'
Require $bridge 'RegionalLoginCount = ClientRegionMap.RegionCount' 'Health must report the canonical regional profile count.'
Require $bridge 'Status = ServerConfiguration.MaintenanceMode ? "maintenance" : "ready"' 'Health must distinguish maintenance from ready state.'

$healthBlockMatch = [regex]::Match(
    $bridge,
    '(?s)private sealed class HealthResponse.*?private sealed class ErrorResponse')
if (-not $healthBlockMatch.Success) {
    throw 'Modern Login observability contract failed: HealthResponse block could not be isolated.'
}
$healthBlock = $healthBlockMatch.Value
foreach ($forbiddenHealthTerm in @('Account', 'Password', 'Database', 'ConnectionString', 'Secret', 'AuthKey', 'IPAddress')) {
    Forbid $healthBlock $forbiddenHealthTerm "HealthResponse must not expose $forbiddenHealthTerm data."
}

Require $startScript '$healthEndpoint = $bridgePrefix + "api/v1/launcher/health"' 'Startup must calculate the loopback health endpoint.'
Require $startScript 'HealthEndpoint = $healthEndpoint' 'Runtime state must record only the non-secret health URL.'
Require $startScript 'test-modern-login-readiness.ps1' 'Startup must run the readiness inspector.'
Require $startScript 'PassThru = $true' 'Startup readiness must remain non-destructive when blockers are found.'
Require $startScript 'The stack is running, but the readiness inspector could not complete' 'An inspector failure must not roll back healthy server processes.'
Require $startScript 'collect-modern-login-diagnostics.ps1' 'Startup output must expose the sanitized evidence command.'

Require $readiness 'Exactly one $requiredName process must be recorded.' 'Readiness must enforce one Master, World and Login process.'
Require $readiness '$process.ProcessName -ne [string]$Record.ProcessName' 'Readiness must verify process identity.'
Require $readiness '$difference -gt 2' 'Readiness must reject recycled PIDs by start time.'
Require $readiness 'Port.Master' 'Readiness must check Master connectivity.'
Require $readiness 'Port.World' 'Readiness must check World connectivity.'
Require $readiness 'Port.LoginSpanish' 'Readiness must check Spanish Login connectivity.'
Require $readiness 'Invoke-RestMethod -Uri $healthUri -Method Get -TimeoutSec 5' 'Readiness must query the local health endpoint with a timeout.'
Require $readiness '[int]$health.regionalLoginCount -ne 10' 'Readiness must enforce all ten regional profiles.'
Require $readiness 'Launcher settings contain a forbidden credential-shaped property.' 'Readiness must scan launcher settings for credentials.'
Require $readiness 'Client.Executable' 'Readiness must verify the configured authorized client.'
Require $readiness 'HKCU:\Software\Gameforge4d\TNTClient\MainApp' 'Readiness must validate the shared current-user InstallationId location.'
Require $readiness 'OverallStatus = $overallStatus' 'Readiness must write a machine-readable overall result.'
Require $readiness 'if ($PassThru)' 'Readiness must support non-terminating composition by the startup and collector scripts.'

Require $collector '[switch]$SelfTest' 'The collector must expose a behavioral redaction self-test.'
Require $collector 'Invoke-RedactionSelfTest' 'The behavioral redaction test must run through the production sanitizer.'
Require $collector 'Protect-DiagnosticLine' 'Diagnostics must sanitize every collected log line.'
Require $collector '<redacted-modern-login-packet>' 'Raw NoS0576 and NoS0577 payloads must be removed.'
Require $collector '<redacted-entry-packet>' 'Raw NsTeST entry payloads must be removed.'
Require $collector '<email>' 'Email addresses must be redacted.'
Require $collector '<guid>' 'GUID-shaped values must be redacted.'
Require $collector '<ip>' 'Non-loopback IP addresses must be redacted.'
Require $collector 'C:\Users\<user>' 'Windows profile names must be redacted.'
Require $collector '<long-value>' 'Long secret-shaped values must be redacted.'
Require $collector '[IO.File]::Open(' 'Log collection must open files with an explicit bounded reader.'
Require $collector '$bytesToRead = [int][Math]::Min($maximumBytes, $sourceLength)' 'The collector must cap bytes before reading a log.'
Require $collector '[IO.SeekOrigin]::End' 'The collector must seek from the end of large logs.'
Require $collector 'Select-Object -Last $MaxLogLines' 'The collector must cap the number of emitted log lines.'
Require $collector '[Array]::Clear($buffer, 0, $buffer.Length)' 'Temporary log byte buffers must be cleared.'
Require $collector '[long]$MaxLogMegabytes * 1MB' 'Each log read must have a byte ceiling.'
Require $collector 'Diagnostic redaction self-test leaked a synthetic private value.' 'The self-test must fail on any synthetic private-value leak.'
Require $collector 'Diagnostic redaction self-test exceeded the configured output ceiling.' 'The self-test must enforce the byte ceiling.'
Require $collector 'ExecutableFile = Split-Path -Leaf' 'Runtime state must omit complete executable paths.'
Require $collector 'CredentialScanPassed' 'The bundle must report whether launcher settings passed the credential scan.'
Require $collector 'Resolve-GitExecutable' 'The collector must resolve Git outside the 32-bit PowerShell PATH when necessary.'
Require $collector '$env:ProgramW6432' 'The collector must probe the native Program Files Git installation.'
Require $collector 'Resolve-GitHeadCommit' 'The collector must resolve HEAD without git.exe when repository metadata is present.'
Require $collector 'RepositoryDirty = $gitDirty' 'The bundle must distinguish a clean commit from locally modified source.'
Require $collector 'Get-FileFingerprint' 'The collector must fingerprint the deployed diagnostic binaries.'
Require $collector 'Get-FileHash -LiteralPath $Path -Algorithm SHA256' 'Binary fingerprints must use SHA-256.'
Require $collector '"NosGm.Core.dll"' 'The Core transport binary must be included in the fixed fingerprint allowlist.'
Require $collector '"NosGm.GameObject.dll"' 'The session binary must be included in the fixed fingerprint allowlist.'
Require $collector '"NosGm.Handler.dll"' 'The World entry handler binary must be included in the fixed fingerprint allowlist.'
Require $collector '"binary-summary.json"' 'The collector must emit a separate bounded binary summary.'
Require $collector '"binary-summary-error.json"' 'Binary fingerprint failures must remain isolated from runtime-state collection.'
Require $collector 'Compress-Archive' 'Diagnostics must produce a portable ZIP bundle.'
Require $collector 'The collector never reads process environment blocks' 'The bundle manifest must state the process-environment boundary.'
Forbid $collector 'GetEnvironmentVariables(' 'The collector must never enumerate environment variables.'
Forbid $collector 'Win32_Process' 'The collector must not query process command lines or environment blocks through WMI.'
Forbid $collector 'Copy-Item -LiteralPath $settingsPath' 'The complete launcher settings file must never be copied into diagnostics.'
Forbid $collector 'Get-ItemProperty -Path "HKCU:\Software\Gameforge4d\TNTClient\MainApp"' 'The complete Gameforge registry key must never be exported.'

Require $logger 'WorldHandshakeDiagnosticFileAppender' 'World diagnostics must use a dedicated bounded appender.'
Require $logger 'File = $"{logPrefix}-handshake.log"' 'World diagnostics must be written to a separate collectible file.'
Require $logger 'StringToMatch = "[WORLD_HANDSHAKE]"' 'The dedicated appender must accept only handshake records.'
Require $logger 'StringToMatch = "[WORLD_ENTRY]"' 'The dedicated appender must accept only entry records.'
Require $logger 'handshakeAppender.AddFilter(new DenyAllFilter())' 'Unrelated World traffic must be denied by the diagnostic appender.'
Require $logger 'ImmediateFlush = true' 'World handshake diagnostics must be durable while the process is running.'
Require $logger 'Stage=DIAGNOSTICS_READY Revision=20260728.4' 'The log must prove that the diagnostic Core binary is active.'
Require $sessionManager '[WORLD_HANDSHAKE] Stage=TCP_CONNECTED' 'World TCP acceptance must be visible in Release logs.'
Require $sessionManager '[WORLD_HANDSHAKE] Stage=TCP_DISCONNECTED' 'World disconnect state must be visible in Release logs.'
Require $networkClient '[WORLD_HANDSHAKE] Stage=INITIAL_FRAME_BUFFERED' 'Fragmented initial frames must report bounded byte counts.'
Require $networkClient '[WORLD_HANDSHAKE] Stage=INITIAL_FRAME_SPLIT' 'Initial frame splitting must report custom and tail lengths.'
Require $networkClient 'Code=INITIAL_FRAME_TOO_LARGE' 'Oversized initial frames must have a stable rejection code.'
Require $clientSession '[WORLD_HANDSHAKE] Stage=SESSION_ESTABLISHED' 'Session establishment must be visible without emitting the SessionId.'
Require $clientSession '[WORLD_HANDSHAKE] Stage=ENTRY_PACKET_WAIT_STARTED' 'Entry packet collection must expose its expected part count.'
Require $clientSession '[WORLD_HANDSHAKE] Stage=ENTRY_PACKET_ASSEMBLED' 'Entry packet assembly must be visible before dispatch.'
Require $clientSession 'Code=FRAME_TOO_SHORT' 'Silently discarded short frames must now have a stable diagnostic code.'
Require $entryPointHandler '[WORLD_ENTRY] Stage=' 'World entry decisions must use the stable structured prefix.'
Require $entryPointHandler 'RejectEntry("MALFORMED_ENTRY_PACKET")' 'Malformed entry rejection must remain observable in Release.'
Require $entryPointHandler 'RejectEntry("LOGIN_NOT_PERMITTED")' 'Master login-permission rejection must remain observable in Release.'
Require $entryPointHandler 'RejectEntry("GAMEFORGE_WORLD_PERMIT_INVALID")' 'Gameforge World-permit rejection must remain observable in Release.'
Require $entryPointHandler 'LogEntryStage("GAMEFORGE_WORLD_PERMIT_ACCEPTED")' 'Successful Gameforge World permits must be observable.'
Require $entryPointHandler 'LogEntryStage("CHARACTER_LIST_SENT"' 'Successful World entry must end with an observable character-list milestone.'
Forbid $entryPointHandler 'Logger.Debug(' 'Release diagnostics must not depend on conditional DEBUG calls.'
Forbid $entryPointHandler 'AccountId={' 'Structured World entry diagnostics must not emit account identifiers.'
Forbid $entryPointHandler 'PacketData={' 'Structured World entry diagnostics must not emit raw entry packets.'
Forbid $clientSession 'Stage=SESSION_ESTABLISHED ClientId={ClientId} SessionId=' 'Session establishment must not emit the raw SessionId.'

Require $runbook './scripts/test-modern-login-readiness.ps1' 'The local runbook must expose readiness inspection.'
Require $runbook './scripts/collect-modern-login-diagnostics.ps1' 'The local runbook must expose sanitized diagnostics.'
Require $acceptance '## Failure map' 'The acceptance guide must map visible symptoms to stages.'
Require $acceptance './scripts/test-modern-login-readiness.ps1 -RequireLauncher' 'The acceptance guide must require readiness before the client test.'
Require $acceptance './scripts/collect-modern-login-diagnostics.ps1' 'The acceptance guide must explain evidence collection.'
Require $acceptance 'The collector never reads the environment blocks of running processes.' 'The acceptance guide must document the strongest privacy boundary.'

Write-Host 'Modern Login loopback health, readiness inspection, sanitized evidence and acceptance-test contracts verified.'
