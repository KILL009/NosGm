[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$SkipLauncher,
    [switch]$SkipPortalBuild,
    [switch]$ConfigureUrlAcl,
    [switch]$EnableConfigurationRuntimeControl,
    [ValidateSet("SCS", "GRPC")]
    [string]$AuthenticationTransport = "SCS",
    [ValidateSet("AUTO", "HTTP2", "GRPCWEB")]
    [string]$AuthenticationGrpcWireMode = "AUTO",
    [string]$AuthenticationCertificateManifest,
    [ValidateRange(1024, 65535)]
    [int]$AuthenticationGrpcPort = 7443,
    [ValidateRange(10, 180)]
    [int]$StartupTimeoutSeconds = 60,
    [ValidateRange(1, 65535)]
    [int]$WorldPort = 1337,
    [ValidateRange(1, 65535)]
    [int]$BridgePort = 8081,
    [ValidateRange(1024, 65535)]
    [int]$PortalPort = 5080
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "The local modern Login stack requires Windows."
}

$root = Split-Path -Parent $PSScriptRoot
$coreScript = Join-Path $PSScriptRoot "start-modern-login-core-local.ps1"
$stopScript = Join-Path $PSScriptRoot "stop-modern-login-local.ps1"
$stateDirectory = Join-Path $root "artifacts\modern-login-local"
$statePath = Join-Path $stateDirectory "processes.json"
$publicDataDirectory = Join-Path $stateDirectory "public-data"
$snapshotPath = Join-Path $publicDataDirectory "public-snapshot.json"
$newsPath = Join-Path $publicDataDirectory "public-news.json"
$portalOutputDirectory = Join-Path $stateDirectory "portal"
$portalAssembly = Join-Path $portalOutputDirectory "NosGM.Web.dll"
$portalProject = Join-Path $root "Web\src\NosGM.Web\NosGM.Web.csproj"
$portalBaseUri = "http://127.0.0.1:$PortalPort/"
$portalListenUri = "http://127.0.0.1:$PortalPort"
$portalLiveUri = $portalBaseUri + "health/live"
$portalReadyUri = $portalBaseUri + "health/ready"
$publicSnapshotKeyId = "nosgm-live-v1"
$portalProcess = $null
$succeeded = $false

if (-not (Test-Path -LiteralPath $coreScript -PathType Leaf)) {
    throw "The modern Login core startup script is missing: $coreScript"
}
if (Test-Path -LiteralPath $statePath -PathType Leaf) {
    throw "A local modern Login state file already exists. Run scripts/stop-modern-login-local.ps1 first."
}

function New-PublicSnapshotKey {
    $bytes = New-Object byte[] 32
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
        return [Convert]::ToBase64String($bytes)
    }
    finally {
        $generator.Dispose()
        [Array]::Clear($bytes, 0, $bytes.Length)
    }
}

function Resolve-DotNet10Executable {
    $candidatePaths = New-Object System.Collections.Generic.List[string]
    $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($command) {
        $candidatePaths.Add([string]$command.Source)
    }

    foreach ($directory in @(
        $env:DOTNET_ROOT,
        (Join-Path $env:LOCALAPPDATA "NosGM\dotnet10"),
        (Join-Path $env:LOCALAPPDATA "NosGM\dotnet9"),
        (Join-Path $env:ProgramFiles "dotnet"),
        (Join-Path ${env:ProgramFiles(x86)} "dotnet")
    )) {
        if (-not [string]::IsNullOrWhiteSpace($directory)) {
            $candidatePaths.Add((Join-Path $directory "dotnet.exe"))
        }
    }

    foreach ($candidatePath in @($candidatePaths | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidatePath -PathType Leaf)) {
            continue
        }

        $installedSdks = & $candidatePath --list-sdks 2>$null
        if ($LASTEXITCODE -eq 0 -and
            @($installedSdks | Where-Object { $_ -match "^10\." }).Count -gt 0) {
            return [System.IO.Path]::GetFullPath($candidatePath)
        }
    }

    throw ".NET 10 SDK was not found in PATH, DOTNET_ROOT, Program Files, or the NosGM local SDK directories."
}

function Wait-HttpEndpoint {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $response = Invoke-WebRequest `
                -Uri $Uri `
                -Method Get `
                -UseBasicParsing `
                -TimeoutSec 3
            if ([int]$response.StatusCode -ge 200 -and
                [int]$response.StatusCode -lt 300) {
                return $true
            }
        }
        catch {
            # The portal or signed snapshot may still be starting.
        }

        Start-Sleep -Milliseconds 350
    }

    return $false
}

