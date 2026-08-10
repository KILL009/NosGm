[CmdletBinding()]
param(
    [ValidateSet("tcp", "login", "world")]
    [string]$Scenario = "tcp",
    [string]$HostName = "127.0.0.1",
    [ValidateRange(0, 65535)]
    [int]$Port = 0,
    [string]$LoginHostName,
    [ValidateRange(0, 65535)]
    [int]$LoginPort = 0,
    [ValidateSet("Modern", "Legacy")]
    [string]$LoginMode = "Modern",
    [string]$AuthBridgeUrl = "http://127.0.0.1:8081/api/v1/launcher/ticket",
    [ValidateSet("NoS0576", "NoS0577")]
    [string]$ModernHeader = "NoS0577",
    [string]$ClientMd5 = "00000000000000000000000000000000",
    [string]$WorldReadyPacket = "finit",
    [string]$Stages = "100,250,500,750,1000,1250,1500",
    [ValidateRange(1, 5000)]
    [int]$RampPerSecond = 100,
    [ValidateRange(0, 3600)]
    [int]$HoldSeconds = 30,
    [ValidateRange(100, 120000)]
    [int]$ConnectTimeoutMilliseconds = 5000,
    [ValidateRange(100, 120000)]
    [int]$ReadTimeoutMilliseconds = 5000,
    [string]$AccountsPath,
    [ValidateRange(0, 9)]
    [int]$Region = 5,
    [string]$ClientVersion = "0.9.3.3254",
    [string]$OutputDirectory,
    [switch]$AllowPublicTarget,
    [switch]$SelfTest
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$project = Join-Path $repositoryRoot "Tools\NosGM.LoadTest\NosGM.LoadTest.csproj"
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "NosGM Load Test project was not found: $project"
}

if ($SelfTest) {
    & dotnet run --project $project -c Release -- --self-test
    if ($LASTEXITCODE -ne 0) {
        throw "NosGM Load Test self-test failed with exit code $LASTEXITCODE."
    }
    exit 0
}

$regionalLoginPort = 4000 + $Region
if ($Port -eq 0) {
    $Port = if ($Scenario -eq "login") { $regionalLoginPort } else { 1337 }
}
if ($LoginPort -eq 0) {
    $LoginPort = $regionalLoginPort
}

if ([string]::IsNullOrWhiteSpace($LoginHostName)) {
    $LoginHostName = $HostName
}

$arguments = @(
    "run",
    "--project", $project,
    "-c", "Release",
    "--",
    "--scenario", $Scenario,
    "--host", $HostName,
    "--port", $Port.ToString([Globalization.CultureInfo]::InvariantCulture),
    "--stages", $Stages,
    "--ramp-per-second", $RampPerSecond.ToString([Globalization.CultureInfo]::InvariantCulture),
    "--hold-seconds", $HoldSeconds.ToString([Globalization.CultureInfo]::InvariantCulture),
    "--connect-timeout-ms", $ConnectTimeoutMilliseconds.ToString([Globalization.CultureInfo]::InvariantCulture),
    "--read-timeout-ms", $ReadTimeoutMilliseconds.ToString([Globalization.CultureInfo]::InvariantCulture),
    "--region", $Region.ToString([Globalization.CultureInfo]::InvariantCulture),
    "--client-version", $ClientVersion,
    "--login-mode", $LoginMode.ToLowerInvariant()
)

if ($Scenario -eq "login" -or $Scenario -eq "world") {
    if ($LoginMode -eq "Modern") {
        $arguments += @(
            "--auth-bridge-url", $AuthBridgeUrl,
            "--modern-header", $ModernHeader,
            "--client-md5", $ClientMd5
        )
    }
}

if ($Scenario -eq "world") {
    $arguments += @(
        "--login-host", $LoginHostName,
        "--login-port", $LoginPort.ToString([Globalization.CultureInfo]::InvariantCulture),
        "--world-ready-packet", $WorldReadyPacket
    )
}

if (-not [string]::IsNullOrWhiteSpace($AccountsPath)) {
    $arguments += @("--accounts", [System.IO.Path]::GetFullPath($AccountsPath))
}

if (-not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $arguments += @("--output", [System.IO.Path]::GetFullPath($OutputDirectory))
}

if ($AllowPublicTarget) {
    $arguments += "--allow-public-target"
}

Write-Host "[LOAD] NosGM $Scenario load test -> ${HostName}:$Port" -ForegroundColor Cyan
if ($Scenario -eq "login" -or $Scenario -eq "world") {
    Write-Host "[LOAD] Login mode=$LoginMode region=$Region" -ForegroundColor Cyan
    if ($LoginMode -eq "Modern") {
        Write-Host "[LOAD] Modern header=$ModernHeader AuthBridge=$AuthBridgeUrl" -ForegroundColor Cyan
    }
}
if ($Scenario -eq "world") {
    Write-Host "[LOAD] Login -> ${LoginHostName}:$LoginPort | World ready packet=$WorldReadyPacket" -ForegroundColor Cyan
}
Write-Host "[LOAD] stages=$Stages ramp=$RampPerSecond/s hold=${HoldSeconds}s connectTimeout=${ConnectTimeoutMilliseconds}ms readTimeout=${ReadTimeoutMilliseconds}ms" -ForegroundColor Cyan

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "NosGM Load Test failed with exit code $LASTEXITCODE."
}
