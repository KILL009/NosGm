param(
    [string]$ConfigurationPath = "Data/NosGm.Configuration/ServerConfiguration.cs",
    [string]$LauncherSettingsPath = "Launcher/src/NosGM.Launcher/LauncherSettings.cs",
    [string]$StartScriptPath = "scripts/start-modern-login-local.ps1",
    [string]$ReadinessPath = "scripts/test-modern-login-readiness.ps1",
    [string]$StopScriptPath = "scripts/stop-modern-login-local.ps1",
    [string]$DocumentationPath = "docs/modern-login-local-runbook.md",
    [string]$WindowsBuildWorkflowPath = ".github/workflows/build-windows.yml"
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
$windowsBuildWorkflow = Read-Required $WindowsBuildWorkflowPath

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
Require $launcherSettings 'AuthenticationTransportEnvironmentVariable = "NOSGM_LOGIN_TRANSPORT"' 'Launcher settings must read the runtime transport explicitly.'
Require $launcherSettings 'LoginServerAddressEnvironmentVariable = "NOSGM_LOGIN_ADDRESS"' 'Launcher settings must read the runtime Login address explicitly.'
Require $launcherSettings 'EnvironmentVariableTarget.Process' 'Launcher runtime overrides must remain process-scoped.'
Require $launcherSettings 'AuthenticationEndpoint = GetRuntimeValue(AuthenticationEndpointEnvironmentVariable) ??' 'The runtime endpoint must override an existing settings file.'
Require $launcherSettings 'AuthenticationTransport = GetRuntimeValue(AuthenticationTransportEnvironmentVariable) ??' 'The runtime transport must override an existing settings file.'
Require $launcherSettings 'LoginServerAddress = GetRuntimeValue(LoginServerAddressEnvironmentVariable) ??' 'The runtime Login address must override an existing settings file.'
Require $launcherSettings 'AuthenticationEndpoint = GetRuntimeValue(AuthenticationEndpointEnvironmentVariable) is null' 'Saving settings must distinguish a persistent endpoint from a process override.'
Require $launcherSettings ': _persistedAuthenticationEndpoint' 'Saving settings must restore the persistent endpoint instead of writing the process override.'
Require $launcherSettings 'AuthenticationTransport = GetRuntimeValue(AuthenticationTransportEnvironmentVariable) is null' 'Saving settings must distinguish a persistent transport from a process override.'
Require $launcherSettings ': _persistedAuthenticationTransport' 'Saving settings must restore the persistent transport instead of writing the process override.'
Require $launcherSettings 'LoginServerAddress = GetRuntimeValue(LoginServerAddressEnvironmentVariable) is null' 'Saving settings must distinguish a persistent Login address from a process override.'
Require $launcherSettings ': _persistedLoginServerAddress' 'Saving settings must restore the persistent Login address instead of writing the process override.'
Forbid $launcherSettings 'Environment.GetEnvironmentVariable("NOSGM_AUTH_ENDPOINT") ?? string.Empty' 'The endpoint must not depend only on a record property initializer.'
Forbid $launcherSettings 'EnvironmentVariableTarget.User' 'Launcher runtime authentication overrides must never be written to the user environment.'
Forbid $launcherSettings 'EnvironmentVariableTarget.Machine' 'Launcher runtime authentication overrides must never be written to the machine environment.'

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
Require $startScript 'function Resolve-LegacyMSBuildSdk' 'Visual Studio 2022 must resolve a compatible SDK for the legacy server solution.'
Require $startScript "'^(9\.0\.[0-9]+)\s+\[(.+)\]$'" 'The compatibility resolver must select a stable .NET 9 SDK.'
Require $startScript '$env:MSBuildSDKsPath = $legacyMSBuildSdk.SdksPath' 'Legacy MSBuild must use the selected .NET 9 SDK instead of the repository-wide .NET 10 SDK.'
Require $startScript '$env:MSBuildSDKsPath = $previousMSBuildSdksPath' 'The temporary legacy MSBuild SDK path must be restored.'
Require $startScript '$env:MSBuildEnableWorkloadResolver = "false"' 'Legacy MSBuild must not ask the repository-wide .NET 10 workload resolver to evaluate the .NET 9 bridge projects.'
Require $startScript '/p:MSBuildEnableWorkloadResolver=false' 'Legacy restore and build commands must explicitly disable the incompatible workload resolver.'
Require $startScript '$previousMSBuildEnableWorkloadResolver' 'The previous workload-resolver policy must be preserved.'
Require $startScript '$env:MSBuildEnableWorkloadResolver = $previousMSBuildEnableWorkloadResolver' 'The temporary workload-resolver policy must be restored.'
Forbid $startScript 'Set-Content -LiteralPath "global.json"' 'Local startup must never rewrite the repository SDK policy.'
$legacySdkRestoreIndex = $startScript.IndexOf(
    '$env:MSBuildSDKsPath = $previousMSBuildSdksPath',
    [StringComparison]::Ordinal)
$legacyWorkloadRestoreIndex = $startScript.IndexOf(
    '$env:MSBuildEnableWorkloadResolver = $previousMSBuildEnableWorkloadResolver',
    [StringComparison]::Ordinal)
$launcherBuildIndex = $startScript.IndexOf(
    '[BUILD] Building launcher Release',
    [StringComparison]::Ordinal)
if ($legacySdkRestoreIndex -lt 0 -or
    $legacyWorkloadRestoreIndex -lt 0 -or
    $launcherBuildIndex -le $legacySdkRestoreIndex -or
    $launcherBuildIndex -le $legacyWorkloadRestoreIndex) {
    throw 'Modern Login runtime activation contract failed: the .NET 9 MSBuild environment must be restored before the .NET 10 launcher build.'
}
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
Require $documentation '.NET 9 compatibility SDK' 'Documentation must explain the side-by-side SDK required by Visual Studio 2022.'
Require $documentation 'MSBuildEnableWorkloadResolver=false' 'Documentation must explain why workloads are disabled only for the legacy server build.'
Require $windowsBuildWorkflow 'MSBuildSDKsPath=$sdkPath' 'Windows CI must select the same .NET 9 SDK imports as local startup.'
Require $windowsBuildWorkflow 'MSBuildEnableWorkloadResolver=false' 'Windows CI must reproduce the local Visual Studio 2022 workload-resolver boundary.'
Forbid $windowsBuildWorkflow 'Set-Content -LiteralPath "global.json"' 'Windows CI must keep the repository-wide .NET 10 SDK policy while testing the legacy build.'

Write-Host 'Modern Login runtime environment, transient endpoint/transport/address overrides, PowerShell 5.1 serialization, package restore fallback, executable layout and safe shutdown contracts verified.'