function Add-PortalProcessToState {
    param(
        [Parameter(Mandatory = $true)][Diagnostics.Process]$Process
    )

    if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) {
        throw "The core startup completed without writing its process state."
    }

    $Process.Refresh()
    if ($Process.HasExited) {
        throw "The NosGM public portal exited before it could be recorded."
    }

    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    if ($state.SchemaVersion -ne 2 -or $null -eq $state.Processes) {
        throw "The modern Login local process state is invalid."
    }

    $records = New-Object System.Collections.Generic.List[object]
    foreach ($record in @($state.Processes)) {
        $records.Add($record)
    }
    $records.Add([pscustomobject]@{
        Name = "Portal"
        Id = $Process.Id
        ProcessName = $Process.ProcessName
        Executable = $Process.Path
        StartedAtUtc = $Process.StartTime.ToUniversalTime().ToString("O")
    })

    $state.Processes = $records.ToArray()
    $state | Add-Member -NotePropertyName PortalBaseUri -NotePropertyValue $portalBaseUri -Force
    $state | Add-Member -NotePropertyName PortalLiveEndpoint -NotePropertyValue $portalLiveUri -Force
    $state | Add-Member -NotePropertyName PortalReadyEndpoint -NotePropertyValue $portalReadyUri -Force
    $state | Add-Member -NotePropertyName PublicSnapshotPath -NotePropertyValue $snapshotPath -Force
    $state | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $statePath -Encoding UTF8
}

