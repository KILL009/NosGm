[CmdletBinding()]
param(
    [string]$CertificateManifest,
    [switch]$SkipBuild,
    [ValidateRange(1024, 65535)]
    [int]$Port = 7443,
    [ValidateRange(10, 120)]
    [int]$StartupTimeoutSeconds = 45,
    [ValidateRange(15, 180)]
    [int]$ClientTimeoutSeconds = 60,
    [ValidateRange(30, 300)]
    [int]$BuildTimeoutSeconds = 180
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
$clientLogRoot = Join-Path $acceptanceRoot "client-logs"
$buildLogRoot = Join-Path $acceptanceRoot "build-logs"
$processWrapperPath = Join-Path $acceptanceRoot "invoke-process-with-exit-code.ps1"
$clientProject = Join-Path $clientRoot "NosGm.Configuration.Grpc.Acceptance.csproj"
$clientSource = Join-Path $clientRoot "Program.cs"
$clientExecutable = Join-Path $clientRoot "bin\Release\net481\NosGm.Configuration.Grpc.Acceptance.exe"
$runtimeProject = Join-Path $root "Data\NosGm.Program\NosGm.Authentication.Server\NosGm.Authentication.Server.csproj"
$runtimeAssembly = Join-Path $runtimeOutput "NosGm.Authentication.Server.dll"
$authenticationClientProject = Join-Path $root "Data\NosGm.Authentication.Client\NosGm.Authentication.Client.csproj"
$authenticationClientOutput = Join-Path $root "Data\NosGm.Authentication.Client\bin\Release\net481"
$authenticationClientAssembly = Join-Path $authenticationClientOutput "NosGm.Authentication.Client.dll"
$clusterContractsAssembly = Join-Path $authenticationClientOutput "NosGm.Cluster.Contracts.dll"

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

function Read-AcceptanceProcessLog {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return "<no output>"
    }
    $content = Get-Content -LiteralPath $Path -Raw -ErrorAction SilentlyContinue
    if ([string]::IsNullOrWhiteSpace($content)) {
        return "<no output>"
    }
    return $content.Trim()
}

function Write-ProcessExitCodeWrapper {
    New-Item -ItemType Directory -Force -Path $acceptanceRoot | Out-Null
    @'
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ExecutablePath,
    [Parameter(Mandatory = $true)][string]$WorkingDirectory,
    [Parameter(Mandatory = $true)][string]$ExitCodePath,
    [Parameter(Mandatory = $true)][string]$ArgumentPayload
)

$ErrorActionPreference = "Stop"
$exitCode = 1
try {
    Set-Location -LiteralPath $WorkingDirectory
    $json = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($ArgumentPayload))
    $decoded = ConvertFrom-Json -InputObject $json
    $arguments = @()
    if ($null -ne $decoded) {
        $arguments = @($decoded | ForEach-Object { [string]$_ })
    }
    & $ExecutablePath @arguments
    if ($null -ne $LASTEXITCODE) {
        $exitCode = [int]$LASTEXITCODE
    }
    elseif ($?) {
        $exitCode = 0
    }
}
catch {
    [Console]::Error.WriteLine($_.Exception.ToString())
    $exitCode = 1
}
finally {
    [IO.File]::WriteAllText($ExitCodePath, $exitCode.ToString([Globalization.CultureInfo]::InvariantCulture))
}
exit $exitCode
'@ | Set-Content -LiteralPath $processWrapperPath -Encoding UTF8
}

function Quote-ProcessArgument {
    param([Parameter(Mandatory = $true)][string]$Value)

    return '"' + $Value.Replace('"', '\"') + '"'
}

function Start-TrackedProcess {
    param(
        [Parameter(Mandatory = $true)][string]$ExecutablePath,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [Parameter(Mandatory = $true)][string]$ExitCodePath,
        [Parameter(Mandatory = $true)][string]$StandardOutputPath,
        [Parameter(Mandatory = $true)][string]$StandardErrorPath
    )

    $argumentJson = if ($Arguments.Count -eq 0) {
        "[]"
    }
    else {
        ConvertTo-Json -InputObject @($Arguments) -Compress
    }
    $argumentPayload = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($argumentJson))
    $windowsPowerShell = Join-Path $PSHOME "powershell.exe"
    if (-not (Test-Path -LiteralPath $windowsPowerShell -PathType Leaf)) {
        throw "Windows PowerShell process wrapper is missing: $windowsPowerShell"
    }

    return Start-Process `
        -FilePath $windowsPowerShell `
        -ArgumentList @(
            "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass",
            "-File", (Quote-ProcessArgument $processWrapperPath),
            "-ExecutablePath", (Quote-ProcessArgument $ExecutablePath),
            "-WorkingDirectory", (Quote-ProcessArgument $WorkingDirectory),
            "-ExitCodePath", (Quote-ProcessArgument $ExitCodePath),
            "-ArgumentPayload", $argumentPayload
        ) `
        -WindowStyle Hidden `
        -PassThru `
        -RedirectStandardOutput $StandardOutputPath `
        -RedirectStandardError $StandardErrorPath
}

