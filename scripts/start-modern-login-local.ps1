[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$SkipLauncher,
    [switch]$ConfigureUrlAcl,
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
    [int]$BridgePort = 8081
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "The local modern Login stack requires Windows."
}

$root = Split-Path -Parent $PSScriptRoot
$masterPort = 4545
$spanishLoginPort = 4005
$stateDirectory = Join-Path $root "artifacts\modern-login-local"
$statePath = Join-Path $stateDirectory "processes.json"
$startedProcesses = New-Object System.Collections.Generic.List[object]
$environmentVariableNames = @(
    "NOSGM_MASTER_AUTH_KEY",
    "NOSGM_AUTH_SERVICE_KEY",
    "NOSGM_GAMEFORGE_TICKET_ISSUER_KEY",
    "NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY",
    "NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN",
    "NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE",
    "NOSGM_START_ALL_REGIONAL_LOGIN_PORTS",
    "NOSGM_LAUNCHER_AUTH_BRIDGE_PREFIX",
    "NOSGM_GAMEFORGE_AUTH_TICKET_TTL_SECONDS",
    "NOSGM_GAMEFORGE_WORLD_PERMIT_TTL_SECONDS",
    "NOSGM_LAUNCHER_AUTH_ATTEMPT_WINDOW_SECONDS",
    "NOSGM_LAUNCHER_AUTH_MAX_ATTEMPTS_PER_WINDOW",
    "NOSGM_AUTH_ENDPOINT",
    "NOSGM_AUTH_TRANSPORT",
    "NOSGM_AUTH_GRPC_URL",
    "NOSGM_AUTH_GRPC_CLIENT_CERT_PATH",
    "NOSGM_AUTH_GRPC_CLIENT_CERT_PASSWORD",
    "NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID",
    "NOSGM_AUTH_GRPC_DEADLINE_MILLISECONDS",
    "NOSGM_AUTH_GRPC_WIRE_MODE",
    "NOSGM_AUTH_GRPC_SERVER_CERT_PATH",
    "NOSGM_AUTH_GRPC_SERVER_CERT_PASSWORD",
    "NOSGM_AUTH_GRPC_AUTHBRIDGE_CERT_SHA256",
    "NOSGM_AUTH_GRPC_LOGIN_CERT_SHA256",
    "NOSGM_AUTH_GRPC_WORLD_CERT_SHA256",
    "NOSGM_AUTH_GRPC_PORT",
    "NOSGM_AUTH_GRPC_TICKET_TTL_SECONDS",
    "NOSGM_AUTH_GRPC_PERMIT_TTL_SECONDS",
    "NOSGM_AUTH_GRPC_INSTANCE_ID",
    "DOTNET_ROOT"
)
$previousEnvironment = @{}
foreach ($name in $environmentVariableNames) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
}

function New-NosGmSecret {
    $bytes = New-Object byte[] 48
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

function Restore-ProcessEnvironment {
    foreach ($name in $environmentVariableNames) {
        [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], "Process")
    }
}

