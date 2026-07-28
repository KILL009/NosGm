[CmdletBinding()]
param(
    [string]$OutputPath,
    [ValidateRange(50, 2000)]
    [int]$MaxLogLines = 400,
    [ValidateRange(1, 50)]
    [int]$MaxLogMegabytes = 8,
    [switch]$SelfTest
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
        '\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b',
        '<email>',
        [Text.RegularExpressions.RegexOptions]::IgnoreCase)
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

    $maximumBytes = [long]$MaxLogMegabytes * 1MB
    $stream = [IO.File]::Open(
        $SourcePath,
        [IO.FileMode]::Open,
        [IO.FileAccess]::Read,
        [IO.FileShare]::ReadWrite)
    try {
        $sourceLength = $stream.Length
        $bytesToRead = [int][Math]::Min($maximumBytes, $sourceLength)
        if ($bytesToRead -eq 0) {
            Set-Content -LiteralPath $DestinationPath -Value "" -Encoding UTF8
            return
        }

        [void]$stream.Seek(-1 * $bytesToRead, [IO.SeekOrigin]::End)
        $buffer = New-Object byte[] $bytesToRead
        $readTotal = 0
        while ($readTotal -lt $bytesToRead) {
            $read = $stream.Read($buffer, $readTotal, $bytesToRead - $readTotal)
            if ($read -le 0) {
                break
            }
            $readTotal += $read
        }

        $text = [Text.Encoding]::UTF8.GetString($buffer, 0, $readTotal)
        [Array]::Clear($buffer, 0, $buffer.Length)
    }
    finally {
        $stream.Dispose()
    }

    $lines = @($text -split '\r?\n')
    if ($sourceLength -gt $maximumBytes -and $lines.Count -gt 1) {
        $lines = @($lines | Select-Object -Skip 1)
    }
    $lines = @($lines | Select-Object -Last $MaxLogLines)

    $sanitized = foreach ($line in $lines) {
        Protect-DiagnosticLine -Line ([string]$line)
    }

    $sanitizedText = $sanitized -join [Environment]::NewLine
    if ([Text.Encoding]::UTF8.GetByteCount($sanitizedText) -gt $maximumBytes) {
        $allowedCharacters = [Math]::Max(1024, [Math]::Floor($maximumBytes / 2))
        if ($sanitizedText.Length -gt $allowedCharacters) {
            $sanitizedText = "<tail truncated to diagnostic size limit>" + [Environment]::NewLine +
                $sanitizedText.Substring($sanitizedText.Length - $allowedCharacters)
        }
    }

    Set-Content -LiteralPath $DestinationPath -Value $sanitizedText -Encoding UTF8
}

function Resolve-GitExecutable {
    $command = Get-Command git.exe -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $command) {
        $command = Get-Command git -ErrorAction SilentlyContinue | Select-Object -First 1
    }
    if ($null -ne $command) {
        return $command.Source
    }

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace([string]$env:ProgramW6432)) {
        $candidates += Join-Path ([string]$env:ProgramW6432) "Git\cmd\git.exe"
    }
    $localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    if (-not [string]::IsNullOrWhiteSpace($localApplicationData)) {
        $candidates += Join-Path $localApplicationData "Programs\Git\cmd\git.exe"
    }
    foreach ($candidate in $candidates) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and
            (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            return $candidate
        }
    }
    return $null
}

