[CmdletBinding()]
param(
    [string]$StatePath,
    [string]$OutputPath,
    [switch]$RequireLauncher,
    [switch]$PassThru
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "The modern Login readiness test requires Windows."
}

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($StatePath)) {
    $StatePath = Join-Path $root "artifacts\modern-login-local\processes.json"
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root "artifacts\modern-login-local\readiness.json"
}

$checks = New-Object System.Collections.Generic.List[object]

function Add-Check {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][ValidateSet("passed", "warning", "failed")][string]$Status,
        [Parameter(Mandatory = $true)][string]$Detail
    )

    $checks.Add([pscustomobject]@{
        Name = $Name
        Status = $Status
        Detail = $Detail
    })
}

function Test-TcpPort {
    param(
        [Parameter(Mandatory = $true)][string]$HostName,
        [Parameter(Mandatory = $true)][int]$Port
    )

    $client = New-Object Net.Sockets.TcpClient
    $result = $null
    try {
        $result = $client.BeginConnect($HostName, $Port, $null, $null)
        if (-not $result.AsyncWaitHandle.WaitOne(1500)) {
            return $false
        }
        $client.EndConnect($result)
        return $client.Connected
    }
    catch {
        return $false
    }
    finally {
        if ($null -ne $result) {
            $result.AsyncWaitHandle.Close()
        }
        $client.Dispose()
    }
}

