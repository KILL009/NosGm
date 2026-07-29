[CmdletBinding()]
param(
    [string]$CertificateManifest,
    [switch]$SkipBuild,
    [ValidateRange(1024, 65535)]
    [int]$Port = 7443,
    [ValidateRange(10, 120)]
    [int]$StartupTimeoutSeconds = 45
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "The live NosGM authentication gRPC acceptance test requires Windows."
}
if (-not (Get-Command dotnet.exe -ErrorAction SilentlyContinue)) {
    throw ".NET 10 SDK was not found."
}

$operatingSystem = Get-CimInstance -ClassName Win32_OperatingSystem
$operatingSystemVersion = [Version]$operatingSystem.Version
$isWorkstation = [int]$operatingSystem.ProductType -eq 1
if (($isWorkstation -and $operatingSystemVersion.Build -lt 22000) -or
    (-not $isWorkstation -and
        $operatingSystemVersion.Build -lt 17763)) {
    throw "The complete NosGM gRPC path requires Windows 11 or Windows Server 2019 or later."
}

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($CertificateManifest)) {
    $CertificateManifest = Join-Path `
        $root `
        "artifacts\authentication-grpc-local\manifest.json"
}
$manifestPath = [System.IO.Path]::GetFullPath($CertificateManifest)
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "The local authentication certificate manifest does not exist: $manifestPath"
}

$manifest =
    Get-Content -LiteralPath $manifestPath -Raw |
    ConvertFrom-Json
