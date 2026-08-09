[CmdletBinding()]
param(
    [string]$CertificateManifest,
    [switch]$SkipBuild,
    [ValidateRange(5, 60)]
    [int]$DeliveryTimeoutSeconds = 20
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "Communication callback shadow acceptance requires Windows."
}

$root = Split-Path -Parent $PSScriptRoot
$statePath = Join-Path $root "artifacts\modern-login-local\processes.json"
if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) {
    throw "No running local stack state was found. Start it with scripts/start-communication-callback-shadow-local.ps1 first."
}

$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
if ($state.SchemaVersion -ne 2 -or
    [string]$state.CommunicationCallbackMode -ne "Shadow" -or
    [string]$state.CommunicationCallbackEffectAuthority -ne "SCS" -or
    [string]$state.CommunicationCallbackPublication -ne "gRPC mirror") {
    throw "The running stack is not the explicit communication callback shadow stack."
}
if ([string]$state.AuthenticationTransport -ne "GRPC") {
    throw "Callback shadow acceptance requires the role-separated GRPC authentication identities."
}
if ([string]$state.AuthenticationGrpcWireMode -notin @("HTTP2", "GRPCWEB")) {
    throw "The running stack has no supported gRPC wire mode."
}

function Resolve-DotNet10Executable {
    $candidates = New-Object System.Collections.Generic.List[string]
    $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($command) {
        $candidates.Add([string]$command.Source)
    }
    foreach ($directory in @(
        $env:DOTNET_ROOT,
        (Join-Path $env:LOCALAPPDATA "NosGM\dotnet10"),
        (Join-Path $env:ProgramFiles "dotnet"),
        (Join-Path ${env:ProgramFiles(x86)} "dotnet")
    )) {
        if (-not [string]::IsNullOrWhiteSpace($directory)) {
            $candidates.Add((Join-Path $directory "dotnet.exe"))
        }
    }
    foreach ($candidate in @($candidates | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            continue
        }
        $sdks = & $candidate --list-sdks 2>$null
        if ($LASTEXITCODE -eq 0 -and
            @($sdks | Where-Object { $_ -match '^10\.' }).Count -gt 0) {
            return [System.IO.Path]::GetFullPath($candidate)
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

function Get-CallbackCursorPath {
    param([Parameter(Mandatory = $true)][string]$CallerInstanceId)

    $localApplicationData =
        [Environment]::GetFolderPath(
            [Environment+SpecialFolder]::LocalApplicationData)
    if ([string]::IsNullOrWhiteSpace($localApplicationData) -or
        -not [System.IO.Path]::IsPathRooted($localApplicationData)) {
        throw "LocalApplicationData is unavailable for callback cursor acceptance."
    }

    $bytes = [Text.Encoding]::UTF8.GetBytes($CallerInstanceId)
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $digest = $sha256.ComputeHash($bytes)
    }
    finally {
        $sha256.Dispose()
    }
    $fingerprint =
        ([BitConverter]::ToString($digest)).Replace("-", "").ToLowerInvariant()
    return [System.IO.Path]::GetFullPath(
        (Join-Path $localApplicationData (
            "NosGM\communication-callback\cursor-" +
            $fingerprint + ".txt")))
}

function Read-CallbackCursor {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [pscustomobject]@{
            Exists = $false
            Generation = ""
            Sequence = [UInt64]0
            LastWriteTimeUtc = [DateTime]::MinValue
        }
    }

    $item = Get-Item -LiteralPath $Path
    $lines = [IO.File]::ReadAllText($Path, [Text.Encoding]::ASCII).Trim() -split "`r?`n"
    if ($lines.Count -ne 3 -or $lines[0] -ne "NOSGM_CALLBACK_CURSOR_V1") {
        throw "The callback cursor is malformed: $Path"
    }
    $generation = [Guid]::Empty
    if (-not [Guid]::TryParseExact($lines[1], "D", [ref]$generation) -or
        $generation -eq [Guid]::Empty) {
        throw "The callback cursor generation is invalid: $Path"
    }
    $sequence = [UInt64]0
    if (-not [UInt64]::TryParse($lines[2], [ref]$sequence)) {
        throw "The callback cursor sequence is invalid: $Path"
    }

    return [pscustomobject]@{
        Exists = $true
        Generation = $generation.ToString("D")
        Sequence = $sequence
        LastWriteTimeUtc = $item.LastWriteTimeUtc
    }
}

function Test-CursorAdvanced {
    param(
        [Parameter(Mandatory = $true)]$Baseline,
        [Parameter(Mandatory = $true)]$Current,
        [Parameter(Mandatory = $true)][UInt64]$AcceptedSequence
    )

    if (-not $Current.Exists -or $Current.Sequence -lt $AcceptedSequence) {
        return $false
    }
    if (-not $Baseline.Exists) {
        return $true
    }
    return $Current.LastWriteTimeUtc -gt $Baseline.LastWriteTimeUtc -and
        ($Current.Generation -ne $Baseline.Generation -or
         $Current.Sequence -ne $Baseline.Sequence)
}

if ([string]::IsNullOrWhiteSpace($CertificateManifest)) {
    $CertificateManifest = Join-Path `
        $root `
        "artifacts\authentication-grpc-local\manifest.json"
}
$manifestPath = [System.IO.Path]::GetFullPath($CertificateManifest)
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "The local authentication certificate manifest does not exist: $manifestPath"
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.SchemaVersion -ne 1 -or
    $null -eq $manifest.Clients -or
    $null -eq $manifest.Clients.Master) {
    throw "The local authentication certificate manifest is invalid."
}
$credentialsPath = [System.IO.Path]::GetFullPath(
    [string]$manifest.CredentialsPath)
