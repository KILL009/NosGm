[CmdletBinding()]
param(
    [string]$CertificateManifest,
    [switch]$SkipBuild,
    [switch]$TrustRootForCurrentUser,
    [ValidateRange(1024, 65535)]
    [int]$Port = 7443,
    [ValidateRange(10, 120)]
    [int]$StartupTimeoutSeconds = 45
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "The Configuration gRPC shadow acceptance test requires Windows."
}

$root = Split-Path -Parent $PSScriptRoot
$acceptanceRoot = Join-Path $root "artifacts\configuration-grpc-shadow-acceptance"
$runtimeOutput = Join-Path $acceptanceRoot "runtime"
$clientRoot = Join-Path $acceptanceRoot "client"
$clientProject = Join-Path $clientRoot "NosGm.Configuration.Grpc.Acceptance.csproj"
$clientSource = Join-Path $clientRoot "Program.cs"
$clientExecutable = Join-Path $clientRoot "bin\Release\net481\NosGm.Configuration.Grpc.Acceptance.exe"
$runtimeProject = Join-Path $root "Data\NosGm.Program\NosGm.Authentication.Server\NosGm.Authentication.Server.csproj"
$runtimeAssembly = Join-Path $runtimeOutput "NosGm.Authentication.Server.dll"
$authenticationClientProject = Join-Path $root "Data\NosGm.Authentication.Client\NosGm.Authentication.Client.csproj"

