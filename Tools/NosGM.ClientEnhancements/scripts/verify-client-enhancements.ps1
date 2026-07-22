$ErrorActionPreference = 'Stop'

$toolRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Resolve-Path (Join-Path $toolRoot '..\..')
$expectedCommit = 'fc1b6dda5d797efc24a053180d30702f8dad162a'

$requiredFiles = @(
    'CMakeLists.txt',
    'README.md',
    'NOTICE.md',
    'LICENSE',
    'include\nosgm\client_enhancements\Pattern.h',
    'include\nosgm\client_enhancements\Profile.h',
    'include\nosgm\client_enhancements\ClientIdentity.h',
    'include\nosgm\client_enhancements\MemoryGuard.h',
    'include\nosgm\client_enhancements\PatchTransaction.h',
    'src\Pattern.cpp',
    'src\Profile.cpp',
    'src\ClientIdentity.cpp',
    'src\MemoryGuard.cpp',
    'src\PatchTransaction.cpp',
    'src\Module.cpp',
    'probe\Main.cpp',
    'tests\Tests.cpp',
    'profiles\client-0.9.3.3255.template.ini'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $toolRoot $relativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required file is missing: $relativePath"
    }
}

$notice = Get-Content (Join-Path $toolRoot 'NOTICE.md') -Raw
$readme = Get-Content (Join-Path $toolRoot 'README.md') -Raw
$license = Get-Content (Join-Path $toolRoot 'LICENSE') -Raw
$cmake = Get-Content (Join-Path $toolRoot 'CMakeLists.txt') -Raw
$module = Get-Content (Join-Path $toolRoot 'src\Module.cpp') -Raw
$template = Get-Content (Join-Path $toolRoot 'profiles\client-0.9.3.3255.template.ini') -Raw

if ($notice -notmatch [regex]::Escape($expectedCommit)) {
    throw 'NOTICE.md does not preserve the reviewed NostaleWidget commit.'
}
if ($notice -notmatch 'ApourtArtt' -or $license -notmatch 'Copyright \(c\) 2022 ApourtArtt') {
    throw 'The original ApourtArtt copyright notice is missing.'
}
if ($license -notmatch 'MIT License') {
    throw 'The component MIT license is missing.'
}

$sourceFiles = Get-ChildItem $toolRoot -Recurse -File -Include *.h,*.hpp,*.cpp,*.cxx
foreach ($file in $sourceFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -notmatch 'SPDX-License-Identifier: MIT') {
        throw "Missing MIT SPDX header: $($file.FullName)"
    }
}

if ($cmake -notmatch 'CMAKE_SIZEOF_VOID_P EQUAL 4' -or $readme -notmatch '\-A Win32') {
    throw 'The x86-only build guard is missing.'
}
if ($module -match 'CreateThread|LoadLibrary|CreateRemoteThread|WriteProcessMemory|OpenProcess') {
    throw 'The module contains loader or injection behavior.'
}
if ($module -notmatch 'DisableThreadLibraryCalls' -or $module -match 'DLL_PROCESS_ATTACH[\s\S]{0,300}CreateThread') {
    throw 'DllMain is not minimal and passive.'
}

$codeText = ($sourceFiles | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"
$forbiddenCode = @(
    'PacketManager',
    'detourSendFunc',
    'detourRcvdFunc',
    'SendPacket',
    'ReceivePacket',
    'allow_unverified_client',
    '858502310669582346'
)
foreach ($term in $forbiddenCode) {
    if ($codeText -match [regex]::Escape($term)) {
        throw "Forbidden client-modification term found in code: $term"
    }
}

$featureKeys = @(
    'enable_discord_presence',
    'enable_cooldown_labels',
    'enable_fps_control',
    'enable_resolution_manager',
    'enable_minimized_rendering'
)
foreach ($key in $featureKeys) {
    if ($template -notmatch "(?m)^$key=false\r?$") {
        throw "Unsafe default in client profile template: $key"
    }
}

$solutionPath = Join-Path $repoRoot 'NosGm.sln'
if (Select-String -Path $solutionPath -Pattern 'NosGM.ClientEnhancements' -Quiet) {
    throw 'NosGM.ClientEnhancements must stay outside NosGm.sln.'
}

$binaryExtensions = @('*.exe', '*.dll', '*.bin', '*.dmp', '*.pak')
foreach ($pattern in $binaryExtensions) {
    $found = Get-ChildItem $toolRoot -Recurse -File -Filter $pattern |
        Where-Object { $_.FullName -notmatch '[\\/]build([\\/]|$)' }
    if ($found) {
        throw "Binary or proprietary-looking file committed under tool source: $($found.FullName -join ', ')"
    }
}

$provenanceFiles = @(
    (Join-Path $repoRoot 'AUTHORS.md'),
    (Join-Path $repoRoot 'NOTICE.md'),
    (Join-Path $repoRoot 'THIRD_PARTY_NOTICES.md'),
    (Join-Path $repoRoot 'docs\PROVENANCE.md'),
    (Join-Path $repoRoot 'docs\LICENSING.md')
)
foreach ($path in $provenanceFiles) {
    $content = Get-Content $path -Raw
    if ($content -notmatch [regex]::Escape($expectedCommit)) {
        throw "Provenance file does not contain the exact upstream commit: $path"
    }
}

Write-Host 'NosGM.ClientEnhancements attribution and safety checks passed.'
