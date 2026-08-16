param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$bridgePath = Join-Path $root "Data/NosGm.Program/NosGm.World/DiscordGmBridge.cs"
if (-not (Test-Path -LiteralPath $bridgePath -PathType Leaf)) {
    throw "Discord GM bridge source was not found."
}

$source = Get-Content -LiteralPath $bridgePath -Raw

function Assert-Contains([string]$expected, [string]$name) {
    if (-not $source.Contains($expected)) {
        throw "$name failed. Missing security contract: $expected"
    }

    Write-Host "[PASS] $name"
}

function Assert-NotContains([string]$unexpected, [string]$name) {
    if ($source.Contains($unexpected)) {
        throw "$name failed. Forbidden bridge capability remains: $unexpected"
    }

    Write-Host "[PASS] $name"
}

Assert-Contains 'private const int MinimumSecretLength = 48;' `
    "Bridge requires a stronger HMAC secret"
Assert-Contains 'NOSGM_GM_BRIDGE_HELPER_IDS' `
    "Helper Discord IDs are configured server-side"
Assert-Contains 'NOSGM_GM_BRIDGE_MODERATOR_IDS' `
    "Moderator Discord IDs are configured server-side"
Assert-Contains 'NOSGM_GM_BRIDGE_ADMIN_IDS' `
    "Admin Discord IDs are configured server-side"
Assert-Contains 'NOSGM_GM_BRIDGE_OWNER_IDS' `
    "Owner Discord IDs are configured server-side"
Assert-Contains 'if (authorizedActors.Count == 0)' `
    "Bridge fails closed when no actors are allowlisted"
Assert-Contains 'ValidateLocalPrefix(prefix);' `
    "Bridge validates its listener prefix before binding"
Assert-Contains '!string.Equals(uri.Host, "127.0.0.1"' `
    "Bridge restricts the listener to loopback"
Assert-Contains 'var role = Authorize(request);' `
    "Every command passes independent actor authorization"
Assert-Contains 'throw new BridgeException(403, "Discord actor is not allowlisted on this World Server.");' `
    "Unknown Discord actors are denied"
Assert-Contains 'throw new BridgeException(403, "Discord actor is not authorized for this command.");' `
    "Role escalation is denied"
Assert-Contains 'return BridgeRole.None;' `
    "Unknown commands fail closed"
Assert-Contains 'case "ban":' `
    "Ban remains explicitly classified"
Assert-Contains 'return BridgeRole.Admin;' `
    "Administrative commands require admin authority"
Assert-Contains 'case "shutdown":' `
    "Shutdown remains explicitly classified"
Assert-Contains 'return BridgeRole.Owner;' `
    "Owner-only commands are isolated"
Assert-Contains 'Remote shutdown is disabled' `
    "Remote shutdown remains disabled"
Assert-Contains 'Item delivery is disabled' `
    "Remote item delivery remains disabled"
Assert-Contains 'NOSGM_GM_BRIDGE_PREVIOUS_SECRET' `
    "Controlled HMAC key rotation is supported"
Assert-Contains 'previousSecretExpiresUnix - now > 3600' `
    "Previous HMAC key validity is bounded to one hour"
Assert-Contains 'supplied.Length != 64' `
    "HMAC signatures require the exact SHA-256 hex length"
Assert-Contains 'nonce.Length < 16' `
    "Replay nonces have a minimum length"
Assert-Contains 'return "Discord:" + request.actor.discordUserId;' `
    "Penalty attribution uses the allowlisted immutable Discord ID"
Assert-Contains '{ "authorizedRole", role.ToString() }' `
    "Audit records include the resolved server-side role"

$deserializeIndex = $source.IndexOf('request = _json.Deserialize<CommandRequest>(body);', [StringComparison]::Ordinal)
$authorizeIndex = $source.IndexOf('var role = Authorize(request);', [StringComparison]::Ordinal)
$executeIndex = $source.IndexOf('var result = Execute(request);', [StringComparison]::Ordinal)
if ($deserializeIndex -lt 0 -or $authorizeIndex -le $deserializeIndex -or $executeIndex -le $authorizeIndex) {
    throw "Authorization ordering failed. The bridge must deserialize, validate/authorize, and only then execute."
}
Write-Host "[PASS] Authorization happens before command execution"

$requiredRoles = [ordered]@{
    'case "status":' = 'Helper'
    'case "kick":' = 'Moderator'
    'case "inventory":' = 'Admin'
    'case "ban":' = 'Admin'
    'case "shutdown":' = 'Owner'
}

foreach ($entry in $requiredRoles.GetEnumerator()) {
    $caseIndex = $source.IndexOf($entry.Key, $source.IndexOf('private static BridgeRole RequiredRoleFor', [StringComparison]::Ordinal), [StringComparison]::Ordinal)
    $roleIndex = $source.IndexOf('return BridgeRole.' + $entry.Value + ';', $caseIndex, [StringComparison]::Ordinal)
    if ($caseIndex -lt 0 -or $roleIndex -lt $caseIndex) {
        throw "Role classification failed for $($entry.Key): expected $($entry.Value)."
    }
}
Write-Host "[PASS] Representative commands preserve least-privilege role tiers"

Assert-NotContains 'Process.Start("powershell' `
    "Bridge does not spawn PowerShell"
Assert-NotContains 'Process.Start("cmd' `
    "Bridge does not spawn cmd.exe"
Assert-NotContains 'Invoke-Expression' `
    "Bridge contains no PowerShell expression execution"
Assert-NotContains 'DownloadString(' `
    "Bridge contains no download-and-execute primitive"

Write-Host "NosGM Discord GM bridge authorization and secret-rotation contracts passed."