function ConvertFrom-SecureStringInMemory {
    param(
        [Parameter(Mandatory = $true)]
        [Security.SecureString]$Value
    )

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Import-LocalAuthenticationBundle {
    param([Parameter(Mandatory = $true)][string]$ManifestPath)

    $resolvedManifestPath = [System.IO.Path]::GetFullPath($ManifestPath)
    if (-not (Test-Path -LiteralPath $resolvedManifestPath -PathType Leaf)) {
        throw "The local authentication certificate manifest does not exist: $resolvedManifestPath"
    }

    $manifest =
        Get-Content -LiteralPath $resolvedManifestPath -Raw |
        ConvertFrom-Json
    if ($manifest.SchemaVersion -ne 1 -or
        $null -eq $manifest.Clients -or
        [string]::IsNullOrWhiteSpace([string]$manifest.CredentialsPath)) {
        throw "The local authentication certificate manifest is invalid."
    }

    $credentialsPath =
        [System.IO.Path]::GetFullPath([string]$manifest.CredentialsPath)
    if (-not (Test-Path -LiteralPath $credentialsPath -PathType Leaf)) {
        throw "The DPAPI-protected authentication credential bundle does not exist."
    }
    $credentials = Import-Clixml -LiteralPath $credentialsPath
    if ($credentials.SchemaVersion -ne 1) {
        throw "The DPAPI-protected authentication credential bundle is invalid."
    }

    foreach ($certificatePath in @(
        [string]$manifest.RootCertificatePath,
        [string]$manifest.ServerCertificatePath,
        [string]$manifest.Clients.AuthBridge.CertificatePath,
        [string]$manifest.Clients.Login.CertificatePath,
        [string]$manifest.Clients.World.CertificatePath
    )) {
        if (-not [System.IO.Path]::IsPathRooted($certificatePath) -or
            -not (Test-Path -LiteralPath $certificatePath -PathType Leaf)) {
            throw "The local authentication certificate bundle contains a missing or non-absolute certificate path."
        }
    }

    foreach ($fingerprint in @(
        [string]$manifest.Clients.AuthBridge.Sha256,
        [string]$manifest.Clients.Login.Sha256,
        [string]$manifest.Clients.World.Sha256
    )) {
        if ($fingerprint -notmatch '^[A-Fa-f0-9]{64}$') {
            throw "The local authentication certificate bundle contains an invalid SHA-256 fingerprint."
        }
    }

    $trustedRootPath =
        "Cert:\CurrentUser\Root\" +
        [string]$manifest.RootCertificateThumbprint
    if (-not (Test-Path -LiteralPath $trustedRootPath)) {
        throw "The NosGM local authentication root is not trusted for the current Windows user. Import '$($manifest.RootCertificatePath)' into Cert:\CurrentUser\Root first."
    }

    return [pscustomobject]@{
        ManifestPath = $resolvedManifestPath
        Manifest = $manifest
        Credentials = $credentials
    }
}

function Merge-ProcessEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [Collections.IDictionary]$Base,
        [Collections.IDictionary]$Additional = @{}
    )

    $result = @{}
    foreach ($entry in $Base.GetEnumerator()) {
        $result[[string]$entry.Key] = [string]$entry.Value
    }
    foreach ($entry in $Additional.GetEnumerator()) {
        $result[[string]$entry.Key] = [string]$entry.Value
    }
    return $result
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    try {
        $principal = New-Object Security.Principal.WindowsPrincipal($identity)
        return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    }
    finally {
        $identity.Dispose()
    }
}

function Resolve-AuthenticationGrpcWireMode {
    param([Parameter(Mandatory = $true)][string]$RequestedMode)

    $operatingSystem =
        Get-CimInstance -ClassName Win32_OperatingSystem
    $version = [Version]$operatingSystem.Version
    $isWorkstation = [int]$operatingSystem.ProductType -eq 1
    $supportsNetFrameworkHttp2 =
        ($isWorkstation -and $version.Build -ge 22000) -or
        (-not $isWorkstation -and $version.Build -ge 17763)

    if ($RequestedMode -eq "AUTO") {
        if ($supportsNetFrameworkHttp2) {
            return "HTTP2"
        }
        return "GRPCWEB"
    }

    if ($RequestedMode -eq "HTTP2" -and
        -not $supportsNetFrameworkHttp2) {
        throw "HTTP2 for the .NET Framework callers requires Windows 11 or Windows Server 2019 or later. Use AUTO or GRPCWEB on Windows 10."
    }

    return $RequestedMode
}

function Wait-TcpPort {
    param(
        [Parameter(Mandatory = $true)][string]$HostName,
        [Parameter(Mandatory = $true)][int]$Port,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $client = New-Object Net.Sockets.TcpClient
        $result = $null
        try {
            $result = $client.BeginConnect($HostName, $Port, $null, $null)
            if ($result.AsyncWaitHandle.WaitOne(500) -and $client.Connected) {
                $client.EndConnect($result)
                Write-Host "[READY] $Description on $HostName`:$Port"
                return
            }
        }
        catch {
            # The service may still be starting.
        }
        finally {
            if ($null -ne $result) {
                $result.AsyncWaitHandle.Close()
            }
            $client.Dispose()
        }
        Start-Sleep -Milliseconds 350
    }

    throw "$Description did not listen on $HostName`:$Port within $StartupTimeoutSeconds seconds."
}

