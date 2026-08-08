[CmdletBinding()]
param(
    [ValidateSet("Status", "Restart")]
    [string]$Operation = "Restart",
    [string]$ExpectedRuntimeGenerationId,
    [string]$AuthenticationCertificateManifest,
    [switch]$SkipBuild,
    [switch]$PassThru
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "Configuration runtime control requires Windows."
}

$root = Split-Path -Parent $PSScriptRoot
$statePath = Join-Path $root "artifacts\modern-login-local\processes.json"
$project = Join-Path $root `
    "Tools\NosGM.ConfigurationRuntimeController\NosGM.ConfigurationRuntimeController.csproj"
$assembly = Join-Path $root `
    "Tools\NosGM.ConfigurationRuntimeController\bin\Release\net10.0\NosGM.ConfigurationRuntimeController.dll"

if ([string]::IsNullOrWhiteSpace($AuthenticationCertificateManifest)) {
    $AuthenticationCertificateManifest = Join-Path $root `
        "artifacts\authentication-grpc-local\manifest.json"
}

function Resolve-DotNet10Executable {
    $candidates = New-Object Collections.Generic.List[string]
    $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($command) {
        [void]$candidates.Add([string]$command.Source)
    }
    foreach ($directory in @(
        $env:DOTNET_ROOT,
        (Join-Path $env:LOCALAPPDATA "NosGM\dotnet10"),
        (Join-Path $env:ProgramFiles "dotnet"),
        (Join-Path ${env:ProgramFiles(x86)} "dotnet")
    )) {
        if (-not [string]::IsNullOrWhiteSpace($directory)) {
            [void]$candidates.Add((Join-Path $directory "dotnet.exe"))
        }
    }

    foreach ($candidate in @($candidates | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            continue
        }
        $sdks = & $candidate --list-sdks 2>$null
        if ($LASTEXITCODE -eq 0 -and
            @($sdks | Where-Object { $_ -match '^10\.' }).Count -gt 0) {
            return [IO.Path]::GetFullPath($candidate)
        }
    }
    throw ".NET 10 SDK was not found."
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

foreach ($required in @($statePath, $project)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required Configuration runtime control file is missing: $required"
    }
}
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
if ($state.SchemaVersion -ne 1 -or
    $state.AuthenticationTransport -ne "GRPC" -or
    [string]::IsNullOrWhiteSpace(
        [string]$state.AuthenticationGrpcEndpoint)) {
    throw "The local stack is not running with the Configuration gRPC runtime."
}
if ($state.PSObject.Properties.Name -notcontains
        "ConfigurationRuntimeControlEnabled" -or
    $state.ConfigurationRuntimeControlEnabled -ne $true) {
    throw "The local stack was not started with -EnableConfigurationRuntimeControl."
}

$manifestPath = [IO.Path]::GetFullPath(
    $AuthenticationCertificateManifest)
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "The authentication certificate manifest does not exist."
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.SchemaVersion -ne 1 -or
    $null -eq $manifest.Clients.Master -or
    [string]::IsNullOrWhiteSpace([string]$manifest.CredentialsPath)) {
    throw "The authentication certificate manifest has no Master identity."
}
$masterCertificatePath =
    [IO.Path]::GetFullPath(
        [string]$manifest.Clients.Master.CertificatePath)
$rootCertificatePath =
    [IO.Path]::GetFullPath([string]$manifest.RootCertificatePath)
foreach ($certificatePath in @(
    $masterCertificatePath,
    $rootCertificatePath
)) {
    if (-not (Test-Path -LiteralPath $certificatePath -PathType Leaf)) {
        throw "A Configuration runtime control certificate is missing."
    }
}
$credentialsPath = [IO.Path]::GetFullPath(
    [string]$manifest.CredentialsPath)
$credentials = Import-Clixml -LiteralPath $credentialsPath
if ($credentials.SchemaVersion -ne 1 -or
    $null -eq $credentials.Master -or
    $credentials.Master -isnot [Security.SecureString]) {
    throw "The DPAPI-protected Master credential is unavailable."
}

$dotnet = Resolve-DotNet10Executable
if (-not $SkipBuild) {
    & $dotnet build $project `
        --configuration Release `
        --nologo `
        --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Configuration runtime controller build failed."
    }
}
if (-not (Test-Path -LiteralPath $assembly -PathType Leaf)) {
    throw "The Configuration runtime controller assembly is missing."
}

