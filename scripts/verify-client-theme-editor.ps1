$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$tool = Join-Path $root "Tools/NosGM.ClientThemeEditor"

$required = @(
    "LICENSE",
    "NOTICE.md",
    "README.md",
    "NosGM.ClientThemeEditor.csproj",
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
    "Pumba98/Nostale-ClientColorizer",
    "9d1e61c717b6a49ca221a5f2d855dfa5fa11591c",
    "no ClientColorizer source code is copied"
)) {
    if (-not $notice.Contains($needle, [System.StringComparison]::Ordinal)) {
        throw "ClientThemeEditor notice is missing: $needle"
    }
}

$project = Get-Content (Join-Path $tool "NosGM.ClientThemeEditor.csproj") -Raw
if (-not $project.Contains("<TargetFramework>net9.0</TargetFramework>")) {
    throw "ClientThemeEditor must target net9.0."
}

$source = Get-ChildItem $tool -Filter *.cs -Recurse | ForEach-Object {
    Get-Content $_.FullName -Raw
}
$joined = $source -join "`n"

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
    "WriteAtomically",
    "Restore"
)) {
    if (-not $joined.Contains($requiredCode, [System.StringComparison]::Ordinal)) {
        throw "Required ClientThemeEditor safety control missing: $requiredCode"
    }
}

$solution = Get-Content (Join-Path $root "NosGm.sln") -Raw
if ($solution.Contains("NosGM.ClientThemeEditor", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "ClientThemeEditor must remain outside NosGm.sln."
}

Write-Host "NosGM.ClientThemeEditor attribution and safety checks passed."
