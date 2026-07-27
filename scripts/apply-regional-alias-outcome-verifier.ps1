$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$path = "scripts/verify-login-outcomes.ps1"
$content = Get-Content -LiteralPath $path -Raw
$newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }

function Replace-ExactOnce {
    param(
        [string]$Old,
        [string]$New,
        [string]$Description
    )

    $oldValue = [regex]::Replace($Old, "`r`n|`n|`r", $script:newLine)
    $newValue = [regex]::Replace($New, "`r`n|`n|`r", $script:newLine)
    $first = $script:content.IndexOf($oldValue, [StringComparison]::Ordinal)
    if ($first -lt 0) {
        throw "Expected source was not found: $Description"
    }
    $second = $script:content.IndexOf($oldValue, $first + $oldValue.Length, [StringComparison]::Ordinal)
    if ($second -ge 0) {
        throw "Expected exactly one source match: $Description"
    }

    $script:content = $script:content.Substring(0, $first) +
        $newValue +
        $script:content.Substring($first + $oldValue.Length)
    Write-Host "Applied: $Description"
}

if ($content.Contains('AccountDTO loadedAccount = DAOFactory.AccountDAO.LoadByName(username);')) {
    Replace-ExactOnce @'
    "AccountDTO loadedAccount = DAOFactory.AccountDAO.LoadByName(username);",
    "if (loadedAccount == null)",
    "if (!string.Equals(loadedAccount.Name, username, StringComparison.Ordinal))",
'@ @'
    "AccountDTO loadedAccount = LoadAccountByLoginName(username, resolvedRegionType);",
    "if (loadedAccount == null)",
    "bool accountNameMatches = string.Equals(loadedAccount.Name, username, StringComparison.Ordinal) ||",
'@ "update deterministic Login order for regional aliases"

    Replace-ExactOnce @'
Assert-Regex $handler 'loadedAccount == null.*?Reject\(LoginFailType\.AccountOrPasswordWrong, "Session removed\. Reason: Unknown account"\)' "unknown account mapping"
Assert-Regex $handler '!string\.Equals\(loadedAccount\.Name, username, StringComparison\.Ordinal\).*?Reject\(LoginFailType\.WrongCaps' "account casing mapping"
'@ @'
Assert-Regex $handler 'LoadAccountByLoginName\(username, resolvedRegionType\).*?loadedAccount == null.*?Reject\(LoginFailType\.AccountOrPasswordWrong, "Session removed\. Reason: Unknown account"\)' "unknown account and optional regional alias mapping"
Assert-Regex $handler '!accountNameMatches.*?Reject\(LoginFailType\.WrongCaps' "exact account or trusted regional alias casing mapping"
Assert-Regex $handler 'private static AccountDTO LoadAccountByLoginName.*?AccountDAO\.LoadByName\(username\).*?TryStripProtocolPrefix.*?profile\.RegionType != resolvedRegionType.*?AccountDAO\.LoadByName\(accountName\)' "regional alias resolution must prefer exact accounts and require the trusted Login region"
'@ "verify exact-first regional alias resolution"

    [IO.File]::WriteAllText(
        (Resolve-Path -LiteralPath $path),
        $content,
        (New-Object Text.UTF8Encoding($true)))
}
else {
    Write-Host "Login outcome verifier already reflects regional account aliases."
}