function Resolve-DotNet10Executable {
    $candidates = New-Object System.Collections.Generic.List[string]
    $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($command) {
        $candidates.Add([string]$command.Source)
    }
    foreach ($directory in @(
        $env:DOTNET_ROOT,
        (Join-Path $env:LOCALAPPDATA "NosGM\dotnet10"),
        (Join-Path $env:LOCALAPPDATA "NosGM\dotnet9"),
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
        if ($LASTEXITCODE -eq 0 -and @($sdks | Where-Object { $_ -match '^10\.' }).Count -gt 0) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }
    throw ".NET 10 SDK was not found."
}

function Test-NetFrameworkGrpcHttp2Support {
    $operatingSystem = Get-CimInstance -ClassName Win32_OperatingSystem
    $version = [Version]$operatingSystem.Version
    $isWorkstation = [int]$operatingSystem.ProductType -eq 1
    return (($isWorkstation -and $version.Build -ge 22000) -or
        (-not $isWorkstation -and $version.Build -ge 17763))
}

function ConvertFrom-SecureStringInMemory {
    param([Parameter(Mandatory = $true)][Security.SecureString]$Value)

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Wait-ConfigurationRuntime {
    param([Parameter(Mandatory = $true)][Diagnostics.Process]$Process)

    $deadline = [DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            throw "The cluster runtime exited with code $($Process.ExitCode) before accepting Configuration calls."
        }
        $client = New-Object Net.Sockets.TcpClient
        $result = $null
        try {
            $result = $client.BeginConnect("127.0.0.1", $Port, $null, $null)
            if ($result.AsyncWaitHandle.WaitOne(500) -and $client.Connected) {
                $client.EndConnect($result)
                Write-Host "[READY] Configuration gRPC on 127.0.0.1:$Port" -ForegroundColor Green
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
    throw "The cluster runtime did not listen on port $Port within $StartupTimeoutSeconds seconds."
}

$dotnet = Resolve-DotNet10Executable
$supportsHttp2 = Test-NetFrameworkGrpcHttp2Support

if ([string]::IsNullOrWhiteSpace($CertificateManifest)) {
    $CertificateManifest = Join-Path $root "artifacts\authentication-grpc-local\manifest.json"
}
$manifestPath = [System.IO.Path]::GetFullPath($CertificateManifest)
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "The authentication certificate manifest does not exist: $manifestPath"
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.SchemaVersion -ne 1 -or $null -eq $manifest.Clients -or $null -eq $manifest.Clients.World) {
    throw "The authentication certificate manifest is invalid."
}
$credentialsPath = [System.IO.Path]::GetFullPath([string]$manifest.CredentialsPath)
if (-not (Test-Path -LiteralPath $credentialsPath -PathType Leaf)) {
    throw "The protected authentication credential bundle is missing."
}
$credentials = Import-Clixml -LiteralPath $credentialsPath
if ($credentials.SchemaVersion -ne 1) {
    throw "The protected authentication credential bundle is invalid."
}

$rootCertificatePath = [System.IO.Path]::GetFullPath([string]$manifest.RootCertificatePath)
$rootThumbprint = [string]$manifest.RootCertificateThumbprint
$trustedRootStorePath = "Cert:\CurrentUser\Root\$rootThumbprint"
$installedRootByTest = $false
if (-not (Test-Path -LiteralPath $trustedRootStorePath)) {
    if (-not $TrustRootForCurrentUser) {
        throw "The net481 World client requires the NosGM development root in Cert:\CurrentUser\Root. Re-run with -TrustRootForCurrentUser to install it temporarily."
    }
    Import-Certificate -FilePath $rootCertificatePath -CertStoreLocation "Cert:\CurrentUser\Root" | Out-Null
    if (-not (Test-Path -LiteralPath $trustedRootStorePath)) {
        throw "The temporary NosGM development root could not be installed for the current user."
    }
    $installedRootByTest = $true
}

$environmentVariableNames = @(
    "NOSGM_AUTH_GRPC_SERVER_CERT_PATH",
    "NOSGM_AUTH_GRPC_SERVER_CERT_PASSWORD",
    "NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH",
    "NOSGM_AUTH_GRPC_AUTHBRIDGE_CERT_SHA256",
    "NOSGM_AUTH_GRPC_LOGIN_CERT_SHA256",
    "NOSGM_AUTH_GRPC_WORLD_CERT_SHA256",
    "NOSGM_AUTH_GRPC_MASTER_CERT_SHA256",
    "NOSGM_AUTH_GRPC_PORT",
    "NOSGM_AUTH_GRPC_TICKET_TTL_SECONDS",
    "NOSGM_AUTH_GRPC_PERMIT_TTL_SECONDS",
    "NOSGM_AUTH_GRPC_INSTANCE_ID",
    "NOSGM_AUTH_GRPC_URL",
    "NOSGM_AUTH_GRPC_CLIENT_CERT_PATH",
    "NOSGM_AUTH_GRPC_CLIENT_CERT_PASSWORD",
    "NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID",
    "NOSGM_AUTH_GRPC_DEADLINE_MILLISECONDS",
    "NOSGM_AUTH_GRPC_WIRE_MODE",
    "NOSGM_CONFIGURATION_ACCEPTANCE_MARKER"
)
$previousEnvironment = @{}
foreach ($name in $environmentVariableNames) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
}

function Set-ProcessEnvironment {
    param([Parameter(Mandatory = $true)][Collections.IDictionary]$Values)

    foreach ($name in $environmentVariableNames) {
        [Environment]::SetEnvironmentVariable($name, $null, "Process")
    }
    foreach ($entry in $Values.GetEnumerator()) {
        if ($environmentVariableNames -notcontains [string]$entry.Key) {
            throw "Process environment variable is not allow-listed: $($entry.Key)"
        }
        [Environment]::SetEnvironmentVariable([string]$entry.Key, [string]$entry.Value, "Process")
    }
}

function Restore-ProcessEnvironment {
    foreach ($name in $environmentVariableNames) {
        [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], "Process")
    }
}

function Write-AcceptanceClient {
    New-Item -ItemType Directory -Force -Path $clientRoot | Out-Null
    $projectReference = [Security.SecurityElement]::Escape($authenticationClientProject)
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net481</TargetFramework>
    <AssemblyName>NosGm.Configuration.Grpc.Acceptance</AssemblyName>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$projectReference" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $clientProject -Encoding UTF8

    @'
using System;
using System.Globalization;
using System.Threading;
using NosGm.Authentication.Client;
using NosGm.Authentication.Client.Configuration;
using NosGm.Cluster.Contracts.V1;

internal static class Program
{
    private static int Main()
    {
        try
        {
            Run();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("[FAIL] " + exception);
            return 1;
        }
    }

    private static void Run()
    {
        long marker = long.Parse(
            Environment.GetEnvironmentVariable("NOSGM_CONFIGURATION_ACCEPTANCE_MARKER"),
            CultureInfo.InvariantCulture);
        AuthenticationGrpcClientOptions options =
            AuthenticationGrpcClientOptions.Load(ClusterNodeRole.World);

        ConfigurationTransportResult baseline;
        using (var first = new GrpcClusterConfigurationTransport(options))
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        {
            baseline = first.GetAsync(timeout.Token).GetAwaiter().GetResult();
            if (baseline.Result != ConfigurationTransportResultCode.Unavailable &&
                baseline.Result != ConfigurationTransportResultCode.Success)
            {
                throw new InvalidOperationException("Unexpected baseline result: " + baseline.Result);
            }

            var snapshot = NewSnapshot(marker);
            ConfigurationTransportResult seeded =
                first.UpdateAsync(snapshot, timeout.Token).GetAwaiter().GetResult();
            AssertEqual(ConfigurationTransportResultCode.Success, seeded.Result, "seed result");
            AssertEqual(checked(baseline.Generation + 1UL), seeded.Generation, "seed generation");
            AssertSnapshot(snapshot, seeded.Configuration, "seed snapshot");
        }

        using (var reconnect = new GrpcClusterConfigurationTransport(options))
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        {
            var snapshot = NewSnapshot(marker);
            ConfigurationTransportResult reread =
                reconnect.GetAsync(timeout.Token).GetAwaiter().GetResult();
            AssertEqual(ConfigurationTransportResultCode.Success, reread.Result, "reconnect get result");
            AssertEqual(checked(baseline.Generation + 1UL), reread.Generation, "reconnect generation");
            AssertSnapshot(snapshot, reread.Configuration, "reconnect snapshot");

            ConfigurationTransportResult duplicate =
                reconnect.UpdateAsync(snapshot, timeout.Token).GetAwaiter().GetResult();
            AssertEqual(ConfigurationTransportResultCode.Success, duplicate.Result, "duplicate result");
            AssertEqual(reread.Generation, duplicate.Generation, "duplicate preserves generation");

            var changed = NewSnapshot(marker + 1L);
            ConfigurationTransportResult updated =
                reconnect.UpdateAsync(changed, timeout.Token).GetAwaiter().GetResult();
            AssertEqual(ConfigurationTransportResultCode.Success, updated.Result, "changed result");
            AssertEqual(checked(reread.Generation + 1UL), updated.Generation, "changed generation");
            AssertSnapshot(changed, updated.Configuration, "changed snapshot");

            ConfigurationTransportResult final =
                reconnect.GetAsync(timeout.Token).GetAwaiter().GetResult();
            AssertEqual(updated.Generation, final.Generation, "final generation");
            AssertSnapshot(changed, final.Configuration, "final snapshot");
        }

        Console.WriteLine("[PASS] Configuration gRPC shadow transport acceptance");
    }

    private static ConfigurationTransportSnapshot NewSnapshot(long marker)
    {
        return new ConfigurationTransportSnapshot
        {
            MaxGold = marker,
            TimeExpBuffUnixTimeMilliseconds = 1_700_000_000_000L + marker,
            TimeGoldBuffUnixTimeMilliseconds = 1_700_100_000_000L + marker
        };
    }

    private static void AssertSnapshot(
        ConfigurationTransportSnapshot expected,
        ConfigurationTransportSnapshot actual,
        string name)
    {
        if (actual == null ||
            actual.MaxGold != expected.MaxGold ||
            actual.TimeExpBuffUnixTimeMilliseconds != expected.TimeExpBuffUnixTimeMilliseconds ||
            actual.TimeGoldBuffUnixTimeMilliseconds != expected.TimeGoldBuffUnixTimeMilliseconds)
        {
            throw new InvalidOperationException(name + " mismatch.");
        }
        Console.WriteLine("[PASS] " + name);
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!object.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', received '" + actual + "'.");
        }
        Console.WriteLine("[PASS] " + name);
    }
}
'@ | Set-Content -LiteralPath $clientSource -Encoding UTF8
}