$variableNames = @(
    "NOSGM_CONFIGURATION_GRPC_CONTROL_URL",
    "NOSGM_CONFIGURATION_GRPC_CONTROL_MASTER_CERT_PATH",
    "NOSGM_CONFIGURATION_GRPC_CONTROL_MASTER_CERT_PASSWORD",
    "NOSGM_CONFIGURATION_GRPC_CONTROL_TRUSTED_ROOT_CERT_PATH",
    "NOSGM_CONFIGURATION_GRPC_CONTROL_INSTANCE_ID",
    "NOSGM_CONFIGURATION_GRPC_CONTROL_DEADLINE_MILLISECONDS",
    "NOSGM_CONFIGURATION_GRPC_CONTROL_WIRE_MODE"
)
$previous = @{}
foreach ($name in $variableNames) {
    $previous[$name] = [Environment]::GetEnvironmentVariable(
        $name,
        "Process")
}

try {
    [Environment]::SetEnvironmentVariable(
        "NOSGM_CONFIGURATION_GRPC_CONTROL_URL",
        [string]$state.AuthenticationGrpcEndpoint,
        "Process")
    [Environment]::SetEnvironmentVariable(
        "NOSGM_CONFIGURATION_GRPC_CONTROL_MASTER_CERT_PATH",
        $masterCertificatePath,
        "Process")
    [Environment]::SetEnvironmentVariable(
        "NOSGM_CONFIGURATION_GRPC_CONTROL_MASTER_CERT_PASSWORD",
        (ConvertFrom-SecureStringInMemory $credentials.Master),
        "Process")
    [Environment]::SetEnvironmentVariable(
        "NOSGM_CONFIGURATION_GRPC_CONTROL_TRUSTED_ROOT_CERT_PATH",
        $rootCertificatePath,
        "Process")
    [Environment]::SetEnvironmentVariable(
        "NOSGM_CONFIGURATION_GRPC_CONTROL_INSTANCE_ID",
        "configuration-runtime-controller-$PID",
        "Process")
    [Environment]::SetEnvironmentVariable(
        "NOSGM_CONFIGURATION_GRPC_CONTROL_DEADLINE_MILLISECONDS",
        "10000",
        "Process")
    $wireMode = if ([string]::IsNullOrWhiteSpace(
            [string]$state.AuthenticationGrpcWireMode)) {
        "HTTP2"
    }
    else {
        [string]$state.AuthenticationGrpcWireMode
    }
    [Environment]::SetEnvironmentVariable(
        "NOSGM_CONFIGURATION_GRPC_CONTROL_WIRE_MODE",
        $wireMode,
        "Process")

    $arguments = @($assembly, $Operation.ToLowerInvariant())
    if ($Operation -eq "Restart" -and
        -not [string]::IsNullOrWhiteSpace(
            $ExpectedRuntimeGenerationId)) {
        if ($ExpectedRuntimeGenerationId -notmatch
                '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') {
            throw "ExpectedRuntimeGenerationId must be a lowercase canonical GUID."
        }
        $arguments += $ExpectedRuntimeGenerationId
    }

    $output = @(& $dotnet @arguments)
    $exitCode = $LASTEXITCODE
}
finally {
    foreach ($name in $variableNames) {
        [Environment]::SetEnvironmentVariable(
            $name,
            $previous[$name],
            "Process")
    }
}

if ($exitCode -ne 0) {
    throw "Configuration runtime control failed with exit code $exitCode."
}
$json = $output -join [Environment]::NewLine
$result = $json | ConvertFrom-Json
if ($result.schemaVersion -ne 1 -or $result.result -ne "Success") {
    throw "Configuration runtime control returned an invalid result."
}

if ($PassThru) {
    return $result
}
$json | Write-Host