function Start-TrackedProcess {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Executable,
        [string[]]$Arguments = @(),
        [string]$WorkingDirectory,
        [Collections.IDictionary]$ProcessEnvironment = @{}
    )

    if (-not (Test-Path -LiteralPath $Executable -PathType Leaf)) {
        throw "Missing $Name executable: $Executable"
    }

    if ([string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        $WorkingDirectory = Split-Path -Parent $Executable
    }

    $startParameters = @{
        FilePath = $Executable
        WorkingDirectory = $WorkingDirectory
        PassThru = $true
    }
    if ($Arguments.Count -gt 0) {
        $startParameters["ArgumentList"] = $Arguments
    }

    $temporaryEnvironment = @{}
    $process = $null
    try {
        foreach ($entry in $ProcessEnvironment.GetEnumerator()) {
            $variableName = [string]$entry.Key
            if ($environmentVariableNames -notcontains $variableName) {
                throw "Process environment variable is not allow-listed: $variableName"
            }
        }

        foreach ($variableName in $environmentVariableNames) {
            $temporaryEnvironment[$variableName] =
                [Environment]::GetEnvironmentVariable(
                    $variableName,
                    "Process")
            $processValue = $null
            if ($ProcessEnvironment.Contains($variableName)) {
                $processValue =
                    [string]$ProcessEnvironment[$variableName]
            }
            [Environment]::SetEnvironmentVariable(
                $variableName,
                $processValue,
                "Process")
        }

        $process = Start-Process @startParameters
    }
    finally {
        foreach ($variableName in $temporaryEnvironment.Keys) {
            [Environment]::SetEnvironmentVariable(
                $variableName,
                $temporaryEnvironment[$variableName],
                "Process")
        }
    }

    if ($null -eq $process) {
        throw "The $Name process could not be started."
    }
    $startedAtUtc = $process.StartTime.ToUniversalTime().ToString("O")
    $record = [pscustomobject]@{
        Name = $Name
        Id = $process.Id
        ProcessName = $process.ProcessName
        Executable = $Executable
        StartedAtUtc = $startedAtUtc
    }
    $startedProcesses.Add($record)
    Write-Host "[START] $Name PID=$($process.Id)"
    return $process
}

function Stop-StartedProcesses {
    $records = $startedProcesses.ToArray()
    [Array]::Reverse($records)
    foreach ($record in $records) {
        try {
            Stop-Process -Id $record.Id -Force -ErrorAction SilentlyContinue
        }
        catch {
            # Best-effort rollback after failed startup.
        }
    }
}

function Resolve-DotNet10Executable {
    $candidatePaths =
        New-Object System.Collections.Generic.List[string]
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

function Resolve-LegacyMSBuildSdk {
    $candidatePaths =
        New-Object System.Collections.Generic.List[string]
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

    $compatibleSdks =
        New-Object System.Collections.Generic.List[object]
    foreach ($candidatePath in @($candidatePaths | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidatePath -PathType Leaf)) {
            continue
        }

        $installedSdks = & $candidatePath --list-sdks 2>$null
        if ($LASTEXITCODE -ne 0) {
            continue
        }

        foreach ($installedSdk in $installedSdks) {
            if ($installedSdk -notmatch '^(9\.0\.[0-9]+)\s+\[(.+)\]$') {
                continue
            }

            $sdkVersion = [Version]$Matches[1]
            $sdkBase = $Matches[2]
            $sdkDirectory =
                Join-Path (Join-Path $sdkBase $sdkVersion.ToString()) "Sdks"
            if (-not (Test-Path `
                    -LiteralPath (Join-Path $sdkDirectory "Microsoft.NET.Sdk\Sdk") `
                    -PathType Container)) {
                continue
            }

            $compatibleSdks.Add([pscustomobject]@{
                Version = $sdkVersion
                SdksPath = [System.IO.Path]::GetFullPath($sdkDirectory)
                DotNetExecutable =
                    [System.IO.Path]::GetFullPath($candidatePath)
            })
        }
    }

    $selectedSdk =
        $compatibleSdks |
        Sort-Object -Property Version -Descending |
        Select-Object -First 1
    if ($null -ne $selectedSdk) {
        return $selectedSdk
    }

    throw ".NET 9 compatibility SDK was not found. Visual Studio 2022 MSBuild 17.x cannot load the .NET 10 SDK. Install it with 'winget install --id Microsoft.DotNet.SDK.9 --exact --source winget' and open a new PowerShell window."
}

function Resolve-MSBuild {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
    $vswhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere) {
        $resolved = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
        if ($resolved) {
            return $resolved
        }
    }

    throw "MSBuild was not found. Install Visual Studio Build Tools 2022 or run with -SkipBuild after building the solution."
}

if (Test-Path -LiteralPath $statePath) {
    throw "A local modern Login state file already exists. Run scripts/stop-modern-login-local.ps1 first."
}

