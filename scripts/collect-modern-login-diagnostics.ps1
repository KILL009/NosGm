[CmdletBinding()]
param(
    [string]$OutputPath,
    [ValidateRange(50, 2000)]
    [int]$MaxLogLines = 400,
    [ValidateRange(1, 50)]
    [int]$MaxLogMegabytes = 8
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($env:OS -ne "Windows_NT") {
    throw "The modern Login diagnostics collector requires Windows."
}

$root = Split-Path -Parent $PSScriptRoot
$statePath = Join-Path $root "artifacts\modern-login-local\processes.json"
$timestamp = [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
$workingRoot = Join-Path $root "artifacts\modern-login-diagnostics"
$workingDirectory = Join-Path $workingRoot $timestamp
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $workingRoot "modern-login-diagnostics-$timestamp.zip"
}

function Protect-DiagnosticLine {
    param([AllowEmptyString()][string]$Line)

    if ($null -eq $Line) {
        return ""
    }

    if ($Line -match '(?i)NoS0576|NoS0577') {
        return [regex]::Replace($Line, '(?i)(NoS0576|NoS0577).*$', '$1 <redacted-modern-login-packet>')
    }
    if ($Line -match '(?i)NsTeST') {
        return [regex]::Replace($Line, '(?i)(NsTeST).*$', '$1 <redacted-entry-packet>')
    }

    $protected = [regex]::Replace(
        $Line,
        '(?i)"(password|authorizationCode|ticket|token|secret|[^"\r\n]*authKey)"\s*:\s*"[^"]*"',
        '"$1":"<redacted>"')
    $protected = [regex]::Replace(
        $protected,
        '(?i)(password|authorizationCode|ticket|token|secret|auth[_-]?key)\s*[=:]\s*[^\s<,;]+',
        '$1=<redacted>')
    $protected = [regex]::Replace(
        $protected,
        '(?i)(account(?:Name|Id)?\s*[=:]\s*)[^\s<,;]+',
        '$1<redacted>')
    $protected = [regex]::Replace(
        $protected,
        '\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}\b',
        '<guid>')
    $protected = [regex]::Replace(
        $protected,
        '\b(?:\d{1,3}\.){3}\d{1,3}\b',
        {
            param($match)
            if ($match.Value -eq '127.0.0.1' -or $match.Value -eq '0.0.0.0') {
                return $match.Value
            }
            return '<ip>'
        })
    $protected = [regex]::Replace(
        $protected,
        '(?i)C:\\Users\\[^\\\s]+',
        'C:\Users\<user>')
    $protected = [regex]::Replace(
        $protected,
        '\b[A-Za-z0-9+/]{40,}={0,2}\b',
        '<long-value>')
    return $protected
}

function Write-SanitizedLogTail {
    param(
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )

    $file = Get-Item -LiteralPath $SourcePath
    $maximumBytes = $MaxLogMegabytes * 1MB
    $lines = Get-Content -LiteralPath $file.FullName -Tail $MaxLogLines -ErrorAction Stop
    $sanitized = foreach ($line in $lines) {
        Protect-DiagnosticLine -Line ([string]$line)
    }

    $text = $sanitized -join [Environment]::NewLine
    if ([Text.Encoding]::UTF8.GetByteCount($text) -gt $maximumBytes) {
        $allowedCharacters = [Math]::Max(1024, [Math]::Floor($maximumBytes / 2))
        if ($text.Length -gt $allowedCharacters) {
            $text = "<tail truncated to diagnostic size limit>" + [Environment]::NewLine +
                $text.Substring($text.Length - $allowedCharacters)
        }
    }

    Set-Content -LiteralPath $DestinationPath -Value $text -Encoding UTF8
}

New-Item -ItemType Directory -Path $workingDirectory -Force | Out-Null
$logsDirectory = Join-Path $workingDirectory "logs"
New-Item -ItemType Directory -Path $logsDirectory -Force | Out-Null

try {
    $readinessPath = Join-Path $workingDirectory "readiness.json"
    try {
        $readiness = & (Join-Path $PSScriptRoot "test-modern-login-readiness.ps1") `
            -OutputPath $readinessPath `
            -PassThru
    }
    catch {
        $readiness = [pscustomobject]@{
            SchemaVersion = 1
            GeneratedAtUtc = [DateTime]::UtcNow.ToString("O")
            OverallStatus = "collector_error"
            Error = "Readiness inspection could not complete."
        }
        $readiness | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $readinessPath -Encoding UTF8
    }

    $state = $null
    if (Test-Path -LiteralPath $statePath -PathType Leaf) {
        try {
            $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
            $sanitizedProcesses = foreach ($record in @($state.Processes)) {
                [pscustomobject]@{
                    Name = [string]$record.Name
                    Id = [int]$record.Id
                    ProcessName = [string]$record.ProcessName
                    ExecutableFile = Split-Path -Leaf ([string]$record.Executable)
                    StartedAtUtc = [string]$record.StartedAtUtc
                }
            }

            $endpointUri = New-Object Uri([string]$state.AuthenticationEndpoint)
            $sanitizedState = [pscustomobject]@{
                SchemaVersion = [int]$state.SchemaVersion
                CreatedAtUtc = [string]$state.CreatedAtUtc
                AuthenticationEndpoint = "$($endpointUri.Scheme)://$($endpointUri.Host):$($endpointUri.Port)$($endpointUri.AbsolutePath)"
                SpanishLoginPort = [int]$state.SpanishLoginPort
                WorldPort = [int]$state.WorldPort
                Processes = @($sanitizedProcesses)
            }
            $sanitizedState | ConvertTo-Json -Depth 5 | Set-Content `
                -LiteralPath (Join-Path $workingDirectory "runtime-state.sanitized.json") `
                -Encoding UTF8
        }
        catch {
            Set-Content -LiteralPath (Join-Path $workingDirectory "runtime-state-error.txt") `
                -Value "Runtime state could not be parsed." `
                -Encoding UTF8
        }
    }

    $dotnetVersion = "not-found"
    if (Get-Command dotnet.exe -ErrorAction SilentlyContinue) {
        try {
            $dotnetVersion = (& dotnet --version 2>$null | Select-Object -First 1)
        }
        catch {
            $dotnetVersion = "unavailable"
        }
    }

    $gitCommit = "unavailable"
    if (Get-Command git.exe -ErrorAction SilentlyContinue) {
        try {
            $gitCommit = (& git -C $root rev-parse HEAD 2>$null | Select-Object -First 1)
        }
        catch {
            $gitCommit = "unavailable"
        }
    }

    $systemSummary = [pscustomobject]@{
        GeneratedAtUtc = [DateTime]::UtcNow.ToString("O")
        OperatingSystemVersion = [Environment]::OSVersion.VersionString
        Is64BitOperatingSystem = [Environment]::Is64BitOperatingSystem
        Is64BitProcess = [Environment]::Is64BitProcess
        PowerShellVersion = $PSVersionTable.PSVersion.ToString()
        DotNetSdkVersion = [string]$dotnetVersion
        RepositoryCommit = [string]$gitCommit
    }
    $systemSummary | ConvertTo-Json -Depth 4 | Set-Content `
        -LiteralPath (Join-Path $workingDirectory "system-summary.json") `
        -Encoding UTF8

    $settingsPath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "NosGM\Launcher\settings.json"
    $launcherSummary = [ordered]@{
        SettingsPresent = $false
        CredentialScanPassed = $false
        Language = "unknown"
        ClientExecutablePresent = $false
        ClientFileName = "unknown"
        ClientFileVersion = "unknown"
        InstallationIdPresent = $false
    }

    if (Test-Path -LiteralPath $settingsPath -PathType Leaf) {
        try {
            $settingsRaw = Get-Content -LiteralPath $settingsPath -Raw
            $launcherSummary.SettingsPresent = $true
            $launcherSummary.CredentialScanPassed = $settingsRaw -notmatch '(?i)"(password|authorizationCode|ticket|token|secret)"\s*:'
            $settings = $settingsRaw | ConvertFrom-Json
            $launcherSummary.Language = [string]$settings.Language
            $launcherSummary.ClientFileName = [string]$settings.GameExecutable
            $clientPath = Join-Path ([string]$settings.InstallRoot) ([string]$settings.GameExecutable)
            if (Test-Path -LiteralPath $clientPath -PathType Leaf) {
                $clientItem = Get-Item -LiteralPath $clientPath
                $launcherSummary.ClientExecutablePresent = $true
                $launcherSummary.ClientFileVersion = [string]$clientItem.VersionInfo.FileVersion
            }
        }
        catch {
            $launcherSummary.CredentialScanPassed = $false
        }
    }

    try {
        $registryValue = Get-ItemPropertyValue -Path "HKCU:\Software\Gameforge4d\TNTClient\MainApp" -Name "InstallationId" -ErrorAction Stop
        $installationId = [Guid]::Empty
        $launcherSummary.InstallationIdPresent = [Guid]::TryParse([string]$registryValue, [ref]$installationId) -and
            $installationId -ne [Guid]::Empty
    }
    catch {
        $launcherSummary.InstallationIdPresent = $false
    }

    [pscustomobject]$launcherSummary | ConvertTo-Json -Depth 4 | Set-Content `
        -LiteralPath (Join-Path $workingDirectory "launcher-summary.json") `
        -Encoding UTF8

    if ($null -ne $state) {
        $usedNames = @{}
        foreach ($record in @($state.Processes)) {
            $processDirectory = Split-Path -Parent ([string]$record.Executable)
            if (-not (Test-Path -LiteralPath $processDirectory -PathType Container)) {
                continue
            }

            $logFiles = Get-ChildItem -LiteralPath $processDirectory -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -like 'log*.xml' -or $_.Extension -eq '.log' } |
                Sort-Object LastWriteTimeUtc -Descending |
                Select-Object -First 3

            foreach ($logFile in $logFiles) {
                $baseName = "$($record.Name)-$($logFile.Name)"
                if ($usedNames.ContainsKey($baseName)) {
                    $usedNames[$baseName]++
                    $baseName = "$($record.Name)-$($usedNames[$baseName])-$($logFile.Name)"
                }
                else {
                    $usedNames[$baseName] = 1
                }

                $destination = Join-Path $logsDirectory ($baseName + ".tail.txt")
                try {
                    Write-SanitizedLogTail -SourcePath $logFile.FullName -DestinationPath $destination
                }
                catch {
                    Set-Content -LiteralPath $destination -Value "Log tail could not be collected." -Encoding UTF8
                }
            }
        }
    }

    $manifest = @"
# NosGM modern Login diagnostics

Generated: $([DateTime]::UtcNow.ToString("O"))

Included:
- readiness result
- sanitized runtime process metadata
- OS, PowerShell, .NET SDK and repository commit versions
- launcher/client presence summary without paths or account names
- bounded tails of local process logs after redaction

Always omitted or redacted:
- passwords
- authorization codes and raw modern Login packets
- authentication keys, tokens and secrets
- account names and account identifiers
- InstallationId and other GUID values
- non-loopback IP addresses
- Windows user profile names
- full launcher settings and registry values

The collector never reads process environment blocks and never writes secret values to the bundle.
"@
    Set-Content -LiteralPath (Join-Path $workingDirectory "MANIFEST.md") -Value $manifest -Encoding UTF8

    $outputDirectory = Split-Path -Parent $OutputPath
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    if (Test-Path -LiteralPath $OutputPath) {
        Remove-Item -LiteralPath $OutputPath -Force
    }
    Compress-Archive -Path (Join-Path $workingDirectory '*') -DestinationPath $OutputPath -CompressionLevel Optimal
}
finally {
    Remove-Item -LiteralPath $workingDirectory -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Sanitized diagnostics bundle: $OutputPath" -ForegroundColor Green