function Read-TrackedProcessExitCode {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Tracked child process did not record an exit code: $Path"
    }
    $raw = (Get-Content -LiteralPath $Path -Raw).Trim()
    $value = 0
    if (-not [int]::TryParse($raw, [Globalization.NumberStyles]::Integer,
        [Globalization.CultureInfo]::InvariantCulture, [ref]$value)) {
        throw "Tracked child process recorded an invalid exit code '$raw': $Path"
    }
    return $value
}

function Stop-ProcessTree {
    param([Parameter(Mandatory = $true)][Diagnostics.Process]$Process)

    if ($Process.HasExited) {
        return
    }

    $taskKill = Join-Path $env:SystemRoot "System32\taskkill.exe"
    if (Test-Path -LiteralPath $taskKill -PathType Leaf) {
        & $taskKill /PID $Process.Id /T /F 2>$null | Out-Null
    }
    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-BoundedDotNet {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    New-Item -ItemType Directory -Force -Path $buildLogRoot | Out-Null
    $safeName = ($Name -replace '[^A-Za-z0-9_.-]', '-').ToLowerInvariant()
    $stdoutPath = Join-Path $buildLogRoot "$safeName.stdout.log"
    $stderrPath = Join-Path $buildLogRoot "$safeName.stderr.log"
    $exitCodePath = Join-Path $buildLogRoot "$safeName.exitcode"
    Remove-Item -LiteralPath $stdoutPath, $stderrPath, $exitCodePath -Force -ErrorAction SilentlyContinue

    Write-Host "[BUILD] $Name"
    $process = Start-TrackedProcess `
        -ExecutablePath $dotnet `
        -Arguments $Arguments `
        -WorkingDirectory $root `
        -ExitCodePath $exitCodePath `
        -StandardOutputPath $stdoutPath `
        -StandardErrorPath $stderrPath
    try {
        $completed = $process.WaitForExit($BuildTimeoutSeconds * 1000)
        if (-not $completed) {
            Stop-ProcessTree -Process $process
            $process.WaitForExit(5000) | Out-Null
            $stdout = Read-AcceptanceProcessLog -Path $stdoutPath
            $stderr = Read-AcceptanceProcessLog -Path $stderrPath
            throw "$Name timed out after $BuildTimeoutSeconds seconds.`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
        }

        $process.WaitForExit()
        $exitCode = Read-TrackedProcessExitCode -Path $exitCodePath
        $stdout = Read-AcceptanceProcessLog -Path $stdoutPath
        $stderr = Read-AcceptanceProcessLog -Path $stderrPath
        if ($stdout -ne "<no output>") {
            Write-Host $stdout
        }
        if ($stderr -ne "<no output>") {
            Write-Host $stderr -ForegroundColor Yellow
        }
        if ($exitCode -ne 0) {
            throw "$Name failed with exit code $exitCode.`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
        }
        Write-Host "[PASS] $Name" -ForegroundColor Green
    }
    finally {
        $process.Dispose()
    }
}

function Invoke-AcceptanceClient {
    param([Parameter(Mandatory = $true)][string]$Mode)

    New-Item -ItemType Directory -Force -Path $clientLogRoot | Out-Null
    $modeName = $Mode.ToLowerInvariant()
    $stdoutPath = Join-Path $clientLogRoot "$modeName.stdout.log"
    $stderrPath = Join-Path $clientLogRoot "$modeName.stderr.log"
    $exitCodePath = Join-Path $clientLogRoot "$modeName.exitcode"
    Remove-Item -LiteralPath $stdoutPath, $stderrPath, $exitCodePath -Force -ErrorAction SilentlyContinue

    $process = $null
    try {
        $process = Start-TrackedProcess `
            -ExecutablePath $clientExecutable `
            -Arguments @() `
            -WorkingDirectory (Split-Path -Parent $clientExecutable) `
            -ExitCodePath $exitCodePath `
            -StandardOutputPath $stdoutPath `
            -StandardErrorPath $stderrPath
    }
    finally {
        # The child already inherited its environment. Do not keep certificate
        # passwords or transport settings in the PowerShell process while it runs.
        Restore-ProcessEnvironment
    }

    if ($null -eq $process) {
        throw "Configuration shadow transport acceptance could not start for $Mode."
    }

    try {
        $completed = $process.WaitForExit($ClientTimeoutSeconds * 1000)
        if (-not $completed) {
            Stop-ProcessTree -Process $process
            $process.WaitForExit(5000) | Out-Null
            $stdout = Read-AcceptanceProcessLog -Path $stdoutPath
            $stderr = Read-AcceptanceProcessLog -Path $stderrPath
            throw "Configuration shadow transport acceptance timed out after $ClientTimeoutSeconds seconds for $Mode.`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
        }

        $process.WaitForExit()
        $exitCode = Read-TrackedProcessExitCode -Path $exitCodePath
        $stdout = Read-AcceptanceProcessLog -Path $stdoutPath
        $stderr = Read-AcceptanceProcessLog -Path $stderrPath
        if ($stdout -ne "<no output>") {
            Write-Host $stdout
        }
        if ($stderr -ne "<no output>") {
            Write-Host $stderr -ForegroundColor Yellow
        }
        if ($exitCode -ne 0) {
            throw "Configuration shadow transport acceptance failed for $Mode with exit code $exitCode.`nSTDOUT:`n$stdout`nSTDERR:`n$stderr"
        }
    }
    finally {
        $process.Dispose()
    }
}

function Write-AcceptanceClient {
    New-Item -ItemType Directory -Force -Path $clientRoot | Out-Null

    if ((Test-Path -LiteralPath $authenticationClientAssembly -PathType Leaf) -and
        (Test-Path -LiteralPath $clusterContractsAssembly -PathType Leaf)) {
        $authenticationClientReference = [Security.SecurityElement]::Escape($authenticationClientAssembly)
        $clusterContractsReference = [Security.SecurityElement]::Escape($clusterContractsAssembly)
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net481</TargetFramework>
    <AssemblyName>NosGm.Configuration.Grpc.Acceptance</AssemblyName>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="NosGm.Authentication.Client">
      <HintPath>$authenticationClientReference</HintPath>
      <Private>true</Private>
    </Reference>
    <Reference Include="NosGm.Cluster.Contracts">
      <HintPath>$clusterContractsReference</HintPath>
      <Private>true</Private>
    </Reference>
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $clientProject -Encoding UTF8
    }
    else {
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
    }

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
            Console.Error.Flush();
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
        Trace("create first transport");
        using (var first = new GrpcClusterConfigurationTransport(options))
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        {
            Trace("baseline get");
            baseline = first.GetAsync(timeout.Token).GetAwaiter().GetResult();
            Trace("baseline get completed: " + baseline.Result);
            if (baseline.Result != ConfigurationTransportResultCode.Unavailable &&
                baseline.Result != ConfigurationTransportResultCode.Success)
            {
                throw new InvalidOperationException("Unexpected baseline result: " + baseline.Result);
            }

            var snapshot = NewSnapshot(marker);
            Trace("seed update");
            ConfigurationTransportResult seeded =
                first.UpdateAsync(snapshot, timeout.Token).GetAwaiter().GetResult();
            Trace("seed update completed");
            AssertEqual(ConfigurationTransportResultCode.Success, seeded.Result, "seed result");
            AssertEqual(checked(baseline.Generation + 1UL), seeded.Generation, "seed generation");
            AssertSnapshot(snapshot, seeded.Configuration, "seed snapshot");
        }
        Trace("first transport disposed");

        Trace("create reconnect transport");
        using (var reconnect = new GrpcClusterConfigurationTransport(options))
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        {
            var snapshot = NewSnapshot(marker);
            Trace("reconnect get");
            ConfigurationTransportResult reread =
                reconnect.GetAsync(timeout.Token).GetAwaiter().GetResult();
            Trace("reconnect get completed");
            AssertEqual(ConfigurationTransportResultCode.Success, reread.Result, "reconnect get result");
            AssertEqual(checked(baseline.Generation + 1UL), reread.Generation, "reconnect generation");
            AssertSnapshot(snapshot, reread.Configuration, "reconnect snapshot");

            Trace("duplicate update");
            ConfigurationTransportResult duplicate =
                reconnect.UpdateAsync(snapshot, timeout.Token).GetAwaiter().GetResult();
            Trace("duplicate update completed");
            AssertEqual(ConfigurationTransportResultCode.Success, duplicate.Result, "duplicate result");
            AssertEqual(reread.Generation, duplicate.Generation, "duplicate preserves generation");

            var changed = NewSnapshot(marker + 1L);
            Trace("changed update");
            ConfigurationTransportResult updated =
                reconnect.UpdateAsync(changed, timeout.Token).GetAwaiter().GetResult();
            Trace("changed update completed");
            AssertEqual(ConfigurationTransportResultCode.Success, updated.Result, "changed result");
            AssertEqual(checked(reread.Generation + 1UL), updated.Generation, "changed generation");
            AssertSnapshot(changed, updated.Configuration, "changed snapshot");

            Trace("final get");
            ConfigurationTransportResult final =
                reconnect.GetAsync(timeout.Token).GetAwaiter().GetResult();
            Trace("final get completed");
            AssertEqual(updated.Generation, final.Generation, "final generation");
            AssertSnapshot(changed, final.Configuration, "final snapshot");
        }
        Trace("reconnect transport disposed");

        Console.WriteLine("[PASS] Configuration gRPC shadow transport acceptance");
        Console.Out.Flush();
    }

    private static void Trace(string message)
    {
        Console.WriteLine("[STEP] " + message);
        Console.Out.Flush();
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
        Console.Out.Flush();
    }

    private static void AssertEqual<T>(T expected, T actual, string name)
    {
        if (!object.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                name + ": expected '" + expected + "', received '" + actual + "'.");
        }
        Console.WriteLine("[PASS] " + name);
        Console.Out.Flush();
    }
}
'@ | Set-Content -LiteralPath $clientSource -Encoding UTF8
}

