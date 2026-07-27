$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$path = "scripts/verify-world-channel-lists.ps1"
$content = Get-Content -LiteralPath $path -Raw
$newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }

$old = 'Assert-Regex $loginHandlerSource ''BuildServersPacket\s*\(\s*username\s*,\s*loginPacket\.RegionType\s*,\s*newSessionId'' "Login must pass RegionType unchanged to Master"'
$newLines = @(
    'Assert-Regex $loginHandlerSource ''CompleteLoginAsync\s*\(\s*loadedAccount\s*,\s*username\s*,\s*loginPacket\.RegionType\s*,\s*null\s*,\s*ignoreUserName\s*,\s*"password"\s*\)'' "NoS0575 must pass its RegionType unchanged into shared Login completion"',
    'Assert-Regex $loginHandlerSource ''CompleteLoginAsync\s*\(\s*loadedAccount\s*,\s*username\s*,\s*payload\.CountryId\s*,\s*culture\s*,\s*false\s*,\s*payload\.Header\s*\)'' "NoS0576 and NoS0577 must pass authenticated CountryId into shared Login completion"',
    'Assert-Regex $loginHandlerSource ''BuildServersPacket\s*\(\s*username\s*,\s*regionType\s*,\s*newSessionId\s*,\s*ignoreUserName\s*,\s*loadedAccount\.AccountId\s*\)'' "Shared Login completion must pass the authenticated region unchanged to Master"'
)
$new = [string]::Join($newLine, $newLines)

$first = $content.IndexOf($old, [StringComparison]::Ordinal)
if ($first -lt 0) {
    throw "Expected legacy RegionType assertion was not found."
}

$second = $content.IndexOf($old, $first + $old.Length, [StringComparison]::Ordinal)
if ($second -ge 0) {
    throw "Expected exactly one legacy RegionType assertion."
}

$content = $content.Substring(0, $first) + $new + $content.Substring($first + $old.Length)
[IO.File]::WriteAllText(
    (Resolve-Path -LiteralPath $path),
    $content,
    (New-Object Text.UTF8Encoding($true)))

Write-Host "Shared legacy and modern RegionType contracts applied."
