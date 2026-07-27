$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$path = "scripts/verify-world-channel-lists.ps1"
$content = Get-Content -LiteralPath $path -Raw
$newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }

function Replace-ExactOnce {
    param(
        [string]$Old,
        [string]$New,
        [string]$Description
    )

    $oldValue = [regex]::Replace($Old, "`r`n|`n|`r", $newLine)
    $newValue = [regex]::Replace($New, "`r`n|`n|`r", $newLine)
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

if ($content.Contains('$expectedCultures = @("en", "es", "de", "fr", "it", "pl", "cs", "ru", "ja", "zh")')) {
    Replace-ExactOnce @'
$expectedCultures = @("en", "es", "de", "fr", "it", "pl", "cs", "ru", "ja", "zh")
'@ @'
$expectedCultures = @("en", "es", "de", "fr", "it", "pl", "cs", "ru", "tr", "ja", "zh")
'@ "add Turkish server culture fallback"

    Replace-ExactOnce @'
$expectedRegions = @(0, 1, 2, 3, 4, 5, 6, 7, 8, 9)
'@ @'
$expectedRegions = @(0, 1, 2, 3, 4, 5, 6, 7, 8)
'@ "limit the verified EU client to RegionType 0 through 8"

    Replace-ExactOnce @'
foreach ($culture in $expectedCultures | Where-Object { $_ -ne "en" }) {
'@ @'
foreach ($culture in $expectedCultures | Where-Object { $_ -ne "en" -and $_ -ne "tr" }) {
'@ "allow Turkish server messages to use the neutral fallback until translated"

    Replace-ExactOnce @'
Write-Host "Verified $($seenCaseIds.Count) world/channel fixtures, 10 protocol region bytes and 10 independent server cultures."
'@ @'
Write-Host "Verified $($seenCaseIds.Count) world/channel fixtures, 9 EU client region bytes and 11 independent server cultures (Turkish uses neutral fallback)."
'@ "update the world and language verification summary"

    [IO.File]::WriteAllText(
        (Resolve-Path -LiteralPath $path),
        $content,
        (New-Object Text.UTF8Encoding($true)))
}
else {
    Write-Host "World/channel verifier already reflects the verified EU client evidence."
}
