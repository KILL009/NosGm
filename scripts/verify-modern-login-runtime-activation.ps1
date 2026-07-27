param(
    [string]$ConfigurationPath = "Data/NosGm.Configuration/ServerConfiguration.cs",
    [string]$LauncherSettingsPath = "Launcher/src/NosGM.Launcher/LauncherSettings.cs",
    [string]$StartScriptPath = "scripts/start-modern-login-local.ps1",
    [string]$ReadinessPath = "scripts/test-modern-login-readiness.ps1",
    [string]$StopScriptPath = "scripts/stop-modern-login-local.ps1",
    [string]$DocumentationPath = "docs/modern-login-local-runbook.md"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Required([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing modern Login runtime activation file: $Path"
    }
    return Get-Content -LiteralPath $Path -Raw
}

function Require([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Modern Login runtime activation contract failed: $Description"
    }
}

function Forbid([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Modern Login runtime activation contract failed: $Description"
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

$configuration = Read-Required $ConfigurationPath
$launcherSettings = Read-Required $LauncherSettingsPath
$startScript = Read-Required $StartScriptPath
$readinessScript = Read-Required $ReadinessPath
$stopScript = Read-Required $StopScriptPath
$documentation = Read-Required $DocumentationPath

Assert-PowerShellParses $StartScriptPath
Assert-PowerShellParses $ReadinessPath
Assert-PowerShellParses $StopScriptPath

Require $configuration 'static ServerConfiguration()' 'Environment overrides must run before any process reads static configuration.'
Require $configuration 'ApplyEnvironmentOverrides();' 'The static configuration initializer does not apply environment overrides.'
Require $configuration 'Interlocked.Exchange(ref _environmentOverridesApplied, 1)' 'Environment application must be idempotent.'
Require $configuration 'NOSGM_MASTER_AUTH_KEY' 'Master service authentication cannot be overridden securely.'
Require $configuration 'NOSGM_AUTH_SERVICE_KEY' 'World authentication cannot be overridden securely.'
Require $configuration 'NOSGM_GAMEFORGE_TICKET_ISSUER_KEY' 'Ticket issuer authentication cannot be overridden securely.'
Require $configuration 'NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY' 'Login ticket consumer authentication cannot be overridden securely.'
Require $configuration 'NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN' 'Modern Login cannot be enabled externally.'
Require $configuration 'NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE' 'The Launcher AuthBridge cannot be enabled externally.'
Require $configuration 'NOSGM_START_ALL_REGIONAL_LOGIN_PORTS' 'Regional Login listeners cannot be selected externally.'
Require $configuration 'MinimumSecretLength = 32' 'Modern Login secrets must have a minimum strength contract.'
Require $configuration 'var seen = new HashSet<string>(StringComparer.Ordinal);' 'Modern Login secrets must be checked for duplicate values.'
Require $configuration 'EnableLauncherAuthBridge && !EnableGameforgeTokenLogin' 'The bridge must not start without modern Login.'
Require $configuration 'uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback' 'Plain HTTP must remain loopback-only.'
Require $configuration 'uri.AbsolutePath != "/"' 'The HttpListener environment override must accept only a listener root.'

Require $launcherSettings 'AuthenticationEndpointEnvironmentVariable = "NOSGM_AUTH_ENDPOINT"' 'Launcher settings must read the runtime endpoint explicitly.'
Require $launcherSettings 'EnvironmentVariableTarget.Process' 'The launcher endpoint override must remain process-scoped.'
Require $launcherSettings 'AuthenticationEndpoint = runtimeEndpoint' 'The runtime endpoint must override an existing settings file.'
Require $launcherSettings 'AuthenticationEndpoint = _persistedAuthenticationEndpoint' 'Saving settings must restore the persisted endpoint instead of writing the runtime value.'
Forbid $launcherSettings 'Environment.GetEnvironmentVariable("NOSGM_AUTH_ENDPOINT") ?? string.Empty' 'The endpoint must not depend only on a record property initializer.'

foreach ($variableName in @(
    'NOSGM_MASTER_AUTH_KEY',
    'NOSGM_AUTH_SERVICE_KEY',
    'NOSGM_GAMEFORGE_TICKET_ISSUER_KEY',
    'NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY',
    'NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN',
    'NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE',
    'NOSGM_LAUNCHER_AUTH_BRIDGE_PREFIX',
    'NOSGM_AUTH_ENDPOINT'
)) {
    Require $startScript $variableName "The local startup script does not configure $variableName."
}

Require $startScript 'RandomNumberGenerator]::Create()' 'Local secrets must come from a cryptographic random generator.'
Require $startScript '[Array]::Clear($bytes, 0, $bytes.Length)' 'Temporary random secret bytes must be cleared.'
Require $startScript 'Restore-ProcessEnvironment' 'The startup script must remove temporary secrets from its own shell.'
Require $startScript 'Secrets were inherited by the child processes' 'The startup script must explain the in-memory secret boundary.'
Require $startScript 'AuthenticationEndpoint = $launcherEndpoint' 'The runtime state should record only the non-secret endpoint.'
Require $startScript 'Processes = $startedProcesses.ToArray()' 'The process list must serialize safely on Windows PowerShell 5.1.'
Require $startScript 'artifacts\modern-login-local' 'Runtime state must be stored in an already ignored artifacts directory.'
Require $startScript '$nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue' 'NuGet CLI detection must be optional.'
Require $startScript 'if ($nuget)' 'The startup script must prefer NuGet CLI when it is available.'
Require $startScript 'nuget.exe not found; restoring packages.config with MSBuild' 'The startup script must explain the MSBuild restore fallback.'
Require $startScript '/p:RestorePackagesConfig=true' 'MSBuild fallback must restore legacy packages.config dependencies.'
Require $startScript '$solutionPath = Join-Path $root "NosGm.sln"' 'Restore and build must share one resolved solution path.'
Require $startScript '$loginExecutable = Join-Path $root "bin\Release\Login\NosGm.Login.exe"' 'The startup script must use the Release|AnyCPU Login output path.'
Require $startScript '$requiredExecutables = @(' 'All required binaries must be preflighted before startup.'
Require $startScript 'Missing $($requiredExecutable.Name) executable after build' 'Preflight failures must identify the missing component and path.'
Forbid $startScript 'Processes = @($startedProcesses)' 'Generic process lists must not use the incompatible array subexpression.'
Forbid $startScript 'Data\NosGm.Program\NosGm.Login\bin\Release\NosGm.Login.exe' 'The obsolete project-local Login output path must not return.'
Forbid $startScript 'throw "nuget.exe was not found.' 'Missing NuGet CLI must not stop a machine with compatible MSBuild.'
Forbid $startScript 'SetEnvironmentVariable($name, $previousEnvironment[$name], "User")' 'Secrets must never be written to the user environment.'
Forbid $startScript 'SetEnvironmentVariable($name, $previousEnvironment[$name], "Machine")' 'Secrets must never be written to the machine environment.'
Forbid $startScript 'ConvertTo-SecureString -AsPlainText' 'The startup script must not create a misleading persisted password wrapper.'

Require $readinessScript 'Checks = $checks.ToArray()' 'The readiness report must serialize safely on Windows PowerShell 5.1.'
Forbid $readinessScript 'Checks = @($checks)' 'Generic readiness lists must not use the incompatible array subexpression.'

$compatibilityList = New-Object System.Collections.Generic.List[object]
$compatibilityList.Add([pscustomobject]@{ Name = 'Compatibility' })
$compatibilityObject = [pscustomobject]@{ Items = $compatibilityList.ToArray() }
if (@($compatibilityObject.Items).Count -ne 1) {
    throw 'Modern Login runtime activation contract failed: generic List serialization is incompatible with this PowerShell runtime.'
}

Require $stopScript '[Array]::Reverse($records)' 'Shutdown should stop child processes in reverse startup order.'
Require $stopScript '$process.ProcessName -ne [string]$record.ProcessName' 'Shutdown must verify the recorded process identity.'
Require $stopScript '$difference -gt 2' 'Shutdown must reject PID reuse by comparing process start time.'
Require $stopScript 'Stop-Process -Id $process.Id -Force' 'Shutdown must stop only the validated PID allowlist.'

Require $documentation './scripts/start-modern-login-local.ps1' 'Documentation must expose the one-command local startup.'
Require $documentation './scripts/stop-modern-login-local.ps1' 'Documentation must expose the safe shutdown command.'
Require $documentation 'NOSGM_MASTER_AUTH_KEY' 'Documentation must list the external secret configuration.'
Require $documentation 'NuGet CLI is optional' 'Documentation must explain the MSBuild packages.config fallback.'

Write-Host 'Modern Login runtime environment, PowerShell 5.1 collection serialization, package restore fallback, executable layout, transient launcher endpoint, local startup and safe shutdown contracts verified.'