function Test-RecordedProcess {
    param([Parameter(Mandatory = $true)]$Record)

    $process = Get-Process -Id ([int]$Record.Id) -ErrorAction SilentlyContinue
    if ($null -eq $process) {
        Add-Check -Name "Process.$($Record.Name)" -Status "failed" -Detail "Recorded PID $($Record.Id) is not running."
        return
    }

    try {
        $expectedStartedAtUtc = [DateTime]::Parse(
            [string]$Record.StartedAtUtc,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
        $difference = [Math]::Abs(($process.StartTime.ToUniversalTime() - $expectedStartedAtUtc).TotalSeconds)
        if ($process.ProcessName -ne [string]$Record.ProcessName -or $difference -gt 2) {
            Add-Check -Name "Process.$($Record.Name)" -Status "failed" -Detail "PID $($Record.Id) no longer matches the recorded process identity."
            return
        }

        Add-Check -Name "Process.$($Record.Name)" -Status "passed" -Detail "PID $($Record.Id) is running with the expected identity."
    }
    finally {
        $process.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $StatePath -PathType Leaf)) {
    Add-Check -Name "RuntimeState" -Status "failed" -Detail "No local stack state was found. Start the stack first."
    $state = $null
}
else {
    $stateRaw = Get-Content -LiteralPath $StatePath -Raw
    if ($stateRaw -match '(?i)"(password|authorizationCode|ticket|token|secret|masterAuthKey|authServiceKey|gameforgeTicketIssuerKey|gameforgeTicketConsumerKey)"\s*:') {
        Add-Check -Name "RuntimeState.Secrets" -Status "failed" -Detail "The runtime state contains a forbidden credential-shaped property."
    }
    else {
        Add-Check -Name "RuntimeState.Secrets" -Status "passed" -Detail "The runtime state contains no credential-shaped properties."
    }

    try {
        $state = $stateRaw | ConvertFrom-Json
        if ($state.SchemaVersion -ne 1 -or $null -eq $state.Processes) {
            throw "Unsupported state schema."
        }
        Add-Check -Name "RuntimeState" -Status "passed" -Detail "Runtime state schema 1 loaded successfully."
    }
    catch {
        Add-Check -Name "RuntimeState" -Status "failed" -Detail "Runtime state JSON is invalid or unsupported."
        $state = $null
    }
}

if ($null -ne $state) {
    $records = @($state.Processes)
    foreach ($record in $records) {
        Test-RecordedProcess -Record $record
    }

    $launcherRecord = @($records | Where-Object { $_.Name -eq "Launcher" })
    if ($RequireLauncher -and $launcherRecord.Count -eq 0) {
        Add-Check -Name "Process.Launcher" -Status "failed" -Detail "Launcher was required but is not recorded in the local stack."
    }
    elseif (-not $RequireLauncher -and $launcherRecord.Count -eq 0) {
        Add-Check -Name "Process.Launcher" -Status "warning" -Detail "Launcher was intentionally omitted or has not been started by the stack."
    }

    $worldPort = [int]$state.WorldPort
    $portChecks = @(
        [pscustomobject]@{ Name = "Port.Master"; Port = 4545 },
        [pscustomobject]@{ Name = "Port.World"; Port = $worldPort },
        [pscustomobject]@{ Name = "Port.LoginSpanish"; Port = 4005 }
    )

    foreach ($portCheck in $portChecks) {
        if (Test-TcpPort -HostName "127.0.0.1" -Port $portCheck.Port) {
            Add-Check -Name $portCheck.Name -Status "passed" -Detail "127.0.0.1:$($portCheck.Port) accepts TCP connections."
        }
        else {
            Add-Check -Name $portCheck.Name -Status "failed" -Detail "127.0.0.1:$($portCheck.Port) is not accepting TCP connections."
        }
    }

    try {
        $ticketUri = New-Object Uri([string]$state.AuthenticationEndpoint)
        $healthBuilder = New-Object UriBuilder($ticketUri)
        $healthBuilder.Path = "/api/v1/launcher/health"
        $healthBuilder.Query = ""
        $healthBuilder.Fragment = ""
        $healthUri = $healthBuilder.Uri

        if (-not $healthUri.IsLoopback) {
            throw "The local health endpoint is not loopback."
        }

        $health = Invoke-RestMethod -Uri $healthUri -Method Get -TimeoutSec 5
        if ($health.service -ne "NosGM.LauncherAuthBridge" -or
            -not [bool]$health.modernLoginEnabled -or
            -not [bool]$health.bridgeEnabled -or
            [int]$health.regionalLoginCount -ne 10) {
            throw "The health response does not describe a complete modern Login stack."
        }

        if ($health.status -eq "maintenance") {
            Add-Check -Name "AuthBridge.Health" -Status "warning" -Detail "AuthBridge is healthy but the server is in maintenance mode."
        }
        elseif ($health.status -eq "ready") {
            Add-Check -Name "AuthBridge.Health" -Status "passed" -Detail "AuthBridge reports ready with 10 regional Login profiles."
        }
        else {
            Add-Check -Name "AuthBridge.Health" -Status "failed" -Detail "AuthBridge returned an unknown health status."
        }
    }
    catch {
        Add-Check -Name "AuthBridge.Health" -Status "failed" -Detail "The loopback health endpoint could not be validated."
    }
}

$settingsPath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "NosGM\Launcher\settings.json"
if (-not (Test-Path -LiteralPath $settingsPath -PathType Leaf)) {
    $settingsStatus = $RequireLauncher ? "failed" : "warning"
    Add-Check -Name "Launcher.Settings" -Status $settingsStatus -Detail "Launcher settings have not been created yet."
}
else {
    try {
        $settingsRaw = Get-Content -LiteralPath $settingsPath -Raw
        if ($settingsRaw -match '(?i)"(password|authorizationCode|ticket|token|secret)"\s*:') {
            throw "Launcher settings contain a forbidden credential-shaped property."
        }

        $settings = $settingsRaw | ConvertFrom-Json
        Add-Check -Name "Launcher.Settings" -Status "passed" -Detail "Launcher settings are readable and contain no credential-shaped properties."

        $clientExecutable = Join-Path ([string]$settings.InstallRoot) ([string]$settings.GameExecutable)
        if (Test-Path -LiteralPath $clientExecutable -PathType Leaf) {
            $clientItem = Get-Item -LiteralPath $clientExecutable
            $version = $clientItem.VersionInfo.FileVersion
            if ([string]::IsNullOrWhiteSpace($version)) {
                $version = "unknown"
            }
            Add-Check -Name "Client.Executable" -Status "passed" -Detail "Authorized client executable exists; file version $version."
        }
        else {
            Add-Check -Name "Client.Executable" -Status "failed" -Detail "The configured authorized client executable does not exist."
        }

        if ([string]$settings.Language -eq "es") {
            Add-Check -Name "Launcher.Region" -Status "passed" -Detail "Launcher language is Spanish, mapped to region 5 and Login port 4005."
        }
        else {
            Add-Check -Name "Launcher.Region" -Status "warning" -Detail "Launcher language is not Spanish; the first acceptance test should use region 5."
        }
    }
    catch {
        Add-Check -Name "Launcher.Settings" -Status "failed" -Detail "Launcher settings are invalid or contain forbidden data."
    }
}

try {
    $registryValue = Get-ItemPropertyValue -Path "HKCU:\Software\Gameforge4d\TNTClient\MainApp" -Name "InstallationId" -ErrorAction Stop
    $installationId = [Guid]::Empty
    if (-not [Guid]::TryParse([string]$registryValue, [ref]$installationId) -or $installationId -eq [Guid]::Empty) {
        throw "InstallationId is invalid."
    }
    Add-Check -Name "Client.InstallationId" -Status "passed" -Detail "The shared current-user InstallationId is present and valid."
}
catch {
    Add-Check -Name "Client.InstallationId" -Status "warning" -Detail "InstallationId is absent or invalid; the launcher will create it when modern Play begins."
}

$failedCount = @($checks | Where-Object { $_.Status -eq "failed" }).Count
$warningCount = @($checks | Where-Object { $_.Status -eq "warning" }).Count
$overallStatus = "ready"
if ($failedCount -gt 0) {
    $overallStatus = "failed"
}
elseif ($warningCount -gt 0) {
    $overallStatus = "warning"
}

$report = [pscustomobject]@{
    SchemaVersion = 1
    GeneratedAtUtc = [DateTime]::UtcNow.ToString("O")
    OverallStatus = $overallStatus
    FailedChecks = $failedCount
    WarningChecks = $warningCount
    Checks = @($checks)
}

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

foreach ($check in $checks) {
    $prefix = switch ($check.Status) {
        "passed" { "[PASS]" }
        "warning" { "[WARN]" }
        default { "[FAIL]" }
    }
    Write-Host "$prefix $($check.Name): $($check.Detail)"
}

Write-Host "Readiness report: $OutputPath"

if ($PassThru) {
    return $report
}

if ($failedCount -gt 0) {
    throw "Modern Login readiness failed with $failedCount blocking check(s)."
}

if ($warningCount -gt 0) {
    Write-Warning "Modern Login readiness completed with $warningCount warning(s)."
}
else {
    Write-Host "Modern Login is ready for the real-client acceptance test." -ForegroundColor Green
}