function Resolve-GitHeadCommit {
    param([Parameter(Mandatory = $true)][string]$RepositoryRoot)

    try {
        $gitDirectory = Join-Path $RepositoryRoot ".git"
        $headPath = Join-Path $gitDirectory "HEAD"
        if (-not (Test-Path -LiteralPath $headPath -PathType Leaf)) {
            return $null
        }

        $head = (Get-Content -LiteralPath $headPath -Raw).Trim()
        if ($head -match '^[0-9a-fA-F]{40}$') {
            return $head.ToLowerInvariant()
        }
        if (-not $head.StartsWith("ref: ", [StringComparison]::Ordinal)) {
            return $null
        }

        $referenceName = $head.Substring(5).Trim()
        $looseReferencePath = Join-Path $gitDirectory ($referenceName.Replace('/', '\'))
        if (Test-Path -LiteralPath $looseReferencePath -PathType Leaf) {
            $looseReference = (Get-Content -LiteralPath $looseReferencePath -Raw).Trim()
            if ($looseReference -match '^[0-9a-fA-F]{40}$') {
                return $looseReference.ToLowerInvariant()
            }
        }

        $packedReferencesPath = Join-Path $gitDirectory "packed-refs"
        if (Test-Path -LiteralPath $packedReferencesPath -PathType Leaf) {
            foreach ($line in Get-Content -LiteralPath $packedReferencesPath) {
                if ($line.StartsWith("#", [StringComparison]::Ordinal) -or
                    $line.StartsWith("^", [StringComparison]::Ordinal)) {
                    continue
                }
                $parts = @($line -split ' ', 2)
                if ($parts.Count -eq 2 -and
                    $parts[1] -eq $referenceName -and
                    $parts[0] -match '^[0-9a-fA-F]{40}$') {
                    return $parts[0].ToLowerInvariant()
                }
            }
        }
    }
    catch {
        return $null
    }
    return $null
}

function Get-FileFingerprint {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    try {
        $item = Get-Item -LiteralPath $Path
        $hash = Get-FileHash -LiteralPath $Path -Algorithm SHA256
        return [pscustomobject]@{
            File = $item.Name
            Status = "available"
            Length = [long]$item.Length
            LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString("O")
            FileVersion = [string]$item.VersionInfo.FileVersion
            Sha256 = [string]$hash.Hash
        }
    }
    catch {
        return [pscustomobject]@{
            File = Split-Path -Leaf $Path
            Status = "unavailable"
            Length = $null
            LastWriteTimeUtc = $null
            FileVersion = $null
            Sha256 = $null
        }
    }
}

function Invoke-RedactionSelfTest {
    $testDirectory = Join-Path ([IO.Path]::GetTempPath()) ("NosGM-redaction-" + [Guid]::NewGuid().ToString("N"))
    $sourcePath = Join-Path $testDirectory "synthetic.log"
    $destinationPath = Join-Path $testDirectory "sanitized.log"
    New-Item -ItemType Directory -Path $testDirectory -Force | Out-Null

    $syntheticPassword = "synthetic-" + "password-value"
    $syntheticAccount = "SyntheticAccount"
    $syntheticEmail = "tester" + "@" + "example.invalid"
    $syntheticGuid = [Guid]::NewGuid().ToString("D")
    $syntheticLongValue = ("A" * 48) + ("B" * 16)

    try {
        $writer = New-Object IO.StreamWriter($sourcePath, $false, [Text.Encoding]::UTF8)
        try {
            for ($index = 0; $index -lt 90000; $index++) {
                $writer.WriteLine("noise-line-$index")
            }
            $writer.WriteLine("NoS0577 raw $syntheticPassword")
            $writer.WriteLine("NsTeST $syntheticAccount $syntheticGuid")
            $writer.WriteLine("password=$syntheticPassword")
            $writer.WriteLine("accountName=$syntheticAccount")
            $writer.WriteLine("contact=$syntheticEmail")
            $writer.WriteLine("session=$syntheticGuid")
            $writer.WriteLine("remote=8.8.8.8 local=127.0.0.1")
            $writer.WriteLine("path=C:\Users\SyntheticUser\NosGM")
            $writer.WriteLine("standalone=$syntheticLongValue")
        }
        finally {
            $writer.Dispose()
        }

        Write-SanitizedLogTail -SourcePath $sourcePath -DestinationPath $destinationPath
        $result = Get-Content -LiteralPath $destinationPath -Raw

        foreach ($forbiddenValue in @(
            $syntheticPassword,
            $syntheticAccount,
            $syntheticEmail,
            $syntheticGuid,
            '8.8.8.8',
            'SyntheticUser',
            $syntheticLongValue
        )) {
            if ($result.IndexOf($forbiddenValue, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Diagnostic redaction self-test leaked a synthetic private value."
            }
        }

        foreach ($requiredMarker in @(
            '<redacted-modern-login-packet>',
            '<redacted-entry-packet>',
            '<redacted>',
            '<email>',
            '<guid>',
            '<ip>',
            'C:\Users\<user>',
            '<long-value>',
            '127.0.0.1'
        )) {
            if ($result.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
                throw "Diagnostic redaction self-test did not emit required marker $requiredMarker."
            }
        }

        $maximumBytes = [long]$MaxLogMegabytes * 1MB
        if ((Get-Item -LiteralPath $destinationPath).Length -gt ($maximumBytes + 4096)) {
            throw "Diagnostic redaction self-test exceeded the configured output ceiling."
        }
    }
    finally {
        Remove-Item -LiteralPath $testDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Modern Login diagnostics redaction and bounded-tail self-test passed." -ForegroundColor Green
}

if ($SelfTest) {
    Invoke-RedactionSelfTest
    return
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

            $endpointUri = [Uri]([string]$state.AuthenticationEndpoint)
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

            try {
                $diagnosticModules = @(
                    "NosGm.Core.dll",
                    "NosGm.GameObject.dll",
                    "NosGm.Handler.dll",
                    "NosGm.Master.Library.dll"
                )
                $binaryProcesses = foreach ($record in @($state.Processes)) {
                    $executable = [string]$record.Executable
                    $processDirectory = Split-Path -Parent $executable
                    $fingerprints = @()

                    $executableFingerprint = Get-FileFingerprint -Path $executable
                    if ($null -ne $executableFingerprint) {
                        $fingerprints += $executableFingerprint
                    }
                    foreach ($moduleName in $diagnosticModules) {
                        $moduleFingerprint = Get-FileFingerprint -Path (Join-Path $processDirectory $moduleName)
                        if ($null -ne $moduleFingerprint) {
                            $fingerprints += $moduleFingerprint
                        }
                    }

                    [pscustomobject]@{
                        Name = [string]$record.Name
                        Files = @($fingerprints)
                    }
                }
                [pscustomobject]@{
                    SchemaVersion = 1
                    GeneratedAtUtc = [DateTime]::UtcNow.ToString("O")
                    Processes = @($binaryProcesses)
                } | ConvertTo-Json -Depth 6 | Set-Content `
                    -LiteralPath (Join-Path $workingDirectory "binary-summary.json") `
                    -Encoding UTF8
            }
            catch {
                [pscustomobject]@{
                    Status = "unavailable"
                    ErrorType = $_.Exception.GetType().Name
                } | ConvertTo-Json -Depth 3 | Set-Content `
                    -LiteralPath (Join-Path $workingDirectory "binary-summary-error.json") `
                    -Encoding UTF8
            }
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
    $gitDirty = $null
    $gitExecutable = Resolve-GitExecutable
    if ($null -ne $gitExecutable) {
        try {
            $resolvedCommit = (& $gitExecutable -C $root rev-parse HEAD 2>$null | Select-Object -First 1)
            if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace([string]$resolvedCommit)) {
                $gitCommit = [string]$resolvedCommit
                $gitStatus = @(& $gitExecutable -C $root status --porcelain --untracked-files=no 2>$null)
                if ($LASTEXITCODE -eq 0) {
                    $gitDirty = $gitStatus.Count -gt 0
                }
            }
        }
        catch {
            $gitCommit = "unavailable"
            $gitDirty = $null
        }
    }
    if ($gitCommit -eq "unavailable") {
        $resolvedHead = Resolve-GitHeadCommit -RepositoryRoot $root
        if (-not [string]::IsNullOrWhiteSpace([string]$resolvedHead)) {
            $gitCommit = [string]$resolvedHead
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
        RepositoryDirty = $gitDirty
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
- SHA-256 fingerprints for the launched executables and the fixed NosGM diagnostic module allowlist
- launcher/client presence summary without paths or account names
- bounded tails of local process logs after redaction

Always omitted or redacted:
- passwords
- authorization codes and raw modern Login packets
- authentication keys, tokens and secrets
- account names and account identifiers
- email addresses
- InstallationId and other GUID values
- non-loopback IP addresses
- Windows user profile names
- full launcher settings and registry values

The collector never reads process environment blocks and never writes secret values to the bundle.
"@
    Set-Content -LiteralPath (Join-Path $workingDirectory "MANIFEST.md") -Value $manifest -Encoding UTF8

    $outputDirectory = Split-Path -Parent $OutputPath
    if ([string]::IsNullOrWhiteSpace($outputDirectory)) {
        $outputDirectory = (Get-Location).Path
        $OutputPath = Join-Path $outputDirectory $OutputPath
    }
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
