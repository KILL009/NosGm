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
    & git grep -n -I -i -E 'NosGm Team Copyright.*(2016|2017|2018|2019|2020|2021|2022|2023|2024|2025)' -- '*.cs' '*.csproj' '*.props' '*.targets' 2>$null
)

if ($LASTEXITCODE -gt 1) {
    Fail 'git grep failed while checking historical copyright claims.'
}

if ($falseHistoricalClaims.Count -gt 0) {
    Write-Host 'Invalid historical copyright claims found:'
    $falseHistoricalClaims | ForEach-Object { Write-Host "  $_" }
    Fail 'NosGM may claim its modifications, but it must not claim upstream work from earlier years.'
}

# Files whose OpenNos project header was renamed to NosGM need a file-specific
# sidecar notice restoring upstream authorship and the original GPL option.
$renamedHeaderFiles = @(
    & git grep -l -I -F 'This file is part of the NosGm Emulator Project' -- '*.cs' 2>$null
)

if ($LASTEXITCODE -gt 1) {
    Fail 'git grep failed while locating renamed source headers.'
}

foreach ($sourcePath in $renamedHeaderFiles) {
    $sidecarPath = "$sourcePath.license"
    if (-not (Test-Path -LiteralPath $sidecarPath -PathType Leaf)) {
        Fail "$sourcePath has a renamed inherited header but no $sidecarPath attribution notice."
    }

    $sidecarText = Get-Content -LiteralPath $sidecarPath -Raw
    foreach ($requiredText in @('OpenNos contributors', 'SPDX-License-Identifier: GPL-2.0-or-later')) {
        if ($sidecarText -notmatch [regex]::Escape($requiredText)) {
            Fail "$sidecarPath is missing required attribution text: $requiredText"
        }
    }
}

$contextPath = 'Data/NosGm.DAL/NosGm.DAL.EF/Context/OpenNosContext.cs'
$contextLicensePath = "$contextPath.license"
if (Test-Path -LiteralPath $contextPath) {
    if (-not (Test-Path -LiteralPath $contextLicensePath -PathType Leaf)) {
        Fail "$contextLicensePath must preserve file-specific attribution for the inherited context."
    }

    $contextLicenseText = Get-Content -LiteralPath $contextLicensePath -Raw
    foreach ($requiredText in @(
        'OpenNos Emulator Project',
        'SPDX-License-Identifier: GPL-2.0-or-later',
        '2026 NosGM contributors',
        '2026-07-22'
    )) {
        if ($contextLicenseText -notmatch [regex]::Escape($requiredText)) {
            Fail "$contextLicensePath is missing required attribution text: $requiredText"
        }
    }
}

$noticeText = Get-Content -LiteralPath 'NOTICE.md' -Raw
foreach ($requiredNotice in @('OpenNos', 'Frostvein', 'ChickenAPI', 'No affiliation', 'No warranty')) {
    if ($noticeText -notmatch [regex]::Escape($requiredNotice)) {
        Fail "NOTICE.md is missing required provenance or legal text: $requiredNotice"
    }
}

Write-Host 'License compliance checks passed.'
