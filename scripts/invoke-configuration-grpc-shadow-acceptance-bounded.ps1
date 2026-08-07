[CmdletBinding()]
param(
    [ValidateRange(60, 540)]
    [int]$TotalTimeoutSeconds = 420
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

if ($env:OS -ne "Windows_NT") {
    throw "The Configuration gRPC shadow acceptance supervisor requires Windows."
}

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$acceptancePath = Join-Path $root "scripts\test-configuration-grpc-shadow-local.ps1"
$logRoot = Join-Path $root "artifacts\configuration-grpc-shadow-acceptance\supervisor-logs"
$stdoutPath = Join-Path $logRoot "acceptance.stdout.log"
$stderrPath = Join-Path $logRoot "acceptance.stderr.log"
$powershell = Join-Path $PSHOME "powershell.exe"

if (-not (Test-Path -LiteralPath $acceptancePath -PathType Leaf)) {
    throw "Configuration shadow acceptance script is missing: $acceptancePath"
}
if (-not (Test-Path -LiteralPath $powershell -PathType Leaf)) {
    throw "Windows PowerShell executable is missing: $powershell"
}

function Read-LogTail {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return "<no output>"
    }

    $lines = @(Get-Content -LiteralPath $Path -Tail 200 -ErrorAction SilentlyContinue)
    if ($lines.Count -eq 0) {
        return "<no output>"
    }
    return ($lines -join [Environment]::NewLine).Trim()
}

function Stop-BoundedProcessTree {
    param([Parameter(Mandatory = $true)][Diagnostics.Process]$Process)

    try {
        if ($Process.HasExited) {
            return
        }
    }
    catch {
        return
    }

    $taskKill = Join-Path $env:SystemRoot "System32\taskkill.exe"
    if (Test-Path -LiteralPath $taskKill -PathType Leaf) {
        $killer = $null
        try {
            $killer = Start-Process `
                -FilePath $taskKill `
                -ArgumentList @("/PID", [string]$Process.Id, "/T", "/F") `
                -NoNewWindow `
                -PassThru
            if (-not $killer.WaitForExit(10000)) {
                Stop-Process -Id $killer.Id -Force -ErrorAction SilentlyContinue
                $killer.WaitForExit(2000) | Out-Null
            }
        }
        catch {
            Write-Warning "Bounded taskkill cleanup failed: $($_.Exception.Message)"
        }
        finally {
            if ($null -ne $killer) {
                $killer.Dispose()
            }
        }
    }

    try {
        if (-not $Process.HasExited) {
            Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Warning "Fallback process cleanup failed: $($_.Exception.Message)"
    }
}

New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue

$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$process = $null
try {
    Write-Host "[BUDGET] Configuration shadow acceptance total budget: $TotalTimeoutSeconds seconds"
    Write-Host "[SUPERVISOR] Starting isolated acceptance harness"

    $quotedAcceptancePath = '"' + $acceptancePath + '"'
    $process = Start-Process `
        -FilePath $powershell `
        -ArgumentList @("-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", $quotedAcceptancePath) `
        -WorkingDirectory $root `
        -NoNewWindow `
        -PassThru `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath

    $completed = $process.WaitForExit($TotalTimeoutSeconds * 1000)
    if (-not $completed) {
        Stop-BoundedProcessTree -Process $process
        $process.WaitForExit(5000) | Out-Null
        $stdout = Read-LogTail -Path $stdoutPath
        $stderr = Read-LogTail -Path $stderrPath
        throw "Configuration shadow acceptance exceeded its total $TotalTimeoutSeconds-second budget after $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)) seconds.`nLast STDOUT:`n$stdout`nLast STDERR:`n$stderr"
    }

    $exitCode = $process.ExitCode
    $stdout = Read-LogTail -Path $stdoutPath
    $stderr = Read-LogTail -Path $stderrPath
    if ($stdout -ne "<no output>") {
        Write-Host $stdout
    }
    if ($stderr -ne "<no output>") {
        Write-Host $stderr -ForegroundColor Yellow
    }
    if ($exitCode -ne 0) {
        throw "Configuration shadow acceptance failed with exit code $exitCode after $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)) seconds.`nLast STDOUT:`n$stdout`nLast STDERR:`n$stderr"
    }

    Write-Host "[PASS] Configuration shadow acceptance completed inside the total budget in $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)) seconds." -ForegroundColor Green
}
finally {
    if ($null -ne $process) {
        try {
            if (-not $process.HasExited) {
                Stop-BoundedProcessTree -Process $process
            }
        }
        catch {
            Write-Warning "Supervisor cleanup check failed: $($_.Exception.Message)"
        }
        finally {
            $process.Dispose()
        }
    }
    $stopwatch.Stop()
}
