$ErrorActionPreference = 'Stop'

function Fail([string]$Message) {
    Write-Error $Message
    exit 1
}

$requiredFiles = @(
    'LICENSE',
    'AUTHORS.md',
    'NOTICE.md',
    'THIRD_PARTY_NOTICES.md',
    'docs/LICENSING.md',
    'docs/PROVENANCE.md'
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        Fail "Required licensing file is missing: $file"
    }
}

$licenseText = Get-Content -LiteralPath 'LICENSE' -Raw
if ($licenseText -notmatch 'GNU GENERAL PUBLIC LICENSE') {
    Fail 'LICENSE does not contain the expected GNU General Public License text.'
}

# Renaming a project in 2026 must not transfer copyright for code authored earlier.
$falseHistoricalClaims = @(
    & git grep -n -I -i -E 'NosGm Team Copyright[^\r\n]*(2016|2017|2018|2019|2020|2021|2022|2023|2024|2025)' -- '*.cs' '*.csproj' '*.props' '*.targets' 2>$null
)

if ($LASTEXITCODE -gt 1) {
    Fail 'git grep failed while checking historical copyright claims.'
}

if ($falseHistoricalClaims.Count -gt 0) {
    Write-Host 'Invalid historical copyright claims found:'
    $falseHistoricalClaims | ForEach-Object { Write-Host "  $_" }
    Fail 'NosGM may claim its modifications, but it must not claim upstream work from earlier years.'
}

$contextPath = 'Data/NosGm.DAL/NosGm.DAL.EF/Context/OpenNosContext.cs'
if (Test-Path -LiteralPath $contextPath) {
    $contextText = Get-Content -LiteralPath $contextPath -Raw
    if ($contextText -notmatch 'derived from the OpenNos Emulator Project') {
        Fail "$contextPath must preserve its OpenNos-derived attribution notice."
    }

    if ($contextText -notmatch 'Modified by the NosGM project') {
        Fail "$contextPath must state that NosGM modified the inherited file."
    }
}

$noticeText = Get-Content -LiteralPath 'NOTICE.md' -Raw
foreach ($requiredNotice in @('OpenNos', 'Frostvein', 'ChickenAPI', 'No affiliation', 'No warranty')) {
    if ($noticeText -notmatch [regex]::Escape($requiredNotice)) {
        Fail "NOTICE.md is missing required provenance or legal text: $requiredNotice"
    }
}

Write-Host 'License compliance checks passed.'
