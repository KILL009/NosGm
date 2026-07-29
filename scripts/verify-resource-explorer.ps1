$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    Write-Error $Message
    exit 1
}

$root = 'Tools/NosGM.ResourceExplorer'
$requiredFiles = @(
    "$root/NosGM.ResourceExplorer.csproj",
    "$root/Program.cs",
    "$root/ArchiveReader.cs",
    "$root/TextDecryptors.cs",
    "$root/ExtractionSandbox.cs",
    "$root/README.md",
    "$root/NOTICE.md",
    "$root/LICENSE"
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        Fail "Required resource explorer file is missing: $file"
    }
}

$notice = Get-Content -LiteralPath "$root/NOTICE.md" -Raw
foreach ($text in @(
    'Pumba98/OnexExplorer',
    'eaee2aa9f0e71b9960da586f425f79e628013021',
    'Boost Software License 1.0',
    'respective upstream contributors'
)) {
    if ($notice -notmatch [regex]::Escape($text)) {
        Fail "Resource explorer notice is missing required attribution: $text"
    }
}

$license = Get-Content -LiteralPath "$root/LICENSE" -Raw
if ($license -notmatch 'Boost Software License - Version 1.0') {
    Fail 'The preserved Boost Software License text is missing.'
}

$project = Get-Content -LiteralPath "$root/NosGM.ResourceExplorer.csproj" -Raw
foreach ($text in @('net10.0', 'BSL-1.0', 'TreatWarningsAsErrors')) {
    if ($project -notmatch [regex]::Escape($text)) {
        Fail "Resource explorer project is missing: $text"
    }
}

$allSource = Get-ChildItem -LiteralPath $root -Filter '*.cs' -File | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }
$joined = $allSource -join "`n"
foreach ($forbidden in @(
    'FileMode.OpenOrCreate',
    'FileMode.Truncate',
    'Process.Start',
    'HttpClient',
    'Socket',
    'WebClient',
    'DllImport',
    'VirtualProtect',
    'WriteProcessMemory'
)) {
    if ($joined -match [regex]::Escape($forbidden)) {
        Fail "Forbidden write, network or process primitive found: $forbidden"
    }
}

if ($joined -notmatch 'ZLibStream' -or $joined -notmatch 'SHA256' -or $joined -notmatch 'GetFullPath') {
    Fail 'Expected decompression, hashing or path-sandbox safeguards are missing.'
}

if (Test-Path -LiteralPath 'NosGm.sln') {
    $solution = Get-Content -LiteralPath 'NosGm.sln' -Raw
    if ($solution -match 'NosGM.ResourceExplorer') {
        Fail 'NosGM.ResourceExplorer must remain outside NosGm.sln.'
    }
}

$trackedProprietary = @(& git ls-files '*.NOS' '*.nos' '*.bin' '*.pak' '*.dat' 2>$null)
if ($LASTEXITCODE -ne 0) {
    Fail 'git ls-files failed while checking proprietary resources.'
}
$toolTracked = $trackedProprietary | Where-Object { $_ -like "$root/*" }
if ($toolTracked.Count -gt 0) {
    Fail "Resource explorer must not contain client archives or extracted resources: $($toolTracked -join ', ')"
}

Write-Host 'NosGM.ResourceExplorer attribution and safety checks passed.'