$runtimeProcess = $null
try {
    Write-ProcessExitCodeWrapper
    Write-AcceptanceClient
    if (-not $SkipBuild) {
        New-Item -ItemType Directory -Force -Path $runtimeOutput | Out-Null
        Invoke-BoundedDotNet `
            -Name "Configuration runtime publish" `
            -Arguments @(
                "publish",
                "`"$runtimeProject`"",
                "--configuration", "Release",
                "--output", "`"$runtimeOutput`"",
                "--nologo"
            )

        if ((Test-Path -LiteralPath $authenticationClientAssembly -PathType Leaf) -and
            (Test-Path -LiteralPath $clusterContractsAssembly -PathType Leaf)) {
            Write-Host "[REUSE] Using the net481 bridge outputs already validated by the .NET 10 foundation build." -ForegroundColor DarkCyan
            Invoke-BoundedDotNet `
                -Name "Configuration net481 client restore" `
                -Arguments @(
                    "restore",
                    "`"$clientProject`"",
                    "--nologo",
                    "/p:RestoreRecursive=false",
                    "/p:NosGmLegacyBuild=true"
                )
            Invoke-BoundedDotNet `
                -Name "Configuration net481 client build" `
                -Arguments @(
                    "build",
                    "`"$clientProject`"",
                    "--configuration", "Release",
                    "--framework", "net481",
                    "--no-restore",
                    "--no-dependencies",
                    "--nologo",
                    "-m:1",
                    "-nodeReuse:false",
                    "/p:NosGmLegacyBuild=true"
                )

            $clientOutput = Split-Path -Parent $clientExecutable
            Copy-Item `
                -Path (Join-Path $authenticationClientOutput "*") `
                -Destination $clientOutput `
                -Recurse `
                -Force
        }
        else {
            Write-Host "[BUILD] net481 bridge output was not present; using the standalone dependency build path." -ForegroundColor DarkCyan
            Invoke-BoundedDotNet `
                -Name "Configuration net481 client build" `
                -Arguments @(
                    "build",
                    "`"$clientProject`"",
                    "--configuration", "Release",
                    "--nologo",
                    "/p:NosGmLegacyBuild=true"
                )
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
    $runtimeProcess = Start-Process -FilePath $dotnet -ArgumentList @($runtimeAssembly) -WorkingDirectory $runtimeOutput -WindowStyle Hidden -PassThru
    Restore-ProcessEnvironment
    Wait-ConfigurationRuntime -Process $runtimeProcess

    $worldPassword = ConvertFrom-SecureStringInMemory $credentials.World
    $clientBase = @{
        NOSGM_AUTH_GRPC_URL = "https://127.0.0.1:$Port"
        NOSGM_AUTH_GRPC_CLIENT_CERT_PATH = [string]$manifest.Clients.World.CertificatePath
        NOSGM_AUTH_GRPC_CLIENT_CERT_PASSWORD = $worldPassword
        NOSGM_AUTH_GRPC_TRUSTED_ROOT_CERT_PATH = $rootCertificatePath
        NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID = "configuration-shadow-world-1"
        NOSGM_AUTH_GRPC_DEADLINE_MILLISECONDS = "10000"
    }

    if (-not $supportsHttp2) {
        throw "The live net481 Configuration acceptance requires native HTTP/2 on Windows 11 or Windows Server 2019 and later. The production GRPCWEB fallback remains available for Windows 10."
    }

    $values = @{}
    foreach ($entry in $clientBase.GetEnumerator()) {
        $values[$entry.Key] = $entry.Value
    }
    $values["NOSGM_AUTH_GRPC_WIRE_MODE"] = "HTTP2"
    $values["NOSGM_CONFIGURATION_ACCEPTANCE_MARKER"] = "2100000200"
    Set-ProcessEnvironment -Values $values
    Write-Host "[TEST] Configuration shadow transport over native HTTP2"
    Invoke-AcceptanceClient -Mode "HTTP2"

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
}