$bridgePrefix = "http://127.0.0.1:$BridgePort/"
$launcherEndpoint = $bridgePrefix + "api/v1/launcher/ticket"
$healthEndpoint = $bridgePrefix + "api/v1/launcher/health"
$authenticationGrpcEndpoint =
    "https://127.0.0.1:$AuthenticationGrpcPort"
$authenticationBundle = $null
$resolvedAuthenticationGrpcWireMode = $null

if ($AuthenticationTransport -eq "GRPC") {
    $resolvedAuthenticationGrpcWireMode =
        Resolve-AuthenticationGrpcWireMode `
            -RequestedMode $AuthenticationGrpcWireMode
    if ([string]::IsNullOrWhiteSpace(
            $AuthenticationCertificateManifest)) {
        $AuthenticationCertificateManifest = Join-Path `
            $root `
            "artifacts\authentication-grpc-local\manifest.json"
    }

    $authenticationBundle = Import-LocalAuthenticationBundle `
        -ManifestPath $AuthenticationCertificateManifest
}
elseif ($AuthenticationGrpcWireMode -ne "AUTO") {
    throw "-AuthenticationGrpcWireMode applies only when -AuthenticationTransport GRPC is selected."
}

$dotnetExecutable = $null
$dotnetRoot = $null
if (-not $SkipBuild -or
    -not $SkipLauncher -or
    $AuthenticationTransport -eq "GRPC") {
    $dotnetExecutable = Resolve-DotNet10Executable
    $dotnetRoot = Split-Path -Parent $dotnetExecutable
}

if ($ConfigureUrlAcl) {
    if (-not (Test-IsAdministrator)) {
        throw "-ConfigureUrlAcl requires an elevated PowerShell window."
    }

    & netsh http delete urlacl url=$bridgePrefix *> $null
    & netsh http add urlacl url=$bridgePrefix user="$env:USERDOMAIN\$env:USERNAME" | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to configure the HttpListener URL ACL for $bridgePrefix"
    }
}

if (-not $SkipBuild) {
    $previousBuildDotNetRoot = $env:DOTNET_ROOT
    try {
        $env:DOTNET_ROOT = $dotnetRoot
        $previousMSBuildSdksPath = $env:MSBuildSDKsPath
        $previousMSBuildEnableWorkloadResolver =
            $env:MSBuildEnableWorkloadResolver
        try {
            $legacyMSBuildSdk = Resolve-LegacyMSBuildSdk
            $env:MSBuildSDKsPath = $legacyMSBuildSdk.SdksPath
            $env:MSBuildEnableWorkloadResolver = "false"
            Write-Host (
                "[BUILD] Visual Studio 2022 compatibility SDK: .NET " +
                $legacyMSBuildSdk.Version +
                " | " +
                $legacyMSBuildSdk.SdksPath)
            $msbuild = Resolve-MSBuild
            $solutionPath = Join-Path $root "NosGm.sln"
            $nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue

            if ($nuget) {
                Write-Host "[BUILD] Restoring NosGm.sln with NuGet CLI"
                & $nuget.Source restore $solutionPath -NonInteractive
                if ($LASTEXITCODE -ne 0) {
                    throw "NuGet restore failed."
                }
            }
            else {
                Write-Host "[BUILD] nuget.exe not found; restoring packages.config with MSBuild"
                & $msbuild $solutionPath /t:Restore /m /nologo /nr:false /v:minimal /p:RestorePackagesConfig=true /p:MSBuildEnableWorkloadResolver=false /p:NosGmLegacyBuild=true /p:Configuration=Release "/p:Platform=Any CPU"
                if ($LASTEXITCODE -ne 0) {
                    throw "MSBuild package restore failed. Install Visual Studio Build Tools 2022 with NuGet targets, or install NuGet CLI."
                }
            }

            Write-Host "[BUILD] Building server Release / Any CPU"
            & $msbuild $solutionPath /t:Build /m /nologo /nr:false /v:minimal /p:MSBuildEnableWorkloadResolver=false /p:NosGmLegacyBuild=true /p:Configuration=Release "/p:Platform=Any CPU"
            if ($LASTEXITCODE -ne 0) {
                throw "Server build failed."
            }
        }
        finally {
            $env:MSBuildEnableWorkloadResolver = $previousMSBuildEnableWorkloadResolver
            $env:MSBuildSDKsPath = $previousMSBuildSdksPath
        }

        Write-Host "[BUILD] Building launcher Release"
        & $dotnetExecutable build `
            (Join-Path $root "Launcher\NosGM.Launcher.sln") `
            --configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Launcher build failed."
        }

        if ($AuthenticationTransport -eq "GRPC") {
            $authenticationProject = Join-Path `
                $root `
                "Data\NosGm.Program\NosGm.Authentication.Server\NosGm.Authentication.Server.csproj"
            $authenticationOutput =
                Join-Path $root "bin\Release\Authentication"
            Write-Host "[BUILD] Publishing the .NET 10 authentication runtime"
            & $dotnetExecutable publish `
                $authenticationProject `
                --configuration Release `
                --output $authenticationOutput `
                --nologo
            if ($LASTEXITCODE -ne 0) {
                throw "Authentication runtime build failed."
            }
        }
    }
    finally {
        $env:DOTNET_ROOT = $previousBuildDotNetRoot
    }
}