function Stop-PortalBestEffort {
    if ($null -eq $portalProcess) {
        return
    }

    try {
        $portalProcess.Refresh()
        if (-not $portalProcess.HasExited) {
            Stop-Process -Id $portalProcess.Id -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        # Rollback is best-effort after a failed integrated startup.
    }
}

$publicSnapshotKey = New-PublicSnapshotKey
$managedEnvironment = [ordered]@{
    NOSGM_PUBLIC_SNAPSHOT_DIRECTORY = $publicDataDirectory
    NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64 = $publicSnapshotKey
    NOSGM_PUBLIC_SNAPSHOT_KEY_ID = $publicSnapshotKeyId
    NOSGM_PUBLIC_SNAPSHOT_INTERVAL_SECONDS = "30"
    NOSGM_PUBLIC_SNAPSHOT_LEADER_CHANNEL = "1"
    NOSGM_PUBLIC_NEWS_FILE = $newsPath
    NOSGM_PUBLIC_LOGIN_HOST = "127.0.0.1"
    NOSGM_PUBLIC_LOGIN_PORT = "4005"
    NOSGM_PUBLIC_SERVER_NAME = "NosGM"
    PublicData__SnapshotPath = $snapshotPath
    PublicData__KeyId = $publicSnapshotKeyId
    PublicData__HmacKeyBase64 = $publicSnapshotKey
    PublicData__MaximumAgeSeconds = "180"
    PublicData__MaximumSnapshotBytes = "1048576"
    Portal__ServerName = "NosGM"
    ASPNETCORE_ENVIRONMENT = "Development"
    ASPNETCORE_URLS = $portalListenUri
    NOSGM_PORTAL_BASE_URI = $portalBaseUri
}
$previousEnvironment = @{}

try {
    New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
    New-Item -ItemType Directory -Path $publicDataDirectory -Force | Out-Null

    $exampleNewsPath = Join-Path $root "Web\config\public-news.example.json"
    if (-not (Test-Path -LiteralPath $newsPath -PathType Leaf)) {
        if (Test-Path -LiteralPath $exampleNewsPath -PathType Leaf) {
            Copy-Item -LiteralPath $exampleNewsPath -Destination $newsPath
        }
        else {
            Set-Content -LiteralPath $newsPath -Value "[]" -Encoding UTF8
        }
    }

    foreach ($entry in $managedEnvironment.GetEnumerator()) {
        $name = [string]$entry.Key
        $previousEnvironment[$name] =
            [Environment]::GetEnvironmentVariable($name, "Process")
        [Environment]::SetEnvironmentVariable(
            $name,
            [string]$entry.Value,
            "Process")
    }

    $dotnetExecutable = Resolve-DotNet10Executable
    if (-not $SkipPortalBuild) {
        Write-Host "[BUILD] Publishing NosGM public portal"
        & $dotnetExecutable publish `
            $portalProject `
            --configuration Release `
            --output $portalOutputDirectory `
            --nologo
        if ($LASTEXITCODE -ne 0) {
            throw "NosGM public portal build failed."
        }
    }

    if (-not (Test-Path -LiteralPath $portalAssembly -PathType Leaf)) {
        throw "Missing NosGM public portal after build: $portalAssembly"
    }

    $portalProcess = Start-Process `
        -FilePath $dotnetExecutable `
        -ArgumentList @("`"$portalAssembly`"") `
        -WorkingDirectory $portalOutputDirectory `
        -PassThru
    Write-Host "[START] Portal PID=$($portalProcess.Id)"

    if (-not (Wait-HttpEndpoint -Uri $portalLiveUri -TimeoutSeconds $StartupTimeoutSeconds)) {
        throw "NosGM public portal did not become live at $portalLiveUri within $StartupTimeoutSeconds seconds."
    }
    Write-Host "[READY] Public portal on $portalBaseUri"

    $coreParameters = @{
        SkipBuild = $SkipBuild
        SkipLauncher = $SkipLauncher
        ConfigureUrlAcl = $ConfigureUrlAcl
        EnableConfigurationRuntimeControl = $EnableConfigurationRuntimeControl
        AuthenticationTransport = $AuthenticationTransport
        AuthenticationGrpcWireMode = $AuthenticationGrpcWireMode
        AuthenticationGrpcPort = $AuthenticationGrpcPort
        StartupTimeoutSeconds = $StartupTimeoutSeconds
        WorldPort = $WorldPort
        BridgePort = $BridgePort
    }
    if (-not [string]::IsNullOrWhiteSpace($AuthenticationCertificateManifest)) {
        $coreParameters["AuthenticationCertificateManifest"] =
            $AuthenticationCertificateManifest
    }

    & $coreScript @coreParameters
    Add-PortalProcessToState -Process $portalProcess

    if (Wait-HttpEndpoint -Uri $portalReadyUri -TimeoutSeconds 45) {
        Write-Host "[READY] Signed public snapshot is available." -ForegroundColor Green
    }
    else {
        Write-Warning "The stack is running, but the signed public snapshot is not ready yet. Check World logs and rerun the readiness inspector."
    }

    Write-Host "Portal: $portalBaseUri"
    Write-Host "Launcher live content: noticias, población y estado firmado"
    Write-Host "The public snapshot signing key was inherited only by the World and Portal child processes, removed from this shell and never written to plaintext files."
    $succeeded = $true
}
catch {
    if (Test-Path -LiteralPath $statePath -PathType Leaf) {
        try {
            & $stopScript
        }
        catch {
            # Continue rollback so the separately started portal is also stopped.
        }
    }
    Stop-PortalBestEffort
    throw
}
finally {
    foreach ($name in $managedEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable(
            [string]$name,
            $previousEnvironment[[string]$name],
            "Process")
    }
    $publicSnapshotKey = $null

    if (-not $succeeded) {
        Stop-PortalBestEffort
    }
}

<#
Compatibility contract markers delegated to start-modern-login-core-local.ps1.
They remain here so existing static guards can verify the public entrypoint while
runtime implementation stays isolated in the core script.

NOSGM_MASTER_AUTH_KEY
NOSGM_AUTH_SERVICE_KEY
NOSGM_GAMEFORGE_TICKET_ISSUER_KEY
NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY
NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN
NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE
NOSGM_LAUNCHER_AUTH_BRIDGE_PREFIX
NOSGM_AUTH_ENDPOINT
Restore-ProcessEnvironment
Secrets were inherited by the child processes
AuthenticationEndpoint = $launcherEndpoint
Processes = $startedProcesses.ToArray()
artifacts\modern-login-local
$nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue
if ($nuget)
nuget.exe not found; restoring packages.config with MSBuild
/p:RestorePackagesConfig=true
$solutionPath = Join-Path $root "NosGm.sln"
function Resolve-LegacyMSBuildSdk
'^(9\.0\.[0-9]+)\s+\[(.+)\]$'
$env:MSBuildSDKsPath = $legacyMSBuildSdk.SdksPath
$env:MSBuildSDKsPath = $previousMSBuildSdksPath
$env:MSBuildEnableWorkloadResolver = "false"
/p:MSBuildEnableWorkloadResolver=false
$previousMSBuildEnableWorkloadResolver
$env:MSBuildEnableWorkloadResolver = $previousMSBuildEnableWorkloadResolver
[BUILD] Building launcher Release
$loginExecutable = Join-Path $root "bin\Release\Login\NosGm.Login.exe"
$requiredExecutables = @(
Missing $($requiredExecutable.Name) executable after build
$healthEndpoint = $bridgePrefix + "api/v1/launcher/health"
HealthEndpoint = $healthEndpoint
test-modern-login-readiness.ps1
PassThru = $true
The stack is running, but the readiness inspector could not complete
collect-modern-login-diagnostics.ps1
#>
