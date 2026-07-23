$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$launcher = Join-Path $root "Launcher"

$required = @(
    "LICENSE",
    "NOTICE.md",
    "README.md",
    "NosGM.Launcher.sln",
    "src/NosGM.Updater.Core/NosGM.Updater.Core.csproj",
    "src/NosGM.ManifestBuilder/NosGM.ManifestBuilder.csproj",
    "src/NosGM.Launcher/NosGM.Launcher.csproj",
    "tests/NosGM.Updater.SelfTest/NosGM.Updater.SelfTest.csproj"
)

foreach ($path in $required) {
    if (-not (Test-Path (Join-Path $launcher $path))) {
        throw "Missing required launcher file: $path"
    }
}

$notice = Get-Content (Join-Path $launcher "NOTICE.md") -Raw
foreach ($needle in @(
    "Mati18505/HexTaleLauncher",
    "50aa50580aa35a45b156a1899a340a25e50f7fb5",
    "no HexTaleLauncher source code",
    "ECDSA P-256 / SHA-256"
)) {
    if (-not $notice.Contains($needle, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Launcher notice is missing: $needle"
    }
}

$sourceFiles = Get-ChildItem $launcher -Filter *.cs -Recurse
$source = ($sourceFiles | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"

foreach ($forbidden in @(
    "MD5.Create",
    "HashAlgorithmName.MD5",
    "Verb = \"runas\"",
    "AllowAutoRedirect = true",
    "WriteProcessMemory",
    "VirtualProtect",
    "DllImport",
    "ffi-napi",
    "hextale.xyz"
)) {
    if ($source.Contains($forbidden, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Forbidden launcher primitive or upstream endpoint found: $forbidden"
    }
}

foreach ($requiredCode in @(
    "ECDSA_P256_SHA256",
    "SHA256",
    "ResolveManagedPath",
    "EnsureNoReparsePoints",
    "AllowAutoRedirect = false",
    "CheckCertificateRevocationList = true",
    "transactions",
    "rollback",
    "IgnoredDeletes",
    "UseShellExecute = true"
)) {
    if (-not $source.Contains($requiredCode, [System.StringComparison]::Ordinal)) {
        throw "Required launcher safety control missing: $requiredCode"
    }
}

$privateKeyMarkers = @(
    "BEGIN EC PRIVATE KEY",
    "BEGIN PRIVATE KEY",
    "BEGIN ENCRYPTED PRIVATE KEY"
)
foreach ($file in Get-ChildItem $launcher -File -Recurse) {
    if ($file.Length -gt 4MB) {
        continue
    }

    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    foreach ($marker in $privateKeyMarkers) {
        if ($content -and $content.Contains($marker, [System.StringComparison]::Ordinal)) {
            throw "Private signing key material found in repository: $($file.FullName)"
        }
    }
}

$proprietary = Get-ChildItem $launcher -File -Recurse | Where-Object {
    $_.Extension -in @(".exe", ".dll", ".nos", ".pak", ".bin")
}
if ($proprietary) {
    throw "Binary or proprietary-looking client material found under Launcher: $($proprietary.FullName -join ', ')"
}

$serverSolution = Get-Content (Join-Path $root "NosGm.sln") -Raw
if ($serverSolution.Contains("NosGM.Launcher", [System.StringComparison]::OrdinalIgnoreCase) -or
    $serverSolution.Contains("NosGM.Updater.Core", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Launcher projects must remain outside the NosGM server solution."
}

Write-Host "NosGM Launcher attribution and safety checks passed."
