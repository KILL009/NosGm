$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$tool = Join-Path $root "Tools/NosGM.ClientThemeEditor"

$required = @(
    "LICENSE",
    "NOTICE.md",
    "README.md",
    "NosGM.ClientThemeEditor.csproj",
    "SafeThemeApplication.cs",
    "docs/LEGACY_RESEARCH.md",
    "themes/example-theme.json"
)

foreach ($path in $required) {
    if (-not (Test-Path (Join-Path $tool $path))) {
        throw "Missing required ClientThemeEditor file: $path"
    }
}

$notice = Get-Content (Join-Path $tool "NOTICE.md") -Raw
foreach ($needle in @(
    "Elendan/Notale-Text-Picker",
    "9eb44d2a0041b49375fabb730121a01acd7bae87",
    "Copyright (c) 2019 Elendan",
    "Cryless",
    "Fizo55",
    "Pumba98/Nostale-ClientColorizer",
    "9d1e61c717b6a49ca221a5f2d855dfa5fa11591c",
    "no ClientColorizer source code is copied",
    "no ClientColorizer pattern, signature or offset is distributed"
)) {
    if (-not $notice.Contains($needle, [System.StringComparison]::Ordinal)) {
        throw "ClientThemeEditor notice is missing: $needle"
    }
}

$project = Get-Content (Join-Path $tool "NosGM.ClientThemeEditor.csproj") -Raw
foreach ($needle in @(
    "<TargetFramework>net9.0</TargetFramework>",
    "<Nullable>enable</Nullable>",
    "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>"
)) {
    if (-not $project.Contains($needle, [System.StringComparison]::Ordinal)) {
        throw "ClientThemeEditor project is missing: $needle"
    }
}

# Inspect only files committed to Git. The workflow builds before this script runs,
# so recursive filesystem scans would otherwise mistake generated bin/obj files for
# source files or bundled binaries.
$trackedRelativePaths = @(& git -C $root ls-files -- "Tools/NosGM.ClientThemeEditor")
if ($LASTEXITCODE -ne 0) {
    throw "Unable to enumerate tracked ClientThemeEditor files."
}

$sourceFiles = @(
    $trackedRelativePaths |
        Where-Object { [System.IO.Path]::GetExtension($_) -ieq ".cs" } |
        ForEach-Object { Get-Item -LiteralPath (Join-Path $root $_) }
)

if ($sourceFiles.Count -eq 0) {
    throw "ClientThemeEditor has no tracked C# source files."
}

foreach ($file in $sourceFiles) {
    $text = Get-Content $file.FullName -Raw
    if (-not $text.StartsWith("// SPDX-License-Identifier: MIT", [System.StringComparison]::Ordinal)) {
        throw "Missing MIT SPDX header: $($file.FullName)"
    }
}

$joined = ($sourceFiles | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"

foreach ($forbidden in @(
    "Process.Start",
    "HttpClient",
    "Socket",
    "TcpClient",
    "UdpClient",
    "DllImport",
    "VirtualProtect",
    "WriteProcessMemory",
    "Assembly.Load",
    "allow_unverified_client"
)) {
    if ($joined.Contains($forbidden, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Forbidden ClientThemeEditor primitive found: $forbidden"
    }
}

foreach ($requiredCode in @(
    "ExpectedSha256",
    "ExpectedFileVersion",
    "ExpectedLength",
    "ExpectedMatches",
    "ExpectedOriginalHex",
    "ResearchOnly",
    "ValidateContentIdentity",
    "WritePatchedAtomically",
    "SafeThemeApplication",
    "NosGM.ThemeBackups",
    "Restore"
)) {
    if (-not $joined.Contains($requiredCode, [System.StringComparison]::Ordinal)) {
        throw "Required ClientThemeEditor safety control missing: $requiredCode"
    }
}

$binaryExtensions = @(".exe", ".dll", ".bin", ".dmp")
$trackedBinaryFiles = @(
    $trackedRelativePaths | Where-Object {
        $binaryExtensions -contains [System.IO.Path]::GetExtension($_).ToLowerInvariant()
    }
)
if ($trackedBinaryFiles.Count -gt 0) {
    throw "ClientThemeEditor must not contain tracked client binaries, DLLs or dumps: $($trackedBinaryFiles -join ', ')"
}

$profiles = Join-Path $tool "profiles"
if (Test-Path $profiles) {
    $activeProfiles = Get-ChildItem $profiles -File -Recurse
    if ($activeProfiles) {
        throw "The first ClientThemeEditor release must not ship active client signature profiles."
    }
}

$solution = Get-Content (Join-Path $root "NosGm.sln") -Raw
if ($solution.Contains("NosGM.ClientThemeEditor", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "ClientThemeEditor must remain outside NosGm.sln."
}

Write-Host "NosGM.ClientThemeEditor attribution and safety checks passed."
