$ErrorActionPreference = "Stop"

$path = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs"
$content = Get-Content -LiteralPath $path -Raw
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
        throw "Expected source not found: $Description"
    }

    $second = $script:content.IndexOf($oldNormalized, $first + $oldNormalized.Length, [StringComparison]::Ordinal)
    if ($second -ge 0) {
        throw "Expected exactly one source match: $Description"
    }

    $script:content = $script:content.Substring(0, $first) +
        $newNormalized +
        $script:content.Substring($first + $oldNormalized.Length)
    Write-Host "Applied: $Description"
}

Replace-ExactOnce @'
            if (!TryGetClearPassword(loginPacket.Password, out string clearPassword))
            {
                Reject(LoginFailType.AccountOrPasswordWrong, "Session removed. Reason: Invalid password payload");
                return;
            }

'@ '' "defer password payload interpretation until the account is loaded"

Replace-ExactOnce @'
            if (!PasswordHashService.VerifyPassword(
                    loadedAccount.Password,
                    clearPassword,
                    ServerConfiguration.UseOldCrypto,
                    out bool passwordNeedsUpgrade))
'@ @'
            if (!PasswordHashService.VerifyLoginPayload(
                    loadedAccount.Password,
                    loginPacket.Password,
                    ServerConfiguration.UseOldCrypto,
                    out string clearPassword,
                    out bool passwordNeedsUpgrade))
'@ "verify both legacy-encoded and plain password payloads"

Replace-ExactOnce @'
        private static bool TryGetClearPassword(string packetPassword, out string clearPassword)
        {
            clearPassword = null;
            if (string.IsNullOrWhiteSpace(packetPassword))
            {
                return false;
            }

            try
            {
                clearPassword = ServerConfiguration.UseOldCrypto
                    ? LoginCryptography.GetPassword(packetPassword)
                    : packetPassword;
            }
            catch (Exception)
            {
                return false;
            }

            return clearPassword != null &&
                   clearPassword.Length <= PasswordHashService.MaximumCredentialLength;
        }

'@ '' "remove the single-format password decoder"

[IO.File]::WriteAllText(
    (Resolve-Path -LiteralPath $path),
    $content,
    (New-Object Text.UTF8Encoding($true)))

Write-Host "Login password payload fallback applied successfully."
