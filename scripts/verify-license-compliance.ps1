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
    'docs/PROVENANCE.md',
    'Data/NosGm.ChickenAPI/NOTICE.md',
    'LICENSES/GPL-3.0-only/README.md',
    'LICENSES/GPL-3.0-only/01.txt',
    'LICENSES/GPL-3.0-only/02.txt',
    'LICENSES/GPL-3.0-only/03.txt',
    'LICENSES/GPL-3.0-only/04.txt'
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        Fail "Required licensing file is missing: $file"
    }
}

$licenseText = Get-Content -LiteralPath 'LICENSE' -Raw
if ($licenseText -notmatch 'GNU GENERAL PUBLIC LICENSE' -or $licenseText -notmatch 'Version 2, June 1991') {
    Fail 'LICENSE does not contain the preserved GNU General Public License version 2 text.'
}

# The complete GPLv3 text is stored in numbered pieces due to the connector's
# per-write payload limit. Concatenating the raw bytes must reproduce the
# canonical license byte-for-byte.
$gpl3Parts = @(
    'LICENSES/GPL-3.0-only/01.txt',
    'LICENSES/GPL-3.0-only/02.txt',
    'LICENSES/GPL-3.0-only/03.txt',
    'LICENSES/GPL-3.0-only/04.txt'
)
$gpl3Stream = [System.IO.MemoryStream]::new()
try {
    foreach ($part in $gpl3Parts) {
        $bytes = [System.IO.File]::ReadAllBytes($part)
        $gpl3Stream.Write($bytes, 0, $bytes.Length)
    }

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $gpl3Hash = (($sha256.ComputeHash($gpl3Stream.ToArray()) | ForEach-Object { $_.ToString('x2') }) -join '')
    }
    finally {
        $sha256.Dispose()
    }
}
finally {
    $gpl3Stream.Dispose()
}

$expectedGpl3Hash = '3972dc9744f6499f0f9b2dbf76696f2ae7ad8af9b23dde66d6af86c9dfb36986'
if ($gpl3Hash -ne $expectedGpl3Hash) {
    Fail "Bundled GPLv3 text is incomplete or modified. Expected $expectedGpl3Hash but found $gpl3Hash."
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

$chickenNoticeText = Get-Content -LiteralPath 'Data/NosGm.ChickenAPI/NOTICE.md' -Raw
foreach ($requiredChickenText in @(
    'Price-H16/NQ-Verde',
    '2594ec13f4fba5d893b424197878c05f801f68a2',
    'GPL-3.0-only',
    '8D199C92-D754-461F-89B0-83C2B4E6DF9F',
    'DECB5668-600C-49F1-A5C8-CDE5A12C9F5A',
    'A8289E58-4507-4614-8A28-9FF936BEE009'
)) {
    if ($chickenNoticeText -notmatch [regex]::Escape($requiredChickenText)) {
        Fail "ChickenAPI notice is missing required provenance text: $requiredChickenText"
    }
}

$noticeText = Get-Content -LiteralPath 'NOTICE.md' -Raw
foreach ($requiredNotice in @('OpenNos', 'Frostvein', 'ChickenAPI', 'GPL-3.0-only', 'No affiliation', 'No warranty')) {
    if ($noticeText -notmatch [regex]::Escape($requiredNotice)) {
        Fail "NOTICE.md is missing required provenance or legal text: $requiredNotice"
    }
}

Write-Host 'License compliance checks passed.'