$masterExecutable = Join-Path $root "bin\Release\Master\NosGm.Master.Server.exe"
$worldExecutable = Join-Path $root "bin\Release\World\NosGm.World.exe"
$loginExecutable = Join-Path $root "bin\Release\Login\NosGm.Login.exe"
$launcherExecutable = Join-Path $root "Launcher\src\NosGM.Launcher\bin\Release\net10.0-windows\NosGM.Launcher.exe"
$authenticationDirectory =
    Join-Path `
        $root `
        "bin\Release\Authentication"
$authenticationAssembly =
    Join-Path `
        $authenticationDirectory `
        "NosGm.Authentication.Server.dll"

$requiredExecutables = @(
    [pscustomobject]@{ Name = "Master"; Path = $masterExecutable },
    [pscustomobject]@{ Name = "World"; Path = $worldExecutable },
    [pscustomobject]@{ Name = "Login"; Path = $loginExecutable }
)
if (-not $SkipLauncher) {
    $requiredExecutables += [pscustomobject]@{ Name = "Launcher"; Path = $launcherExecutable }
}
if ($AuthenticationTransport -eq "GRPC") {
    $requiredExecutables += [pscustomobject]@{
        Name = "Authentication gRPC runtime"
        Path = $authenticationAssembly
    }
}

foreach ($requiredExecutable in $requiredExecutables) {
    if (-not (Test-Path -LiteralPath $requiredExecutable.Path -PathType Leaf)) {
        throw "Missing $($requiredExecutable.Name) executable after build: $($requiredExecutable.Path)"
    }
}

$sharedServerEnvironment = @{
    NOSGM_MASTER_AUTH_KEY = New-NosGmSecret
    NOSGM_AUTH_SERVICE_KEY = New-NosGmSecret
    NOSGM_GAMEFORGE_TICKET_ISSUER_KEY = New-NosGmSecret
    NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY = New-NosGmSecret
    NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN = "true"
    NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE = "true"
    NOSGM_START_ALL_REGIONAL_LOGIN_PORTS = "true"
    NOSGM_LAUNCHER_AUTH_BRIDGE_PREFIX = $bridgePrefix
    NOSGM_GAMEFORGE_AUTH_TICKET_TTL_SECONDS = "120"
    NOSGM_GAMEFORGE_WORLD_PERMIT_TTL_SECONDS = "120"
    NOSGM_LAUNCHER_AUTH_ATTEMPT_WINDOW_SECONDS = "60"
    NOSGM_LAUNCHER_AUTH_MAX_ATTEMPTS_PER_WINDOW = "10"
    NOSGM_AUTH_TRANSPORT = $AuthenticationTransport
}
$masterEnvironment = $sharedServerEnvironment
$worldEnvironment = $sharedServerEnvironment
$loginEnvironment = $sharedServerEnvironment
$authenticationRuntimeEnvironment = @{}