$credentials = Import-Clixml -LiteralPath $credentialsPath
if ($credentials.SchemaVersion -ne 1 -or
    $credentials.Master -isnot [Security.SecureString]) {
    throw "The DPAPI-protected Master callback credential is unavailable."
}

$dotnet = Resolve-DotNet10Executable
$selfTestProject = Join-Path `
    $root `
    "tests\NosGm.Authentication.Runtime.SelfTest\NosGm.Authentication.Runtime.SelfTest.csproj"
$selfTestAssembly = Join-Path `
    $root `
    "tests\NosGm.Authentication.Runtime.SelfTest\bin\Release\net10.0\NosGm.Authentication.Runtime.SelfTest.dll"
if (-not $SkipBuild) {
    & $dotnet build `
        $selfTestProject `
        --configuration Release `
        --nologo `
        -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) {
        throw "The PenaltyRefresh live probe build failed."
    }
}
if (-not (Test-Path -LiteralPath $selfTestAssembly -PathType Leaf)) {
    throw "The PenaltyRefresh live probe assembly is missing: $selfTestAssembly"
}

$loginCursorPath = Get-CallbackCursorPath "login-local-1"
$worldCursorPath = Get-CallbackCursorPath "world-local-1"
$loginBaseline = Read-CallbackCursor $loginCursorPath
$worldBaseline = Read-CallbackCursor $worldCursorPath

$environmentVariableNames = @(
    "NOSGM_COMMUNICATION_GRPC_URL",
    "NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PATH",
    "NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PASSWORD",
    "NOSGM_COMMUNICATION_GRPC_TRUSTED_ROOT_CERT_PATH",
    "NOSGM_COMMUNICATION_GRPC_MASTER_INSTANCE_ID",
    "NOSGM_COMMUNICATION_GRPC_DEADLINE_MILLISECONDS",
    "NOSGM_COMMUNICATION_GRPC_WIRE_MODE",
    "NOSGM_COMMUNICATION_GRPC_USE_EXISTING_IDENTITY_FALLBACK"
)
$previousEnvironment = @{}
foreach ($name in $environmentVariableNames) {
    $previousEnvironment[$name] =
        [Environment]::GetEnvironmentVariable(
            $name,
            [EnvironmentVariableTarget]::Process)
}

