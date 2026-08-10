[CmdletBinding()]
param(
    [string]$HostName = "127.0.0.1",
    [ValidateRange(1, 65535)]
    [int]$Port = 1337,
    [string]$LoginHostName = "127.0.0.1",
    [ValidateRange(1, 65535)]
    [int]$LoginPort = 4005,
    [string]$AccountsPath = "C:\NosGM-Test\accounts.csv",
    [ValidateRange(0, 9)]
    [int]$Region = 5,
    [string]$Stages = "500,750,1000",
    [ValidateRange(1, 5000)]
    [int]$RampPerSecond = 2,
    [ValidateRange(0, 3600)]
    [int]$HoldSeconds = 60,
    [ValidateRange(100, 120000)]
    [int]$ConnectTimeoutMilliseconds = 5000,
    [ValidateRange(100, 120000)]
    [int]$ReadTimeoutMilliseconds = 30000,
    [ValidateRange(0, 100)]
    [int]$ActiveMovementPercent = 5,
    [ValidateRange(100, 60000)]
    [int]$MovementIntervalMilliseconds = 1000,
    [ValidateRange(0, 32767)]
    [int]$MovementBaseX = 80,
    [ValidateRange(0, 32767)]
    [int]$MovementBaseY = 115,
    [ValidateRange(1, 16)]
    [int]$MovementStep = 1,
    [ValidateRange(1, 32767)]
    [int]$MovementSpeed = 11,
    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($MovementBaseX + $MovementStep -gt 32767) {
    throw "MovementBaseX + MovementStep must not exceed 32767."
}

$runner = Join-Path $PSScriptRoot "run-load-test-local.ps1"
if (-not (Test-Path -LiteralPath $runner -PathType Leaf)) {
    throw "NosGM load-test wrapper was not found: $runner"
}

$environment = [ordered]@{
    NOSGM_LOADTEST_MOVEMENT_PERCENT = $ActiveMovementPercent.ToString([Globalization.CultureInfo]::InvariantCulture)
    NOSGM_LOADTEST_MOVEMENT_INTERVAL_MS = $MovementIntervalMilliseconds.ToString([Globalization.CultureInfo]::InvariantCulture)
    NOSGM_LOADTEST_MOVEMENT_BASE_X = $MovementBaseX.ToString([Globalization.CultureInfo]::InvariantCulture)
    NOSGM_LOADTEST_MOVEMENT_BASE_Y = $MovementBaseY.ToString([Globalization.CultureInfo]::InvariantCulture)
    NOSGM_LOADTEST_MOVEMENT_STEP = $MovementStep.ToString([Globalization.CultureInfo]::InvariantCulture)
    NOSGM_LOADTEST_MOVEMENT_SPEED = $MovementSpeed.ToString([Globalization.CultureInfo]::InvariantCulture)
}

$previous = @{}
foreach ($name in $environment.Keys) {
    $previous[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
    [Environment]::SetEnvironmentVariable($name, $environment[$name], "Process")
}

try {
    Write-Host "[ACTIVE] World movement workload enabled" -ForegroundColor Magenta
    Write-Host "[ACTIVE] movers=$ActiveMovementPercent% interval=${MovementIntervalMilliseconds}ms path=($MovementBaseX,$MovementBaseY)<->($($MovementBaseX + $MovementStep),$MovementBaseY) speed=$MovementSpeed" -ForegroundColor Magenta
    Write-Host "[ACTIVE] stages=$Stages ramp=$RampPerSecond/s hold=${HoldSeconds}s" -ForegroundColor Magenta

    $parameters = @{
        Scenario = "world"
        HostName = $HostName
        Port = $Port
        LoginHostName = $LoginHostName
        LoginPort = $LoginPort
        LoginMode = "Modern"
        AccountsPath = $AccountsPath
        Region = $Region
        Stages = $Stages
        RampPerSecond = $RampPerSecond
        HoldSeconds = $HoldSeconds
        ConnectTimeoutMilliseconds = $ConnectTimeoutMilliseconds
        ReadTimeoutMilliseconds = $ReadTimeoutMilliseconds
    }
    if (-not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
        $parameters["OutputDirectory"] = $OutputDirectory
    }

    & $runner @parameters
    if ($LASTEXITCODE -ne 0) {
        throw "NosGM active World load test failed with exit code $LASTEXITCODE."
    }
}
finally {
    foreach ($name in $environment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], "Process")
    }
}