if ($AuthenticationTransport -eq "GRPC") {
    $manifest = $authenticationBundle.Manifest
    $credentials = $authenticationBundle.Credentials

    $authenticationRuntimeEnvironment = @{
        NOSGM_AUTH_GRPC_SERVER_CERT_PATH =
            [string]$manifest.ServerCertificatePath
        NOSGM_AUTH_GRPC_SERVER_CERT_PASSWORD =
            ConvertFrom-SecureStringInMemory $credentials.Server
        NOSGM_AUTH_GRPC_AUTHBRIDGE_CERT_SHA256 =
            [string]$manifest.Clients.AuthBridge.Sha256
        NOSGM_AUTH_GRPC_LOGIN_CERT_SHA256 =
            [string]$manifest.Clients.Login.Sha256
        NOSGM_AUTH_GRPC_WORLD_CERT_SHA256 =
            [string]$manifest.Clients.World.Sha256
        NOSGM_AUTH_GRPC_PORT = $AuthenticationGrpcPort.ToString()
        NOSGM_AUTH_GRPC_TICKET_TTL_SECONDS = "120"
        NOSGM_AUTH_GRPC_PERMIT_TTL_SECONDS = "120"
        NOSGM_AUTH_GRPC_INSTANCE_ID = "authentication-local-1"
    }

    $masterEnvironment = Merge-ProcessEnvironment `
        -Base $sharedServerEnvironment `
        -Additional @{
            NOSGM_AUTH_GRPC_URL = $authenticationGrpcEndpoint
            NOSGM_AUTH_GRPC_CLIENT_CERT_PATH =
                [string]$manifest.Clients.AuthBridge.CertificatePath
            NOSGM_AUTH_GRPC_CLIENT_CERT_PASSWORD =
                ConvertFrom-SecureStringInMemory $credentials.AuthBridge
            NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID = "authbridge-local-1"
            NOSGM_AUTH_GRPC_DEADLINE_MILLISECONDS = "10000"
            NOSGM_AUTH_GRPC_WIRE_MODE =
                $resolvedAuthenticationGrpcWireMode
        }
    $loginEnvironment = Merge-ProcessEnvironment `
        -Base $sharedServerEnvironment `
        -Additional @{
            NOSGM_AUTH_GRPC_URL = $authenticationGrpcEndpoint
            NOSGM_AUTH_GRPC_CLIENT_CERT_PATH =
                [string]$manifest.Clients.Login.CertificatePath
            NOSGM_AUTH_GRPC_CLIENT_CERT_PASSWORD =
                ConvertFrom-SecureStringInMemory $credentials.Login
            NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID = "login-local-1"
            NOSGM_AUTH_GRPC_DEADLINE_MILLISECONDS = "10000"
            NOSGM_AUTH_GRPC_WIRE_MODE =
                $resolvedAuthenticationGrpcWireMode
        }
    $worldEnvironment = Merge-ProcessEnvironment `
        -Base $sharedServerEnvironment `
        -Additional @{
            NOSGM_AUTH_GRPC_URL = $authenticationGrpcEndpoint
            NOSGM_AUTH_GRPC_CLIENT_CERT_PATH =
                [string]$manifest.Clients.World.CertificatePath
            NOSGM_AUTH_GRPC_CLIENT_CERT_PASSWORD =
                ConvertFrom-SecureStringInMemory $credentials.World
            NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID = "world-local-1"
            NOSGM_AUTH_GRPC_DEADLINE_MILLISECONDS = "10000"
            NOSGM_AUTH_GRPC_WIRE_MODE =
                $resolvedAuthenticationGrpcWireMode
        }
}

