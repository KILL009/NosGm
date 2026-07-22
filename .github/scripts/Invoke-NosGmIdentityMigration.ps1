param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$legacyPascal = [string]::Concat('F', 'rostvein')
$legacyLower = $legacyPascal.ToLowerInvariant()
$legacyUpper = $legacyPascal.ToUpperInvariant()
$newPascal = 'NosGm'
$newLower = 'nosgm'
$newUpper = 'NOSGM'

$temporaryPaths = @(
    '.github/scripts/Invoke-NosGmIdentityMigration.ps1',
    '.github/workflows/nosgm-identity-migration.yml',
    '.github/identity-migration-trigger.txt'
)

$binaryExtensions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@(
    '.7z', '.avi', '.bak', '.bin', '.binlog', '.bmp', '.cer', '.db', '.dll', '.doc', '.docx',
    '.exe', '.gif', '.gz', '.ico', '.jpeg', '.jpg', '.mp3', '.mp4', '.nupkg', '.otf', '.pdb',
    '.pdf', '.pfx', '.png', '.rar', '.snk', '.sqlite', '.tar', '.ttf', '.wav', '.woff', '.woff2',
    '.xls', '.xlsx', '.zip'
) | ForEach-Object { [void]$binaryExtensions.Add($_) }

function Get-TextEncodingInfo {
    param([byte[]]$Bytes)

    if ($Bytes.Length -ge 3 -and $Bytes[0] -eq 0xEF -and $Bytes[1] -eq 0xBB -and $Bytes[2] -eq 0xBF) {
        return [pscustomobject]@{
            Encoding = [System.Text.UTF8Encoding]::new($true, $true)
            PreambleLength = 3
        }
    }

    if ($Bytes.Length -ge 2 -and $Bytes[0] -eq 0xFF -and $Bytes[1] -eq 0xFE) {
        return [pscustomobject]@{
            Encoding = [System.Text.UnicodeEncoding]::new($false, $true, $true)
            PreambleLength = 2
        }
    }

    if ($Bytes.Length -ge 2 -and $Bytes[0] -eq 0xFE -and $Bytes[1] -eq 0xFF) {
        return [pscustomobject]@{
            Encoding = [System.Text.UnicodeEncoding]::new($true, $true, $true)
            PreambleLength = 2
        }
    }

    if ($Bytes -contains 0) {
        return $null
    }

    return [pscustomobject]@{
        Encoding = [System.Text.UTF8Encoding]::new($false, $true)
        PreambleLength = 0
    }
}

function Replace-IdentityText {
    param([string]$Text)

    return $Text.Replace($legacyPascal, $newPascal)
        .Replace($legacyLower, $newLower)
        .Replace($legacyUpper, $newUpper)
}

function Replace-IdentityPath {
    param([string]$Path)

    return $Path.Replace($legacyPascal, $newPascal)
        .Replace($legacyLower, $newLower)
        .Replace($legacyUpper, $newUpper)
}

$contentFilesChanged = 0
$contentOccurrencesChanged = 0
$skippedBinaryFiles = 0
$trackedFiles = @(git ls-files)

foreach ($relativePath in $trackedFiles) {
    if ($temporaryPaths -contains $relativePath) {
        continue
    }

    $extension = [System.IO.Path]::GetExtension($relativePath)
    if ($binaryExtensions.Contains($extension)) {
        $skippedBinaryFiles++
        continue
    }

    $absolutePath = Join-Path (Get-Location) $relativePath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        continue
    }

    [byte[]]$bytes = [System.IO.File]::ReadAllBytes($absolutePath)
    $encodingInfo = Get-TextEncodingInfo -Bytes $bytes
    if ($null -eq $encodingInfo) {
        $skippedBinaryFiles++
        continue
    }

    try {
        $payloadLength = $bytes.Length - $encodingInfo.PreambleLength
        $text = $encodingInfo.Encoding.GetString($bytes, $encodingInfo.PreambleLength, $payloadLength)
    }
    catch [System.Text.DecoderFallbackException] {
        $skippedBinaryFiles++
        continue
    }

    $occurrences = ([regex]::Matches($text, [regex]::Escape($legacyPascal), [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)).Count
    if ($occurrences -eq 0) {
        continue
    }

    $updated = Replace-IdentityText -Text $text
    if ($updated -eq $text) {
        continue
    }

    [byte[]]$body = $encodingInfo.Encoding.GetBytes($updated)
    [byte[]]$preamble = $encodingInfo.Encoding.GetPreamble()
    if ($encodingInfo.PreambleLength -eq 0) {
        [System.IO.File]::WriteAllBytes($absolutePath, $body)
    }
    else {
        [byte[]]$output = New-Object byte[] ($preamble.Length + $body.Length)
        [System.Array]::Copy($preamble, 0, $output, 0, $preamble.Length)
        [System.Array]::Copy($body, 0, $output, $preamble.Length, $body.Length)
        [System.IO.File]::WriteAllBytes($absolutePath, $output)
    }

    $contentFilesChanged++
    $contentOccurrencesChanged += $occurrences
}

$pathsRenamed = 0
$paths = @(git ls-files | Sort-Object { $_.Length } -Descending)
foreach ($oldPath in $paths) {
    if ($temporaryPaths -contains $oldPath) {
        continue
    }

    $newPath = Replace-IdentityPath -Path $oldPath
    if ($newPath -eq $oldPath) {
        continue
    }

    if (Test-Path -LiteralPath $newPath) {
        throw "Identity migration collision: '$oldPath' -> '$newPath'"
    }

    $parent = Split-Path -Parent $newPath
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    & git mv -- $oldPath $newPath
    if ($LASTEXITCODE -ne 0) {
        throw "git mv failed: '$oldPath' -> '$newPath'"
    }

    $pathsRenamed++
}

$remainingContent = @(& git grep -n -I -i -- $legacyPascal -- . 2>$null)
$remainingPaths = @(git ls-files | Where-Object { $_ -match [regex]::Escape($legacyPascal) })

if ($remainingContent.Count -gt 0 -or $remainingPaths.Count -gt 0) {
    Write-Host 'Remaining legacy identity references:'
    $remainingContent | Select-Object -First 100 | ForEach-Object { Write-Host $_ }
    $remainingPaths | Select-Object -First 100 | ForEach-Object { Write-Host "PATH: $_" }
    throw "The migration left $($remainingContent.Count) content matches and $($remainingPaths.Count) path matches."
}

New-Item -ItemType Directory -Force -Path 'docs' | Out-Null
$report = @"
# NosGm source identity migration

This migration assigns the complete internal source identity to **NosGm**.

- Text files changed: $contentFilesChanged
- Identity occurrences changed: $contentOccurrencesChanged
- Tracked paths renamed: $pathsRenamed
- Binary files deliberately left byte-for-byte unchanged: $skippedBinaryFiles
- Remaining legacy identity references in tracked text and paths: 0

The migration includes namespaces, assemblies, project names, project paths, solution references,
configuration type names, executable names, build workflows, scripts, resources and documentation.
Binary assets are not rewritten internally because changing arbitrary binary bytes would corrupt them.
"@
[System.IO.File]::WriteAllText((Join-Path (Get-Location) 'docs/NOSGM_IDENTITY_MIGRATION.md'), $report, [System.Text.UTF8Encoding]::new($false))

Write-Host "[NOSGM_IDENTITY] TextFilesChanged=$contentFilesChanged OccurrencesChanged=$contentOccurrencesChanged PathsRenamed=$pathsRenamed SkippedBinary=$skippedBinaryFiles Remaining=0"