$runtimeProcess = $null
try {
    Write-AcceptanceClient
    if (-not $SkipBuild) {
        New-Item -ItemType Directory -Force -Path $runtimeOutput | Out-Null
        & $dotnet publish $runtimeProject --configuration Release --output $runtimeOutput --nologo
        if ($LASTEXITCODE -ne 0) {
            throw "Configuration cluster runtime publish failed."
        }
        & $dotnet build $clientProject --configuration Release --nologo /p:NosGmLegacyBuild=true
        if ($LASTEXITCODE -ne 0) {
            throw "Configuration net481 acceptance client build failed."
        }
    }
    foreach ($required in @($runtimeAssembly, $clientExecutable)) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
            throw "A required Configuration acceptance binary is missing: $required"
        }
    }

    Set-ProcessEnvironment -Values @{
        NOSGM_AUTH_GRPC_SERVER_CERT_PATH = [string]$manifest.ServerCertificatePath
        NOSGM_AUTH_GRPC_SERVER_CERT_PASSWORD = ConvertFrom-SecureStringInMemory $credentials.Server
        NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH = $rootCertificatePath
        NOSGM_AUTH_GRPC_AUTHBRIDGE_CERT_SHA256 = [string]$manifest.Clients.AuthBridge.Sha256
        NOSGM_AUTH_GRPC_LOGIN_CERT_SHA256 = [string]$manifest.Clients.Login.Sha256
        NOSGM_AUTH_GRPC_WORLD_CERT_SHA256 = [string]$manifest.Clients.World.Sha256
        NOSGM_AUTH_GRPC_MASTER_CERT_SHA256 = [string]$manifest.Clients.Master.Sha256
        NOSGM_AUTH_GRPC_PORT = [string]$Port
        NOSGM_AUTH_GRPC_TICKET_TTL_SECONDS = "120"
        NOSGM_AUTH_GRPC_PERMIT_TTL_SECONDS = "120"
        NOSGM_AUTH_GRPC_INSTANCE_ID = "configuration-shadow-acceptance-1"
    }
    $runtimeProcess = Start-Process -FilePath $dotnet -ArgumentList @($runtimeAssembly) -WorkingDirectory $runtimeOutput -NoNewWindow -PassThru
    Restore-ProcessEnvironment
    Wait-ConfigurationRuntime -Process $runtimeProcess

    $worldPassword = ConvertFrom-SecureStringInMemory $credentials.World
    $clientBase = @{
        NOSGM_AUTH_GRPC_URL = "https://127.0.0.1:$Port"
        NOSGM_AUTH_GRPC_CLIENT_CERT_PATH = [string]$manifest.Clients.World.CertificatePath
        NOSGM_AUTH_GRPC_CLIENT_CERT_PASSWORD = $worldPassword
        NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID = "configuration-shadow-world-1"
        NOSGM_AUTH_GRPC_DEADLINE_MILLISECONDS = "10000"
    }

    $modes = New-Object System.Collections.Generic.List[string]
    $modes.Add("GRPCWEB")
    if ($supportsHttp2) {
        $modes.Add("HTTP2")
    }
    foreach ($mode in $modes) {
        $marker = if ($mode -eq "GRPCWEB") { "2100000100" } else { "2100000200" }
        $values = @{}
        foreach ($entry in $clientBase.GetEnumerator()) {
            $values[$entry.Key] = $entry.Value
        }
        $values["NOSGM_AUTH_GRPC_WIRE_MODE"] = $mode
        $values["NOSGM_CONFIGURATION_ACCEPTANCE_MARKER"] = $marker
        Set-ProcessEnvironment -Values $values
        Write-Host "[TEST] Configuration shadow transport over $mode"
        & $clientExecutable
        if ($LASTEXITCODE -ne 0) {
            throw "Configuration shadow transport acceptance failed for $mode."
        }
        Restore-ProcessEnvironment
    }

    if (-not $supportsHttp2) {
        Write-Host "[SKIP] Native HTTP/2 is unavailable to net481 callers on this Windows version; GRPCWEB acceptance passed." -ForegroundColor Yellow
    }

    Write-Host "NosGM Configuration gRPC shadow transport acceptance passed." -ForegroundColor Green
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
    if ($installedRootByTest -and (Test-Path -LiteralPath $trustedRootStorePath)) {
        Remove-Item -LiteralPath $trustedRootStorePath -Force
        Write-Host "[CLEANUP] Removed temporary NosGM root from Cert:\CurrentUser\Root." -ForegroundColor Green
    }
}