try {
    New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null

    if ($AuthenticationTransport -eq "GRPC") {
        Start-TrackedProcess `
            -Name "AuthenticationGrpc" `
            -Executable $dotnetExecutable `
            -Arguments @($authenticationAssembly) `
            -WorkingDirectory $authenticationDirectory `
            -ProcessEnvironment $authenticationRuntimeEnvironment |
            Out-Null
        $authenticationRuntimeEnvironment.Clear()
        Wait-TcpPort `
            -HostName "127.0.0.1" `
            -Port $AuthenticationGrpcPort `
            -Description "Authentication gRPC"
    }

    Start-TrackedProcess `
        -Name "Master" `
        -Executable $masterExecutable `
        -ProcessEnvironment $masterEnvironment |
        Out-Null
    if ($AuthenticationTransport -eq "GRPC") {
        $masterEnvironment.Clear()
    }
    Wait-TcpPort -HostName "127.0.0.1" -Port $masterPort -Description "Master"
    Wait-TcpPort -HostName "127.0.0.1" -Port $BridgePort -Description "Launcher AuthBridge"

    Start-TrackedProcess `
        -Name "World" `
        -Executable $worldExecutable `
        -Arguments @("--nomsg", "--port", $WorldPort.ToString()) `
        -ProcessEnvironment $worldEnvironment |
        Out-Null
    if ($AuthenticationTransport -eq "GRPC") {
        $worldEnvironment.Clear()
    }
    Wait-TcpPort -HostName "127.0.0.1" -Port $WorldPort -Description "World"

    Start-TrackedProcess `
        -Name "Login" `
        -Executable $loginExecutable `
        -Arguments @("--nomsg") `
        -ProcessEnvironment $loginEnvironment |
        Out-Null
    if ($AuthenticationTransport -eq "GRPC") {
        $loginEnvironment.Clear()
    }
    $sharedServerEnvironment.Clear()
    Wait-TcpPort -HostName "127.0.0.1" -Port $spanishLoginPort -Description "Spanish Login"

    if (-not $SkipLauncher) {
        Start-TrackedProcess `
            -Name "Launcher" `
            -Executable $launcherExecutable `
            -ProcessEnvironment @{
                NOSGM_AUTH_ENDPOINT = $launcherEndpoint
                DOTNET_ROOT = $dotnetRoot
            } |
            Out-Null
    }

    $state = [pscustomobject]@{
        SchemaVersion = 1
        CreatedAtUtc = [DateTime]::UtcNow.ToString("O")
        AuthenticationTransport = $AuthenticationTransport
        AuthenticationGrpcWireMode =
            if ($AuthenticationTransport -eq "GRPC") {
                $resolvedAuthenticationGrpcWireMode
            }
            else {
                $null
            }
        AuthenticationEndpoint = $launcherEndpoint
        AuthenticationGrpcEndpoint =
            if ($AuthenticationTransport -eq "GRPC") {
                $authenticationGrpcEndpoint
            }
            else {
                $null
            }
        HealthEndpoint = $healthEndpoint
        SpanishLoginPort = $spanishLoginPort
        WorldPort = $WorldPort
        Processes = $startedProcesses.ToArray()
    }
    $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath -Encoding UTF8
    Restore-ProcessEnvironment

    if (-not $SkipLauncher) {
        Start-Sleep -Seconds 2
    }

    $readiness = [pscustomobject]@{ OverallStatus = "failed" }
    try {
        $readinessParameters = @{
            OutputPath = (Join-Path $stateDirectory "readiness.json")
            PassThru = $true
        }
        if (-not $SkipLauncher) {
            $readinessParameters["RequireLauncher"] = $true
        }
        $readiness = & (Join-Path $PSScriptRoot "test-modern-login-readiness.ps1") @readinessParameters
    }
    catch {
        Write-Warning "The stack is running, but the readiness inspector could not complete: $($_.Exception.Message)"
    }

    Write-Host ""
    Write-Host "NosGM modern Login local stack is ready." -ForegroundColor Green
    Write-Host "Authentication endpoint: $launcherEndpoint"
    Write-Host "Internal authentication transport: $AuthenticationTransport"
    if ($AuthenticationTransport -eq "GRPC") {
        Write-Host "Authentication gRPC endpoint: $authenticationGrpcEndpoint"
        Write-Host "Authentication wire mode: $resolvedAuthenticationGrpcWireMode"
    }
    Write-Host "Health endpoint: $healthEndpoint"
    Write-Host "Launcher language: Español (region 5 / Login $spanishLoginPort)"
    Write-Host "Secrets were inherited by the child processes only for their role, removed from this shell and never written to plaintext files."
    if ($readiness.OverallStatus -eq "failed") {
        Write-Warning "The stack is running, but readiness found blockers. Fix them and rerun ./scripts/test-modern-login-readiness.ps1"
    }
    elseif ($readiness.OverallStatus -eq "warning") {
        Write-Warning "The stack is running with readiness warnings. Review readiness.json before the client test."
    }
    else {
        Write-Host "Readiness checks passed. You can begin the real-client acceptance test." -ForegroundColor Green
    }
    Write-Host "Collect sanitized evidence with: ./scripts/collect-modern-login-diagnostics.ps1"
    Write-Host "Stop the stack with: ./scripts/stop-modern-login-local.ps1"
}
catch {
    Restore-ProcessEnvironment
    $authenticationRuntimeEnvironment.Clear()
    $masterEnvironment.Clear()
    $worldEnvironment.Clear()
    $loginEnvironment.Clear()
    $sharedServerEnvironment.Clear()
    Stop-StartedProcesses
    Remove-Item -LiteralPath $statePath -Force -ErrorAction SilentlyContinue
    throw
}
