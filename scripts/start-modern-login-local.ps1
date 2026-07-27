[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$SkipLauncher,
    [switch]$ConfigureUrlAcl,
    [ValidateRange(10, 180)]
    [int]$StartupTimeoutSeconds = 60,
    [ValidateRange(1, 65535)]
    [int]$MasterPort = 4545,
    [ValidateRange(1, 65535)]
    [int]$WorldPort = 1337,
    [ValidateRange(1, 65535)]
    [int]$SpanishLoginPort = 4005,
    [ValidateRange(1, 65535)]
    [int]$BridgePort = 8081
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "The local modern Login stack requires Windows."
}

$root = Split-Path -Parent $PSScriptRoot
$stateDirectory = Join-Path $root "artifacts\modern-login-local"
$statePath = Join-Path $stateDirectory "processes.json"
$startedProcesses = New-Object System.Collections.Generic.List[object]

function New-NosGmSecret {
    $bytes = New-Object byte[] 48
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
        return [Convert]::ToBase64String($bytes)
    }
    finally {
        $generator.Dispose()
        [Array]::Clear($bytes, 0, $bytes.Length)
    }
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Wait-TcpPort {
    param(
        [Parameter(Mandatory = $true)][string]$HostName,
        [Parameter(Mandatory = $true)][int]$Port,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($StartupTimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $client = New-Object Net.Sockets.TcpClient
        try {
            $result = $client.BeginConnect($HostName, $Port, $null, $null)
            if ($result.AsyncWaitHandle.WaitOne(500) -and $client.Connected) {
                $client.EndConnect($result)
                Write-Host "[READY] $Description on $HostName`:$Port"
                return
            }
        }
        catch {
            # The service may still be starting.
        }
        finally {
            $client.Dispose()
        }
        Start-Sleep -Milliseconds 350
    }

    throw "$Description did not listen on $HostName`:$Port within $StartupTimeoutSeconds seconds."
}

function Start-TrackedProcess {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Executable,
        [string[]]$Arguments = @()
    )

    if (-not (Test-Path -LiteralPath $Executable -PathType Leaf)) {
        throw "Missing $Name executable: $Executable"
    }

    $process = Start-Process \
        -FilePath $Executable \
        -ArgumentList $Arguments \
        -WorkingDirectory (Split-Path -Parent $Executable) \
        -PassThru

    $startedAtUtc = $process.StartTime.ToUniversalTime().ToString("O")
    $record = [pscustomobject]@{
        Name = $Name
        Id = $process.Id
        ProcessName = $process.ProcessName
        Executable = $Executable
        StartedAtUtc = $startedAtUtc
    }
    $startedProcesses.Add($record)
    Write-Host "[START] $Name PID=$($process.Id)"
    return $process
}

function Stop-StartedProcesses {
    foreach ($record in @($startedProcesses | Select-Object -Reverse)) {
        try {
            Stop-Process -Id $record.Id -Force -ErrorAction SilentlyContinue
        }
        catch {
            # Best-effort rollback after failed startup.
        }
    }
}

function Resolve-MSBuild {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere) {
        $resolved = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
        if ($resolved) {
            return $resolved
        }
    }

    throw "MSBuild was not found. Install Visual Studio Build Tools 2022 or run with -SkipBuild after building the solution."
}

if (Test-Path -LiteralPath $statePath) {
    throw "A local modern Login state file already exists. Run scripts/stop-modern-login-local.ps1 first."
}

$bridgePrefix = "http://127.0.0.1:$BridgePort/"
$launcherEndpoint = $bridgePrefix + "api/v1/launcher/ticket"

if ($ConfigureUrlAcl) {
    if (-not (Test-IsAdministrator)) {
        throw "-ConfigureUrlAcl requires an elevated PowerShell window."
    }

    & netsh http delete urlacl url=$bridgePrefix *> $null
    & netsh http add urlacl url=$bridgePrefix user="$env:USERDOMAIN\$env:USERNAME" | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to configure the HttpListener URL ACL for $bridgePrefix"
    }
}

