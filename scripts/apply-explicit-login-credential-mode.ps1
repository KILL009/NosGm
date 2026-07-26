$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Replace-ExactOnce {
    param(
        [string]$Content,
        [string]$Old,
        [string]$New,
        [string]$Description,
        [string]$NewLine
    )

    $oldNormalized = [regex]::Replace($Old, "`r`n|`n|`r", $NewLine)
    $newNormalized = [regex]::Replace($New, "`r`n|`n|`r", $NewLine)
    $first = $Content.IndexOf($oldNormalized, [StringComparison]::Ordinal)
    if ($first -lt 0) {
        throw "Expected source not found: $Description"
    }

    $second = $Content.IndexOf($oldNormalized, $first + $oldNormalized.Length, [StringComparison]::Ordinal)
    if ($second -ge 0) {
        throw "Expected exactly one source match: $Description"
    }

    Write-Host "Applied: $Description"
    return $Content.Substring(0, $first) +
        $newNormalized +
        $Content.Substring($first + $oldNormalized.Length)
}

function Write-Utf8Bom {
    param(
        [string]$Path,
        [string]$Content
    )

    [IO.File]::WriteAllText(
        (Resolve-Path -LiteralPath $Path),
        $Content,
        (New-Object Text.UTF8Encoding($true)))
}

$configPath = "Data/NosGm.Configuration/ServerConfiguration.cs"
$configContent = Get-Content -LiteralPath $configPath -Raw
$configNewLine = if ($configContent.Contains("`r`n")) { "`r`n" } else { "`n" }
$configContent = Replace-ExactOnce $configContent @'
        public static bool UseOldCrypto = false;
        public static bool StartGlacernonAutomaticly = false;
'@ @'
        public static bool UseOldCrypto = false;
        public static bool LoginUsesPrehashedSha512 = true;
        public static bool StartGlacernonAutomaticly = false;
'@ "declare the login credential format explicitly" $configNewLine
Write-Utf8Bom $configPath $configContent

$corePath = "Data/NosGm.Core/Security/PasswordHashService.cs"
$coreContent = Get-Content -LiteralPath $corePath -Raw
$coreNewLine = if ($coreContent.Contains("`r`n")) { "`r`n" } else { "`n" }
$coreContent = Replace-ExactOnce $coreContent @'
        public static bool VerifyLoginPayload(
            string storedPassword,
            string packetPassword,
            bool useOldCrypto,
            out string clearPassword,
            out bool needsUpgrade)
'@ @'
        public static bool VerifyLoginPayload(
            string storedPassword,
            string packetPassword,
            bool useOldCrypto,
            bool loginUsesPrehashedSha512,
            out string clearPassword,
            out bool needsUpgrade)
'@ "add an explicit prehashed credential mode" $coreNewLine

$coreContent = Replace-ExactOnce $coreContent @'
            // Current clients can send the SHA-512 credential directly. In that path the
            // server does not know the original password, so it must not attempt PBKDF2 migration.
            if (TryVerifyPrehashedSha512(storedPassword, packetPassword))
            {
                return true;
            }

            if (!useOldCrypto)
            {
                return TryVerifyCandidate(
                    storedPassword,
                    packetPassword,
                    false,
                    out clearPassword,
                    out needsUpgrade);
            }

            if (LooksLikeLegacyPasswordPayload(packetPassword) &&
                TryDecodeLegacyPassword(packetPassword, out string decodedPassword) &&
                TryVerifyCandidate(
                    storedPassword,
                    decodedPassword,
                    true,
                    out clearPassword,
                    out needsUpgrade))
            {
                return true;
            }

            return TryVerifyCandidate(
                storedPassword,
                packetPassword,
                true,
                out clearPassword,
                out needsUpgrade);
'@ @'
            if (useOldCrypto)
            {
                if (LooksLikeLegacyPasswordPayload(packetPassword) &&
                    TryDecodeLegacyPassword(packetPassword, out string decodedPassword) &&
                    TryVerifyCandidate(
                        storedPassword,
                        decodedPassword,
                        true,
                        out clearPassword,
                        out needsUpgrade))
                {
                    return true;
                }

                return TryVerifyCandidate(
                    storedPassword,
                    packetPassword,
                    true,
                    out clearPassword,
                    out needsUpgrade);
            }

            // The prehashed route is enabled only by explicit deployment configuration.
            // It is never available to the legacy decoder path, where a stored digest must
            // not become a reusable login credential.
            if (loginUsesPrehashedSha512)
            {
                return TryVerifyPrehashedSha512(storedPassword, packetPassword);
            }

            return TryVerifyCandidate(
                storedPassword,
                packetPassword,
                false,
                out clearPassword,
                out needsUpgrade);