try {
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_URL",
        [string]$state.AuthenticationGrpcEndpoint,
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PATH",
        [string]$manifest.Clients.Master.CertificatePath,
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PASSWORD",
        (ConvertFrom-SecureStringInMemory $credentials.Master),
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_TRUSTED_ROOT_CERT_PATH",
        [string]$manifest.RootCertificatePath,
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_MASTER_INSTANCE_ID",
        "master-callback-penalty-probe-1",
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_DEADLINE_MILLISECONDS",
        "10000",
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_WIRE_MODE",
        [string]$state.AuthenticationGrpcWireMode,
        [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable(
        "NOSGM_COMMUNICATION_GRPC_USE_EXISTING_IDENTITY_FALLBACK",
        "false",
        [EnvironmentVariableTarget]::Process)

    Write-Host "[CALLBACK-SHADOW] Publishing observation-only PenaltyRefresh through Master mTLS..." -ForegroundColor Cyan
    $probeOutput = @(& $dotnet $selfTestAssembly --live-penalty-refresh-probe 2>&1)
    $probeExitCode = $LASTEXITCODE
    $probeOutput | ForEach-Object { Write-Host $_ }
    if ($probeExitCode -ne 0) {
        throw "The typed PenaltyRefresh publication probe failed with exit code $probeExitCode."
    }

    $probeText = $probeOutput -join "`n"
    $match = [Regex]::Match(
        $probeText,
        '\[CALLBACK_PENALTY_PROBE\] AcceptedSequence=(\d+) MatchedSubscribers=(\d+) PenaltyLogId=(\d+)')
    if (-not $match.Success) {
        throw "The PenaltyRefresh probe did not return a parseable acceptance marker."
    }
    $acceptedSequence = [UInt64]::Parse($match.Groups[1].Value)
    $matchedSubscribers = [UInt32]::Parse($match.Groups[2].Value)
    if ($acceptedSequence -eq 0 -or $matchedSubscribers -lt 2) {
        throw "The PenaltyRefresh probe did not match both Login and World subscribers."
    }

    $deadline = [DateTime]::UtcNow.AddSeconds($DeliveryTimeoutSeconds)
    $loginCurrent = $loginBaseline
    $worldCurrent = $worldBaseline
    while ([DateTime]::UtcNow -lt $deadline) {
        $loginCurrent = Read-CallbackCursor $loginCursorPath
        $worldCurrent = Read-CallbackCursor $worldCursorPath
        if ((Test-CursorAdvanced $loginBaseline $loginCurrent $acceptedSequence) -and
            (Test-CursorAdvanced $worldBaseline $worldCurrent $acceptedSequence)) {
            break
        }
        Start-Sleep -Milliseconds 200
    }

    if (-not (Test-CursorAdvanced $loginBaseline $loginCurrent $acceptedSequence)) {
        throw "Login did not durably observe PenaltyRefresh sequence $acceptedSequence within $DeliveryTimeoutSeconds seconds."
    }
    if (-not (Test-CursorAdvanced $worldBaseline $worldCurrent $acceptedSequence)) {
        throw "World did not durably observe PenaltyRefresh sequence $acceptedSequence within $DeliveryTimeoutSeconds seconds."
    }
    if ($loginCurrent.Generation -ne $worldCurrent.Generation) {
        throw "Login and World committed the probe against different callback runtime generations."
    }

    Write-Host "[PASS] Login durably observed PenaltyRefresh sequence $($loginCurrent.Sequence)." -ForegroundColor Green
    Write-Host "[PASS] World durably observed PenaltyRefresh sequence $($worldCurrent.Sequence)." -ForegroundColor Green
    Write-Host "[PASS] Login and World share callback runtime generation $($loginCurrent.Generation)." -ForegroundColor Green
    Write-Host "[PASS] SCS remains callback effect authority; the typed probe was observation-only." -ForegroundColor Green
    Write-Host "Communication PenaltyRefresh real-process shadow acceptance passed." -ForegroundColor Green
}
finally {
    foreach ($name in $environmentVariableNames) {
        [Environment]::SetEnvironmentVariable(
            $name,
            $previousEnvironment[$name],
            [EnvironmentVariableTarget]::Process)
    }
}