if (-not $SkipBuild) {
    $nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue
    if (-not $nuget) {
        throw "nuget.exe was not found. Install NuGet CLI or run with -SkipBuild after restoring the solution."
    }

    $msbuild = Resolve-MSBuild
    Write-Host "[BUILD] Restoring NosGm.sln"
    & $nuget.Source restore (Join-Path $root "NosGm.sln") -NonInteractive
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet restore failed."
    }

    Write-Host "[BUILD] Building server Release / Any CPU"
    & $msbuild (Join-Path $root "NosGm.sln") /t:Build /m /nologo /nr:false /v:minimal /p:Configuration=Release "/p:Platform=Any CPU"
    if ($LASTEXITCODE -ne 0) {
        throw "Server build failed."
    }

    Write-Host "[BUILD] Building launcher Release"
    & dotnet build (Join-Path $root "Launcher\NosGM.Launcher.sln") --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Launcher build failed."
    }
}

$masterExecutable = Join-Path $root "bin\Release\Master\NosGm.Master.Server.exe"
$worldExecutable = Join-Path $root "bin\Release\World\NosGm.World.exe"
$loginExecutable = Join-Path $root "Data\NosGm.Program\NosGm.Login\bin\Release\NosGm.Login.exe"
$launcherExecutable = Join-Path $root "Launcher\src\NosGM.Launcher\bin\Release\net9.0-windows\NosGM.Launcher.exe"

$env:NOSGM_MASTER_AUTH_KEY = New-NosGmSecret
$env:NOSGM_AUTH_SERVICE_KEY = New-NosGmSecret
$env:NOSGM_GAMEFORGE_TICKET_ISSUER_KEY = New-NosGmSecret
$env:NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY = New-NosGmSecret
$env:NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN = "true"
$env:NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE = "true"
$env:NOSGM_START_ALL_REGIONAL_LOGIN_PORTS = "true"
$env:NOSGM_LAUNCHER_AUTH_BRIDGE_PREFIX = $bridgePrefix
$env:NOSGM_GAMEFORGE_AUTH_TICKET_TTL_SECONDS = "120"
$env:NOSGM_GAMEFORGE_WORLD_PERMIT_TTL_SECONDS = "120"
$env:NOSGM_LAUNCHER_AUTH_ATTEMPT_WINDOW_SECONDS = "60"
$env:NOSGM_LAUNCHER_AUTH_MAX_ATTEMPTS_PER_WINDOW = "10"
$env:NOSGM_AUTH_ENDPOINT = $launcherEndpoint

try {
    New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null

    Start-TrackedProcess -Name "Master" -Executable $masterExecutable | Out-Null
    Wait-TcpPort -HostName "127.0.0.1" -Port $MasterPort -Description "Master"
    Wait-TcpPort -HostName "127.0.0.1" -Port $BridgePort -Description "Launcher AuthBridge"

    Start-TrackedProcess -Name "World" -Executable $worldExecutable -Arguments @("--nomsg", "--port", $WorldPort.ToString()) | Out-Null
    Wait-TcpPort -HostName "127.0.0.1" -Port $WorldPort -Description "World"

    Start-TrackedProcess -Name "Login" -Executable $loginExecutable -Arguments @("--nomsg") | Out-Null
    Wait-TcpPort -HostName "127.0.0.1" -Port $SpanishLoginPort -Description "Spanish Login"

    if (-not $SkipLauncher) {
        Start-TrackedProcess -Name "Launcher" -Executable $launcherExecutable | Out-Null
    }

    $state = [pscustomobject]@{
        SchemaVersion = 1
        CreatedAtUtc = [DateTime]::UtcNow.ToString("O")
        AuthenticationEndpoint = $launcherEndpoint
        SpanishLoginPort = $SpanishLoginPort
        WorldPort = $WorldPort
        Processes = @($startedProcesses)
    }
    $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath -Encoding UTF8

    Write-Host ""
    Write-Host "NosGM modern Login local stack is ready." -ForegroundColor Green
    Write-Host "Authentication endpoint: $launcherEndpoint"
    Write-Host "Launcher language: Español (region 5 / Login $SpanishLoginPort)"
    Write-Host "Secrets exist only in the child process environments and were not written to disk."
    Write-Host "Stop the stack with: ./scripts/stop-modern-login-local.ps1"
}
catch {
    Stop-StartedProcesses
    Remove-Item -LiteralPath $statePath -Force -ErrorAction SilentlyContinue
    throw
}