'@ "separate legacy, prehashed and clear credential modes" $coreNewLine
Write-Utf8Bom $corePath $coreContent

$handlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs"
$handlerContent = Get-Content -LiteralPath $handlerPath -Raw
$handlerNewLine = if ($handlerContent.Contains("`r`n")) { "`r`n" } else { "`n" }
$handlerContent = Replace-ExactOnce $handlerContent @'
                    loginPacket.Password,
                    ServerConfiguration.UseOldCrypto,
                    out string clearPassword,
'@ @'
                    loginPacket.Password,
                    ServerConfiguration.UseOldCrypto,
                    ServerConfiguration.LoginUsesPrehashedSha512,
                    out string clearPassword,
'@ "pass the configured login credential format" $handlerNewLine
Write-Utf8Bom $handlerPath $handlerContent

$testPath = "scripts/verify-password-hashing.ps1"
$testContent = Get-Content -LiteralPath $testPath -Raw
$testNewLine = if ($testContent.Contains("`r`n")) { "`r`n" } else { "`n" }

$modernOld = "        `$false,$testNewLine        [ref]`$resolvedPassword,"
$modernNew = "        `$false,$testNewLine        `$true,$testNewLine        [ref]`$resolvedPassword,"
$modernCount = ([regex]::Matches($testContent, [regex]::Escape($modernOld))).Count
if ($modernCount -ne 3) {
    throw "Expected three modern credential test invocations, found $modernCount."
}
$testContent = $testContent.Replace($modernOld, $modernNew)

$legacyOld = "        `$true,$testNewLine        [ref]`$resolvedPassword,"
$legacyNew = "        `$true,$testNewLine        `$false,$testNewLine        [ref]`$resolvedPassword,"
$legacyCount = ([regex]::Matches($testContent, [regex]::Escape($legacyOld))).Count
if ($legacyCount -ne 3) {
    throw "Expected three legacy credential test invocations, found $legacyCount."
}
$testContent = $testContent.Replace($legacyOld, $legacyNew)

$testContent = Replace-ExactOnce $testContent @'
$resolvedPassword = $null
$needsUpgrade = $false
if ([NosGm.Core.PasswordHashService]::VerifyLoginPayload(
        $firstHash,
        $legacySha512,
        $false,
        $true,
        [ref]$resolvedPassword,
        [ref]$needsUpgrade)) {
    throw "A versioned hash incorrectly accepted a legacy prehashed SHA-512 payload."
}

'@ @'
$resolvedPassword = $null
$needsUpgrade = $false
if ([NosGm.Core.PasswordHashService]::VerifyLoginPayload(
        $firstHash,
        $legacySha512,
        $false,
        $true,
        [ref]$resolvedPassword,
        [ref]$needsUpgrade)) {
    throw "A versioned hash incorrectly accepted a legacy prehashed SHA-512 payload."
}

$resolvedPassword = $null
$needsUpgrade = $false
if ([NosGm.Core.PasswordHashService]::VerifyLoginPayload(
        $legacySha512,
        $upperLegacySha512,
        $true,
        $true,
        [ref]$resolvedPassword,
        [ref]$needsUpgrade)) {
    throw "The legacy crypto mode accepted a replayed stored SHA-512 credential."
}

$longHexPassword = "a" * 128
$resolvedPassword = $null
$needsUpgrade = $false
if (-not [NosGm.Core.PasswordHashService]::VerifyLoginPayload(
        $longHexPassword,
        $longHexPassword,
        $false,
        $false,
        [ref]$resolvedPassword,
        [ref]$needsUpgrade) -or
    $resolvedPassword -ne $longHexPassword -or
    -not $needsUpgrade) {
    throw "The explicit clear-password mode did not preserve migration for a 128-character hexadecimal password."
}

'@ "cover explicit credential mode boundaries" $testNewLine
Write-Utf8Bom $testPath $testContent

Write-Host "Explicit login credential mode applied successfully."
