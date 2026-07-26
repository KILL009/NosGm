param(
    [string]$AssemblyPath = "Data/NosGm.Core/bin/Release/NosGm.Core.dll"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $AssemblyPath)) {
    throw "Built NosGm.Core assembly not found: $AssemblyPath"
}

[Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path) | Out-Null

function Get-Sha512Hex {
    param([string]$Value)

    $sha512 = [System.Security.Cryptography.SHA512]::Create()
    try {
        $bytes = $sha512.ComputeHash([Text.Encoding]::UTF8.GetBytes($Value))
    }
    finally {
        $sha512.Dispose()
    }

    return -join ($bytes | ForEach-Object { $_.ToString("x2") })
}

$password = "NosGM-Passw0rd-2026"
$firstHash = $null
$secondHash = $null

if (-not [NosGm.Core.PasswordHashService]::TryHashPassword($password, [ref]$firstHash)) {
    throw "Unable to create the first password hash."
}

if (-not [NosGm.Core.PasswordHashService]::TryHashPassword($password, [ref]$secondHash)) {
    throw "Unable to create the second password hash."
}

if ($firstHash -eq $secondHash) {
    throw "Unique password salts were not applied."
}

if ($firstHash.Length -gt 255 -or $secondHash.Length -gt 255) {
    throw "The encoded password hash does not fit the Account.Password column."
}

$needsUpgrade = $false
if (-not [NosGm.Core.PasswordHashService]::VerifyPassword($firstHash, $password, $false, [ref]$needsUpgrade)) {
    throw "The correct password did not verify."
}

if ($needsUpgrade) {
    throw "A current password hash was incorrectly marked for upgrade."
}

$needsUpgrade = $false
if ([NosGm.Core.PasswordHashService]::VerifyPassword($firstHash, "wrong-password", $false, [ref]$needsUpgrade)) {
    throw "An incorrect password verified successfully."
}

$needsUpgrade = $false
if ([NosGm.Core.PasswordHashService]::VerifyPassword("nosgm`$broken", $password, $false, [ref]$needsUpgrade)) {
    throw "A malformed versioned hash verified successfully."
}

$legacySha512 = Get-Sha512Hex $password
$needsUpgrade = $false
if (-not [NosGm.Core.PasswordHashService]::VerifyPassword($legacySha512, $password, $true, [ref]$needsUpgrade) -or -not $needsUpgrade) {
    throw "A valid legacy SHA-512 password was not accepted for migration."
}

$resolvedPassword = $null
$needsUpgrade = $true
$upperLegacySha512 = $legacySha512.ToUpperInvariant()
if (-not [NosGm.Core.PasswordHashService]::VerifyLoginPayload(
        $legacySha512,
        $upperLegacySha512,
        $false,
        [ref]$resolvedPassword,
        [ref]$needsUpgrade) -or
    $null -ne $resolvedPassword -or
    $needsUpgrade) {
    throw "A prehashed SHA-512 login payload was not accepted case-insensitively without migration."
}

$resolvedPassword = $null
$needsUpgrade = $false
$wrongPrehash = Get-Sha512Hex "wrong-password"
if ([NosGm.Core.PasswordHashService]::VerifyLoginPayload(
        $legacySha512,
        $wrongPrehash,
        $false,
        [ref]$resolvedPassword,
        [ref]$needsUpgrade)) {
    throw "An incorrect prehashed SHA-512 login payload verified successfully."
}

$resolvedPassword = $null
$needsUpgrade = $false
if (-not [NosGm.Core.PasswordHashService]::VerifyLoginPayload(
        $legacySha512,
        $password,
        $true,
        $false,
        [ref]$resolvedPassword,
        [ref]$needsUpgrade) -or
    $resolvedPassword -ne $password -or
    -not $needsUpgrade) {
    throw "A plain current-client password payload was not accepted with UseOldCrypto enabled."
}

$hexPassword = "abcdef"
$hexLegacySha512 = Get-Sha512Hex $hexPassword
$resolvedPassword = $null
$needsUpgrade = $false
if (-not [NosGm.Core.PasswordHashService]::VerifyLoginPayload(
        $hexLegacySha512,
        $hexPassword,
        $true,
        $false,
        [ref]$resolvedPassword,
        [ref]$needsUpgrade) -or
    $resolvedPassword -ne $hexPassword) {
    throw "A plain hexadecimal password was mistaken for a legacy encoded payload."
}

$resolvedPassword = $null
$needsUpgrade = $false
if (-not [NosGm.Core.PasswordHashService]::VerifyLoginPayload(
        $firstHash,
        $password,
        $true,
        $false,
        [ref]$resolvedPassword,
        [ref]$needsUpgrade) -or
    $resolvedPassword -ne $password -or
    $needsUpgrade) {
    throw "A versioned hash did not accept a plain payload with UseOldCrypto enabled."
}

$resolvedPassword = $null
$needsUpgrade = $false
if ([NosGm.Core.PasswordHashService]::VerifyLoginPayload(
        $firstHash,
        $legacySha512,
        $false,
        [ref]$resolvedPassword,
        [ref]$needsUpgrade)) {
    throw "A versioned hash incorrectly accepted a legacy prehashed SHA-512 payload."
}

$legacyIterations = 10000
$salt = [byte[]](0..15)
$derive = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
    $password,
    $salt,
    $legacyIterations,
    [System.Security.Cryptography.HashAlgorithmName]::SHA256)
try {
    $legacyHashBytes = $derive.GetBytes(32)
}
finally {
    $derive.Dispose()
}

$legacyVersionedHash = [string]::Join(
    '$',
    @(
        'nosgm',
        'pbkdf2-sha256',
        'v1',
        $legacyIterations.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        [Convert]::ToBase64String($salt),
        [Convert]::ToBase64String($legacyHashBytes)
    ))

$needsUpgrade = $false
if (-not [NosGm.Core.PasswordHashService]::VerifyPassword($legacyVersionedHash, $password, $false, [ref]$needsUpgrade) -or -not $needsUpgrade) {
    throw "A valid lower-cost hash was not marked for upgrade."
}

Write-Host "Password hashing and explicit login credential modes verified against the built NosGm.Core assembly."
