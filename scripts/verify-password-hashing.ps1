param(
    [string]$CryptographyBasePath = "Data/NosGm.Core/Cryptography/CryptographyBase.cs",
    [string]$PasswordHashServicePath = "Data/NosGm.Core/Security/PasswordHashService.cs"
)

$ErrorActionPreference = "Stop"

foreach ($path in @($CryptographyBasePath, $PasswordHashServicePath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Password hashing source not found: $path"
    }
}

Add-Type -Path @($CryptographyBasePath, $PasswordHashServicePath)

$password = "NosGM-Passw0rd-ñ-测试"
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

$legacySha512 = [NosGm.Core.CryptographyBase]::Sha512($password).ToUpperInvariant()
$needsUpgrade = $false
if (-not [NosGm.Core.PasswordHashService]::VerifyPassword($legacySha512, $password, $true, [ref]$needsUpgrade) -or -not $needsUpgrade) {
    throw "A valid legacy SHA-512 password was not accepted for migration."
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

Write-Host "Password hashing verified: salted PBKDF2-SHA256, legacy compatibility and rehash detection."
