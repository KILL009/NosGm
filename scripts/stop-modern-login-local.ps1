[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$statePath = Join-Path $root "artifacts\modern-login-local\processes.json"

if (-not (Test-Path -LiteralPath $statePath -PathType Leaf)) {
    Write-Host "No NosGM modern Login local stack state was found."
    return
}

$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
if ($state.SchemaVersion -ne 1 -or $null -eq $state.Processes) {
    throw "The modern Login local process state is invalid."
}

$records = @($state.Processes)
[Array]::Reverse($records)

foreach ($record in $records) {
    $process = Get-Process -Id ([int]$record.Id) -ErrorAction SilentlyContinue
    if ($null -eq $process) {
        Write-Host "[DONE] $($record.Name) PID=$($record.Id) was already stopped."
        continue
    }

    $actualStartedAtUtc = $process.StartTime.ToUniversalTime()
    $expectedStartedAtUtc = [DateTime]::Parse(
        [string]$record.StartedAtUtc,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    $difference = [Math]::Abs(($actualStartedAtUtc - $expectedStartedAtUtc).TotalSeconds)

    if ($process.ProcessName -ne [string]$record.ProcessName -or $difference -gt 2) {
        Write-Warning "Skipped PID $($record.Id): it no longer matches the process recorded for $($record.Name)."
        continue
    }

    Stop-Process -Id $process.Id -Force
    Write-Host "[STOP] $($record.Name) PID=$($process.Id)"
}

Remove-Item -LiteralPath $statePath -Force
Write-Host "NosGM modern Login local stack stopped." -ForegroundColor Green
