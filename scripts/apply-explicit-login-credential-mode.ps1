$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Update-ExactFile {
    param(
        [string]$Path,
        [scriptblock]$Transform
    )

    $content = Get-Content -LiteralPath $Path -Raw
    $newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }

    function Replace-ExactOnce {
        param(
            [string]$Old,
            [string]$New,
            [string]$Description
        )

        $oldNormalized = [regex]::Replace($Old, "`r`n|`n|`r", $newLine)
        $newNormalized = [regex]::Replace($New, "`r`n|`n|`r", $newLine)
        $first = $script:content.IndexOf($oldNormalized, [StringComparison]::Ordinal)
        if ($first -lt 0) {
            throw "Expected source not found in ${Path}: $Description"
        }

        $second = $script:content.IndexOf($oldNormalized, $first + $oldNormalized.Length, [StringComparison]::Ordinal)
        if ($second -ge 0) {
            throw "Expected exactly one source match in ${Path}: $Description"
        }

        $script:content = $script:content.Substring(0, $first) +
            $newNormalized +
            $script:content.Substring($first + $oldNormalized.Length)
        Write-Host "Applied: $Description"
    }

    & $Transform
    [IO.File]::WriteAllText(
        (Resolve-Path -LiteralPath $Path),
        $content,
        (New-Object Text.UTF8Encoding($true)))
}

Update-ExactFile "Data/NosGm.Configuration/ServerConfiguration.cs" {
    Replace-ExactOnce @'
        public static bool UseOldCrypto = false;
        public static bool StartGlacernonAutomaticly = false;
'@ @'
        public static bool UseOldCrypto = false;
        public static bool LoginUsesPrehashedSha512 = true;
        public static bool StartGlacernonAutomaticly = false;
'@ "declare the login credential format explicitly"
}

Update-ExactFile "Data/NosGm.Core/Security/PasswordHashService.cs" {
    Replace-ExactOnce @'
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
'@ "add an explicit prehashed credential mode"

    Replace-ExactOnce @'
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
'@ "separate legacy, prehashed and clear credential modes"
}

Update-ExactFile "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs" {
    Replace-ExactOnce @'
                    loginPacket.Password,
                    ServerConfiguration.UseOldCrypto,
                    out string clearPassword,
'@ @'
                    loginPacket.Password,
                    ServerConfiguration.UseOldCrypto,
                    ServerConfiguration.LoginUsesPrehashedSha512,
                    out string clearPassword,
'@ "pass the configured login credential format"
}

Update-ExactFile "scripts/verify-password-hashing.ps1" {
    $script:content = $content

    $content = $content.Replace(
        "        `$false,`r`n        [ref]`$resolvedPassword,",
        "        `$false,`r`n        `$true,`r`n        [ref]`$resolvedPassword,")
    if ($content -eq $script:content) {
        $content = $script:content.Replace(
            "        `$false,`n        [ref]`$resolvedPassword,",
            "        `$false,`n        `$true,`n        [ref]`$resolvedPassword,")
    }
    if ($content -eq $script:content) {
        throw "Unable to update modern prehashed test invocations."
    }

    $beforeOldMode = $content
    $content = $content.Replace(
        "        `$true,`r`n        [ref]`$resolvedPassword,",
        "        `$true,`r`n        `$false,`r`n        [ref]`$resolvedPassword,")
    if ($content -eq $beforeOldMode) {
        $content = $content.Replace(
            "        `$true,`n        [ref]`$resolvedPassword,",
            "        `$true,`n        `$false,`n        [ref]`$resolvedPassword,")
    }
    if ($content -eq $beforeOldMode) {
        throw "Unable to update legacy test invocations."
    }

    $anchor = @'
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

'@

    $additionalTests = @'
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

'@

    Replace-ExactOnce $anchor ($anchor + $additionalTests) "cover explicit credential mode boundaries"
}

Write-Host "Explicit login credential mode applied successfully."