if ($manifest.SchemaVersion -ne 1 -or $null -eq $manifest.Clients) {
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

$trustedRootPath =
    "Cert:\CurrentUser\Root\" +
    [string]$manifest.RootCertificateThumbprint
if (-not (Test-Path -LiteralPath $trustedRootPath)) {
    throw "The NosGM development root is not trusted for the current Windows user. Import '$($manifest.RootCertificatePath)' into Cert:\CurrentUser\Root first."
}

foreach ($certificatePath in @(
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

$environmentVariableNames = @(
    "NOSGM_AUTH_GRPC_SERVER_CERT_PATH",
    "NOSGM_AUTH_GRPC_SERVER_CERT_PASSWORD",
    "NOSGM_AUTH_GRPC_AUTHBRIDGE_CERT_SHA256",
    "NOSGM_AUTH_GRPC_LOGIN_CERT_SHA256",
    "NOSGM_AUTH_GRPC_WORLD_CERT_SHA256",
    "NOSGM_AUTH_GRPC_PORT",
    "NOSGM_AUTH_GRPC_TICKET_TTL_SECONDS",
    "NOSGM_AUTH_GRPC_PERMIT_TTL_SECONDS",
    "NOSGM_AUTH_GRPC_INSTANCE_ID",
    "NOSGM_AUTH_GRPC_URL",
    "NOSGM_AUTH_GRPC_LIVE_AUTHBRIDGE_CERT_PATH",
    "NOSGM_AUTH_GRPC_LIVE_AUTHBRIDGE_CERT_PASSWORD",
    "NOSGM_AUTH_GRPC_LIVE_LOGIN_CERT_PATH",
    "NOSGM_AUTH_GRPC_LIVE_LOGIN_CERT_PASSWORD",
    "NOSGM_AUTH_GRPC_LIVE_WORLD_CERT_PATH",
    "NOSGM_AUTH_GRPC_LIVE_WORLD_CERT_PASSWORD"
)
$previousEnvironment = @{}
foreach ($variableName in $environmentVariableNames) {
    $previousEnvironment[$variableName] =
        [Environment]::GetEnvironmentVariable(
            $variableName,
            "Process")
}

function Restore-ProcessEnvironment {
    foreach ($variableName in $environmentVariableNames) {
        [Environment]::SetEnvironmentVariable(
            $variableName,
            $previousEnvironment[$variableName],
            "Process")
    }
}

function Set-ProcessEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [Collections.IDictionary]$Values
    )

    foreach ($entry in $Values.GetEnumerator()) {
        $variableName = [string]$entry.Key
        if ($environmentVariableNames -notcontains $variableName) {
            throw "Process environment variable is not allow-listed: $variableName"
        }
    }
    foreach ($variableName in $environmentVariableNames) {
        [Environment]::SetEnvironmentVariable(
            $variableName,
            $null,
            "Process")
    }
    foreach ($entry in $Values.GetEnumerator()) {
        [Environment]::SetEnvironmentVariable(
            [string]$entry.Key,
            [string]$entry.Value,
            "Process")
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

function Wait-AuthenticationRuntime {
    param(
        [Parameter(Mandatory = $true)]
        [Diagnostics.Process]$Process
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            throw "The authentication runtime exited with code $($Process.ExitCode) before accepting connections."
        }

        $client = New-Object Net.Sockets.TcpClient
        $result = $null
        try {
            $result = $client.BeginConnect(
                "127.0.0.1",
                $Port,
                $null,
                $null)
            if ($result.AsyncWaitHandle.WaitOne(500) -and
                $client.Connected) {
                $client.EndConnect($result)
                Write-Host `
                    "[READY] Authentication gRPC on 127.0.0.1:$Port" `
                    -ForegroundColor Green
                return
            }
        }
        catch {
            # Kestrel may still be starting.
        }
        finally {
            if ($null -ne $result) {
                $result.AsyncWaitHandle.Close()
            }
            $client.Dispose()
        }
        Start-Sleep -Milliseconds 300
    }

    throw "The authentication runtime did not listen on port $Port within $StartupTimeoutSeconds seconds."
}

$authenticationOutput =
    Join-Path $root "artifacts\authentication-grpc-acceptance\runtime"
$authenticationProject = Join-Path `
    $root `
    "Data\NosGm.Program\NosGm.Authentication.Server\NosGm.Authentication.Server.csproj"
$selfTestProject = Join-Path `
    $root `
    "tests\NosGm.Authentication.Runtime.SelfTest\NosGm.Authentication.Runtime.SelfTest.csproj"
$selfTestAssembly = Join-Path `
    $root `
    "tests\NosGm.Authentication.Runtime.SelfTest\bin\Release\net10.0\NosGm.Authentication.Runtime.SelfTest.dll"

if (-not $SkipBuild) {
    & dotnet publish `
        $authenticationProject `
        --configuration Release `
        --output $authenticationOutput `
        --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Authentication runtime publish failed."
    }

    & dotnet build `
        $selfTestProject `
        --configuration Release `
        --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Authentication runtime self-test build failed."
    }
}

$authenticationExecutable =
    Join-Path $authenticationOutput "NosGm.Authentication.Server.exe"
foreach ($requiredFile in @(
    $authenticationExecutable,
    $selfTestAssembly
)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "A required live acceptance binary is missing: $requiredFile"
    }
}

$runtimeProcess = $null
try {
    Set-ProcessEnvironment -Values @{
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
        NOSGM_AUTH_GRPC_PORT = $Port.ToString()
        NOSGM_AUTH_GRPC_TICKET_TTL_SECONDS = "120"
        NOSGM_AUTH_GRPC_PERMIT_TTL_SECONDS = "120"
        NOSGM_AUTH_GRPC_INSTANCE_ID = "authentication-acceptance-1"
    }
    $runtimeProcess = Start-Process `
        -FilePath $authenticationExecutable `
        -WorkingDirectory $authenticationOutput `
        -NoNewWindow `
        -PassThru
    Restore-ProcessEnvironment
    Wait-AuthenticationRuntime -Process $runtimeProcess

    Set-ProcessEnvironment -Values @{
        NOSGM_AUTH_GRPC_URL = "https://127.0.0.1:$Port"
        NOSGM_AUTH_GRPC_LIVE_AUTHBRIDGE_CERT_PATH =
            [string]$manifest.Clients.AuthBridge.CertificatePath
        NOSGM_AUTH_GRPC_LIVE_AUTHBRIDGE_CERT_PASSWORD =
            ConvertFrom-SecureStringInMemory $credentials.AuthBridge
        NOSGM_AUTH_GRPC_LIVE_LOGIN_CERT_PATH =
            [string]$manifest.Clients.Login.CertificatePath
        NOSGM_AUTH_GRPC_LIVE_LOGIN_CERT_PASSWORD =
            ConvertFrom-SecureStringInMemory $credentials.Login
        NOSGM_AUTH_GRPC_LIVE_WORLD_CERT_PATH =
            [string]$manifest.Clients.World.CertificatePath
        NOSGM_AUTH_GRPC_LIVE_WORLD_CERT_PASSWORD =
            ConvertFrom-SecureStringInMemory $credentials.World
    }

    & dotnet $selfTestAssembly --live
    if ($LASTEXITCODE -ne 0) {
        throw "Live authentication gRPC acceptance failed."
    }

    Write-Host ""
    Write-Host `
        "NosGM authentication gRPC mTLS acceptance passed." `
        -ForegroundColor Green
}
finally {
    Restore-ProcessEnvironment
    if ($null -ne $runtimeProcess) {
        try {
            if (-not $runtimeProcess.HasExited) {
                Stop-Process -Id $runtimeProcess.Id -Force
            }
        }
        finally {
            $runtimeProcess.Dispose()
        }
    }
}
