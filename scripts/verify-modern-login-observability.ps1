param(
    [string]$BridgePath = "Data/NosGm.Program/NosGm.Master.Server/LauncherAuthBridge.cs",
    [string]$StartScriptPath = "scripts/start-modern-login-local.ps1",
    [string]$ReadinessScriptPath = "scripts/test-modern-login-readiness.ps1",
    [string]$CollectorScriptPath = "scripts/collect-modern-login-diagnostics.ps1",
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
Require $collector 'ExecutableFile = Split-Path -Leaf' 'Runtime state must omit complete executable paths.'
Require $collector 'CredentialScanPassed' 'The bundle must report whether launcher settings passed the credential scan.'
Require $collector 'Compress-Archive' 'Diagnostics must produce a portable ZIP bundle.'
Require $collector 'The collector never reads process environment blocks' 'The bundle manifest must state the process-environment boundary.'
Forbid $collector 'GetEnvironmentVariables(' 'The collector must never enumerate environment variables.'
Forbid $collector 'Win32_Process' 'The collector must not query process command lines or environment blocks through WMI.'
Forbid $collector 'Copy-Item -LiteralPath $settingsPath' 'The complete launcher settings file must never be copied into diagnostics.'
Forbid $collector 'Get-ItemProperty -Path "HKCU:\Software\Gameforge4d\TNTClient\MainApp"' 'The complete Gameforge registry key must never be exported.'

Require $runbook './scripts/test-modern-login-readiness.ps1' 'The local runbook must expose readiness inspection.'
Require $runbook './scripts/collect-modern-login-diagnostics.ps1' 'The local runbook must expose sanitized diagnostics.'
Require $acceptance '## Failure map' 'The acceptance guide must map visible symptoms to stages.'
Require $acceptance './scripts/test-modern-login-readiness.ps1 -RequireLauncher' 'The acceptance guide must require readiness before the client test.'
Require $acceptance './scripts/collect-modern-login-diagnostics.ps1' 'The acceptance guide must explain evidence collection.'
Require $acceptance 'The collector never reads the environment blocks of running processes.' 'The acceptance guide must document the strongest privacy boundary.'

Write-Host 'Modern Login loopback health, readiness inspection, sanitized evidence and acceptance-test contracts verified.'
