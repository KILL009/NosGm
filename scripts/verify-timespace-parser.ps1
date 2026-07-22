$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    Write-Error $Message
    exit 1
}

$requiredFiles = @(
    'Tools/NosGM.TimeSpaceParser/NosGM.TimeSpaceParser.csproj',
    'Tools/NosGM.TimeSpaceParser/Program.cs',
    'Tools/NosGM.TimeSpaceParser/CliOptions.cs',
    'Tools/NosGM.TimeSpaceParser/Model.cs',
    'Tools/NosGM.TimeSpaceParser/CaptureParser.cs',
    'Tools/NosGM.TimeSpaceParser/XmlOutput.cs',
    'Tools/NosGM.TimeSpaceParser/README.md',
    'Tools/NosGM.TimeSpaceParser/NOTICE.md',
    'Tools/NosGM.TimeSpaceParser/Samples/packet.sample.txt',
    '.github/workflows/validate-timespace-parser.yml'
)

foreach ($path in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Fail "Required NosGM.TimeSpaceParser file is missing: $path"
    }
}

$upstreamCommit = '36bd96a51c4b4a1e55e8827ca7eb375cc189ad9e'
$notice = Get-Content -LiteralPath 'Tools/NosGM.TimeSpaceParser/NOTICE.md' -Raw
foreach ($requiredText in @(
    'noszanou/OpennosTimeSpaceParser',
    $upstreamCommit,
    'Elendan/TimeSpace-Generator',
    'SEOVA',
    'OpenNos',
    'GPL-3.0-only',
    'NosGM contributors'
)) {
    if (-not $notice.Contains($requiredText)) {
        Fail "NosGM.TimeSpaceParser NOTICE.md is missing attribution text: $requiredText"
    }
}

$project = Get-Content -LiteralPath 'Tools/NosGM.TimeSpaceParser/NosGM.TimeSpaceParser.csproj' -Raw
foreach ($requiredText in @('net9.0', 'GPL-3.0-only', 'Elendan', 'SEOVA', 'noszanou', 'OpenNos contributors', 'NosGM contributors')) {
    if (-not $project.Contains($requiredText)) {
        Fail "NosGM.TimeSpaceParser.csproj is missing framework, attribution or license text: $requiredText"
    }
}

$sourceFiles = @(Get-ChildItem -LiteralPath 'Tools/NosGM.TimeSpaceParser' -Filter '*.cs' -File -Recurse | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
foreach ($sourceFile in $sourceFiles) {
    $source = Get-Content -LiteralPath $sourceFile.FullName -Raw
    foreach ($requiredText in @(
        'SPDX-License-Identifier: GPL-3.0-only',
        'Elendan',
        'SEOVA',
        'noszanou',
        'OpenNos',
        'NosGM contributors'
    )) {
        if (-not $source.Contains($requiredText)) {
            Fail "$($sourceFile.FullName) is missing attribution or license text: $requiredText"
        }
    }
}

$solution = Get-Content -LiteralPath 'NosGm.sln' -Raw
if ($solution.Contains('NosGM.TimeSpaceParser', [System.StringComparison]::OrdinalIgnoreCase)) {
    Fail 'NosGM.TimeSpaceParser must remain outside the game-server solution.'
}

$allSource = ($sourceFiles | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
foreach ($forbiddenText in @(
    'HttpClient',
    'TcpClient',
    'System.Net.Sockets',
    'PacketDotNet',
    'SharpPcap',
    'Process.GetProcesses'
)) {
    if ($allSource.Contains($forbiddenText, [System.StringComparison]::Ordinal)) {
        Fail "NosGM.TimeSpaceParser must not capture traffic or connect to clients/servers: $forbiddenText"
    }
}

$program = Get-Content -LiteralPath 'Tools/NosGM.TimeSpaceParser/Program.cs' -Raw
foreach ($requiredText in @('"parse"', '"batch"', '"validate"', '"self-test"', '--strict', '--force')) {
    if (-not $program.Contains($requiredText)) {
        Fail "NosGM.TimeSpaceParser CLI is missing required behavior: $requiredText"
    }
}

$parser = Get-Content -LiteralPath 'Tools/NosGM.TimeSpaceParser/CaptureParser.cs' -Raw
if ($parser.Contains('Gold = 1500') -or $parser.Contains('Reputation = 50')) {
    Fail 'The parser must not retain arbitrary reward defaults from the reviewed upstream implementation.'
}
if (-not $parser.Contains('PORTAL_DESTINATION_INFERRED')) {
    Fail 'The parser must report inferred portal destinations.'
}

$sample = Get-Content -LiteralPath 'Tools/NosGM.TimeSpaceParser/Samples/packet.sample.txt' -Raw
if (-not $sample.Contains('Synthetic non-proprietary capture')) {
    Fail 'The committed sample must explicitly identify itself as synthetic and non-proprietary.'
}

$unexpectedFiles = @(git ls-files 'Tools/NosGM.TimeSpaceParser' | Where-Object {
    $_ -notmatch '\.(cs|csproj|md|txt)$'
})
if ($unexpectedFiles.Count -gt 0) {
    $unexpectedFiles | ForEach-Object { Write-Host "unexpected: $_" }
    Fail 'NosGM.TimeSpaceParser contains an unexpected binary, archive or client asset.'
}

Write-Host 'NosGM.TimeSpaceParser safety and attribution checks passed.'
