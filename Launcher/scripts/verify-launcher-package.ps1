[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,

    [switch]$RequireAuthenticode
)

$ErrorActionPreference = 'Stop'
$package = [IO.Path]::GetFullPath($PackageDirectory)
if (-not (Test-Path $package -PathType Container)) {
    throw "Package directory does not exist: $package"
}

$requiredFiles = @(
    'NosGM.Launcher.exe',
    'LICENSE.txt',
    'LAUNCHER-NOTICE.md',
    'README.md',
    'NOSGM-AUTHORS.md',
    'NOSGM-NOTICE.md',
    'THIRD_PARTY_NOTICES.md',
    'SOURCE_COMMIT.txt',
    'release-info.json'
)
foreach ($relative in $requiredFiles) {
    if (-not (Test-Path (Join-Path $package $relative) -PathType Leaf)) {
        throw "Launcher package is missing required file: $relative"
    }
}

$metadata = Get-Content (Join-Path $package 'release-info.json') -Raw | ConvertFrom-Json
if ($metadata.schemaVersion -ne 1 -or
    $metadata.product -ne 'NosGM Launcher' -or
    $metadata.sourceCommit -notmatch '^[0-9a-f]{40}$' -or
    $metadata.publicKeyFingerprint -notmatch '^[0-9A-F]{64}$' -or
    -not $metadata.selfContained) {
    throw 'Launcher release metadata is invalid or incomplete.'
}

$listed = @{}
foreach ($entry in @($metadata.files)) {
    $relative = [string]$entry.path
    if ([string]::IsNullOrWhiteSpace($relative) -or
        $relative.Contains('\') -or
        $relative.StartsWith('/') -or
        $relative.Split('/') -contains '..' -or
        $listed.ContainsKey($relative)) {
        throw "Release metadata contains an unsafe or duplicate path: $relative"
    }

    $fullPath = [IO.Path]::GetFullPath((Join-Path $package $relative))
    $prefix = $package.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path $fullPath -PathType Leaf)) {
        throw "Release metadata points outside the package or to a missing file: $relative"
    }

    $file = Get-Item $fullPath
    $actualHash = (Get-FileHash $fullPath -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($file.Length -ne [long]$entry.size -or
        $actualHash -ne ([string]$entry.sha256).ToUpperInvariant()) {
        throw "Release metadata hash or size mismatch: $relative"
    }
    $listed[$relative] = $true
}

$actualFiles = @(Get-ChildItem $package -File -Recurse | ForEach-Object {
    [IO.Path]::GetRelativePath($package, $_.FullName).Replace('\', '/')
} | Where-Object { $_ -ne 'release-info.json' })
if ($actualFiles.Count -ne $listed.Count) {
    throw 'Launcher package contains files not covered by release-info.json.'
}
foreach ($relative in $actualFiles) {
    if (-not $listed.ContainsKey($relative)) {
        throw "Launcher package contains an unlisted file: $relative"
    }
}

$forbiddenNames = @('NostaleClient.exe', 'NostaleClientX.exe', 'Nostale.exe')
$forbidden = @(Get-ChildItem $package -File -Recurse | Where-Object {
    $_.Name -in $forbiddenNames -or $_.Extension -in @('.nos', '.pak')
})
if ($forbidden.Count -gt 0) {
    throw "Proprietary client-looking material found in launcher package: $($forbidden.FullName -join ', ')"
}

$textExtensions = @('.txt', '.md', '.json', '.config', '.xml')
foreach ($file in Get-ChildItem $package -File -Recurse | Where-Object {
    $_.Length -le 4MB -and $_.Extension -in $textExtensions
}) {
    $text = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($text -and ($text.Contains('BEGIN PRIVATE KEY', [StringComparison]::Ordinal) -or
                    $text.Contains('BEGIN EC PRIVATE KEY', [StringComparison]::Ordinal) -or
                    $text.Contains('BEGIN ENCRYPTED PRIVATE KEY', [StringComparison]::Ordinal))) {
        throw "Private key material found in launcher package: $($file.FullName)"
    }
}

$commitFile = (Get-Content (Join-Path $package 'SOURCE_COMMIT.txt') -Raw).Trim()
if ($commitFile -ne $metadata.sourceCommit) {
    throw 'SOURCE_COMMIT.txt does not match release-info.json.'
}

if ($RequireAuthenticode) {
    $signature = Get-AuthenticodeSignature (Join-Path $package 'NosGM.Launcher.exe')
    if ($signature.Status -ne 'Valid') {
        throw "NosGM.Launcher.exe does not have a valid Authenticode signature: $($signature.Status)"
    }
}

Write-Host 'NosGM Launcher package verification passed.'
