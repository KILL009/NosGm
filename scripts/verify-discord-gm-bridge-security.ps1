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

function Get-HmacHex([string]$secret, [string]$text) {
    $hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($secret))
    try {
        $hash = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))
        return -join ($hash | ForEach-Object { $_.ToString("x2") })
    }
    finally {
        $hmac.Dispose()
    }
}

Assert-Contains 'private const int MinimumSecretLength = 48;' `
    "Bridge requires strong HMAC secrets"
Assert-Contains 'private const string ActorSignatureDomain = "nosgm-actor-v1";' `
    "Actor signatures are domain separated"
Assert-Contains 'ReadRequiredSecret("NOSGM_GM_BRIDGE_SECRET")' `
    "Gateway HMAC secret is mandatory"
Assert-Contains 'ReadRequiredSecret("NOSGM_GM_BRIDGE_IDENTITY_SECRET")' `
    "Independent actor-identity HMAC secret is mandatory"
Assert-Contains 'NOSGM_GM_BRIDGE_SECRET and NOSGM_GM_BRIDGE_IDENTITY_SECRET must be different secrets.' `
    "Gateway and actor secrets cannot be reused"
Assert-Contains 'NOSGM_GM_BRIDGE_PREVIOUS_IDENTITY_SECRET' `
    "Actor identity key participates in bounded rotation"
Assert-Contains 'previousSecretConfigured != previousIdentityConfigured' `
    "Previous gateway and actor keys rotate as a pair"
Assert-Contains 'previousSecretExpiresUnix - now > 3600' `
    "Previous key generation is bounded to one hour"
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
Assert-Contains 'request.Headers["X-NosGM-Signature"]' `
    "Gateway signature header is mandatory"
Assert-Contains 'request.Headers["X-NosGM-Actor-Signature"]' `
    "Independent actor signature header is mandatory"
Assert-Contains 'command.actor.discordUserId + "\n" +' `
    "Actor signature cryptographically binds the Discord user ID"
Assert-Contains 'authentication.UsesPreviousSecret ? _previousIdentitySecret : _identitySecret' `
    "Actor signature uses the same key generation as the gateway signature"
Assert-Contains 'ConsumeNonce(authentication.Nonce, authentication.Now);' `
    "Replay nonce is consumed only after dual authentication"
Assert-Contains 'var role = Authorize(request);' `
    "Every authenticated command passes server-side actor authorization"
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
Assert-Contains 'value.Length == 64 && value.All(IsHexCharacter)' `
    "Both HMAC signatures require exact SHA-256 hex encoding"
Assert-Contains 'nonce.Length < 16' `
    "Replay nonces have a minimum length"
Assert-Contains 'return "Discord:" + request.actor.discordUserId;' `
    "Penalty attribution uses the allowlisted immutable Discord ID"
Assert-Contains '{ "authorizedRole", role.ToString() }' `
    "Audit records include the resolved server-side role"
Assert-Contains 'var trustedRequest = fullyAuthenticated ? request : null;' `
    "Unauthenticated request contents are not trusted for audit output"

$gatewayIndex = $source.IndexOf('var authentication = AuthenticateGateway(context.Request, body);', [StringComparison]::Ordinal)
$deserializeIndex = $source.IndexOf('request = _json.Deserialize<CommandRequest>(body);', [StringComparison]::Ordinal)
$actorIndex = $source.IndexOf('AuthenticateActor(context.Request, authentication, request, body);', [StringComparison]::Ordinal)
$nonceIndex = $source.IndexOf('ConsumeNonce(authentication.Nonce, authentication.Now);', [StringComparison]::Ordinal)
$authorizeIndex = $source.IndexOf('var role = Authorize(request);', [StringComparison]::Ordinal)
$executeIndex = $source.IndexOf('var result = Execute(request);', [StringComparison]::Ordinal)
if ($gatewayIndex -lt 0 -or
    $deserializeIndex -le $gatewayIndex -or
    $actorIndex -le $deserializeIndex -or
    $nonceIndex -le $actorIndex -or
    $authorizeIndex -le $nonceIndex -or
    $executeIndex -le $authorizeIndex) {
    throw "Authentication ordering failed. Gateway HMAC, actor HMAC, replay barrier and authorization must all complete before command execution."
}
Write-Host "[PASS] Dual authentication and authorization happen before command execution"

$requiredRoles = [ordered]@{
    'case "status":' = 'Helper'
    'case "kick":' = 'Moderator'
    'case "inventory":' = 'Admin'
    'case "ban":' = 'Admin'
    'case "shutdown":' = 'Owner'
}

$roleSection = $source.IndexOf('private static BridgeRole RequiredRoleFor', [StringComparison]::Ordinal)
foreach ($entry in $requiredRoles.GetEnumerator()) {
    $caseIndex = $source.IndexOf($entry.Key, $roleSection, [StringComparison]::Ordinal)
    $roleIndex = $source.IndexOf('return BridgeRole.' + $entry.Value + ';', $caseIndex, [StringComparison]::Ordinal)
    if ($caseIndex -lt 0 -or $roleIndex -lt $caseIndex) {
        throw "Role classification failed for $($entry.Key): expected $($entry.Value)."
    }
}
Write-Host "[PASS] Representative commands preserve least-privilege role tiers"

# Cryptographic model regression: the gateway signature authenticates the request
# transport, while the second key cryptographically binds the Discord actor ID.
$gatewaySecret = 'gateway-secret-0123456789-ABCDEFGHIJKLMNOPQRSTUVWXYZ'
$identitySecret = 'identity-secret-9876543210-ZYXWVUTSRQPONMLKJIHGFEDCBA'
$timestamp = '1786924800'
$nonce = 'sample_nonce_0123456789ABCDEF'
$actorId = '123456789012345678'
$body = '{"requestId":"security-test","actor":{"discordUserId":"123456789012345678"},"command":"status","arguments":{}}'
$gatewayCanonical = $timestamp + "`n" + $nonce + "`n" + $body
$actorCanonical = 'nosgm-actor-v1' + "`n" + $timestamp + "`n" + $nonce + "`n" + $actorId + "`n" + $body
$gatewaySignature = Get-HmacHex $gatewaySecret $gatewayCanonical
$actorSignature = Get-HmacHex $identitySecret $actorCanonical

if ($gatewaySignature.Length -ne 64 -or $actorSignature.Length -ne 64 -or $gatewaySignature -eq $actorSignature) {
    throw "Dual-signature model did not produce independent SHA-256 HMAC values."
}

$spoofedActorCanonical = 'nosgm-actor-v1' + "`n" + $timestamp + "`n" + $nonce + "`n" + '999999999999999999' + "`n" + $body
if ((Get-HmacHex $identitySecret $spoofedActorCanonical) -eq $actorSignature) {
    throw "Actor identity is not cryptographically bound to the second signature."
}

if ((Get-HmacHex $gatewaySecret $actorCanonical) -eq $actorSignature) {
    throw "Gateway and actor signature trust anchors are not independent in the regression model."
}
Write-Host "[PASS] A valid actor signature cannot be reused for a different Discord user ID or gateway key"

Assert-NotContains 'Process.Start("powershell' `
    "Bridge does not spawn PowerShell"
Assert-NotContains 'Process.Start("cmd' `
    "Bridge does not spawn cmd.exe"
Assert-NotContains 'Invoke-Expression' `
    "Bridge contains no PowerShell expression execution"
Assert-NotContains 'DownloadString(' `
    "Bridge contains no download-and-execute primitive"

Write-Host "NosGM Discord GM bridge dual-signature authorization and secret-rotation contracts passed."
