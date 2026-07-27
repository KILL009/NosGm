param(
    [string]$LoginHandlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$EntryHandlerPath = "Data/NosGm.Handler/PacketHandler/CharScreen/EntryPointPacketHandler.cs",
    [string]$AuthServicePath = "Data/NosGm.Program/NosGm.Master.Server/AuthentificationService.cs",
    [string]$AuthInterfacePath = "Data/NosGm.Master.Library/Interface/IAuthentificationService.cs",
    [string]$AuthClientPath = "Data/NosGm.Master.Library/Client/AuthentificationServiceClient.cs",
    [string]$PacketAttributePath = "Data/NosGm.Core/Handling/PacketAttribute.cs",
    [string]$HandlerReferencePath = "Data/NosGm.Core/Handling/HandlerMethodReference.cs",
    [string]$LegacyPacketPath = "Data/NosGm.Packets/Packets/ClientPackets/LoginPacket.cs"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Source {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required modern-login source file not found: $Path"
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Assert-Contains {
    param(
        [string]$Content,
        [string]$Needle,
        [string]$Description
    )

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Modern-login contract failed: $Description"
    }
}

function Assert-NotContains {
    param(
        [string]$Content,
        [string]$Needle,
        [string]$Description
    )

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "Modern-login contract failed: $Description"
    }
}

function Assert-Ordered {
    param(
        [string]$Content,
        [string[]]$Needles,
        [string]$Description
    )

    $position = 0
    foreach ($needle in $Needles) {
        $next = $Content.IndexOf($needle, $position, [StringComparison]::Ordinal)
        if ($next -lt 0) {
            throw "Modern-login contract failed: $Description. Missing or out-of-order token: $needle"
        }

        $position = $next + $needle.Length
    }
}

function Test-Hex {
    param(
        [string]$Value,
        [int]$MinimumLength,
        [int]$MaximumLength
    )

    if ([string]::IsNullOrEmpty($Value) -or
        $Value.Length -lt $MinimumLength -or
        $Value.Length -gt $MaximumLength) {
        return $false
    }

    return $Value -match '^[0-9a-fA-F]+$'
}

function Test-ModernPacket {
    param([string]$RawPacket)

    if ([string]::IsNullOrEmpty($RawPacket) -or $RawPacket.Length -gt 4096) {
        return $false
    }

    $headerEnd = $RawPacket.IndexOf(' ')
    if ($headerEnd -le 0) {
        return $false
    }

    $header = $RawPacket.Substring(0, $headerEnd)
    if ($header -ne 'NoS0576' -and $header -ne 'NoS0577') {
        return $false
    }

    $tokenStart = $headerEnd + 1
    $doubleSpace = $RawPacket.IndexOf('  ', $tokenStart, [StringComparison]::Ordinal)
    if ($doubleSpace -le $tokenStart -or $doubleSpace + 2 -ge $RawPacket.Length) {
        return $false
    }

    if ($RawPacket[$doubleSpace + 2] -eq ' ') {
        return $false
    }

    $token = $RawPacket.Substring($tokenStart, $doubleSpace - $tokenStart)
    if ($token.Length -lt 16 -or $token.Length -gt 1024 -or $token -match '\s') {
        return $false
    }

    $tail = $RawPacket.Substring($doubleSpace + 2)
    if ($tail.StartsWith(' ') -or $tail.EndsWith(' ') -or $tail.Contains('  ')) {
        return $false
    }

    $fields = $tail.Split([char]' ')
    if ($fields.Length -ne 5) {
        return $false
    }

    $installationId = [Guid]::Empty
    if (-not [Guid]::TryParse($fields[0], [ref]$installationId) -or
        -not (Test-Hex $fields[1] 1 32) -or
        $fields[3] -ne '0' -or
        -not (Test-Hex $fields[4] 32 32)) {
        return $false
    }

    $verticalTab = [char]0x0B
    $separatorIndex = $fields[2].IndexOf($verticalTab)
    if ($separatorIndex -le 0 -or
        $separatorIndex -ne $fields[2].LastIndexOf($verticalTab) -or
        $separatorIndex + 1 -ge $fields[2].Length) {
        return $false
    }

    $region = 0
    $version = $null
    return [byte]::TryParse($fields[2].Substring(0, $separatorIndex), [ref]$region) -and
        [Version]::TryParse($fields[2].Substring($separatorIndex + 1), [ref]$version)
}

$login = Read-Source $LoginHandlerPath
$entry = Read-Source $EntryHandlerPath
$authService = Read-Source $AuthServicePath
$authInterface = Read-Source $AuthInterfacePath
$authClient = Read-Source $AuthClientPath
$packetAttribute = Read-Source $PacketAttributePath
$handlerReference = Read-Source $HandlerReferencePath
$legacyPacket = Read-Source $LegacyPacketPath

Assert-Contains $login '[Packet("NoS0576", "NoS0577")]' 'Login must bind both observed modern packet headers to one raw handler'
Assert-Contains $login 'rawPacket.IndexOf("  ", tokenStart, StringComparison.Ordinal)' 'Parser must preserve the mandatory double-space separator'
Assert-Contains $login 'int separatorIndex = fields[2].IndexOf' 'Parser must require a dedicated region/version separator'
Assert-Contains $login 'AuthentificationServiceClient.Instance.ConsumeModernLoginTicket(' 'Login must resolve the one-time ticket through Master'
Assert-Contains $login 'RegisterModernLoginSession(' 'Login must register a separate modern World-entry permit'
Assert-Contains $login 'BuildServersPacket(' 'Modern login must reuse the canonical NsTeST serializer'
Assert-NotContains $login 'Logger.Info(rawPacket' 'Credential-bearing raw modern packets must never be logged'
Assert-NotContains $login '$"{rawPacket}' 'Credential-bearing raw modern packets must never be interpolated into logs'

Assert-Contains $legacyPacket '[PacketHeader("NoS0575", IsCharScreen = true)]' 'Legacy NoS0575 support must remain intact'
Assert-Contains $packetAttribute '"NoS0576"' 'Raw modern login handlers must be marked usable before character selection'
Assert-Contains $packetAttribute '"NoS0577"' 'Both modern login headers must receive the character-screen flag'
Assert-Ordered $handlerReference @(
    'HandlerMethodAttribute = handlerMethodAttribute;',
    'IsCharScreen = HandlerMethodAttribute.IsCharScreen;',
    'Amount = HandlerMethodAttribute.Amount;'
) 'Raw packet metadata must propagate into the dispatcher'

Assert-Contains $authInterface 'StoreModernLoginTicket(' 'The trusted AuthServer must have a ticket-ingress contract'
Assert-Contains $authInterface 'ConsumeModernLoginTicket(' 'Login must have a one-time ticket-consumption contract'
Assert-Contains $authInterface 'RegisterModernLoginSession(' 'Login must register the World-entry permit'
Assert-Contains $authInterface 'ConsumeModernLoginSession(' 'World must consume the World-entry permit'
Assert-Contains $authService 'TimeSpan.FromMinutes(2)' 'Tickets and World permits must be short-lived'
Assert-Contains $authService 'LoginTickets.Remove(code);' 'Authentication tickets must be one-time'
Assert-Contains $authService 'WorldPermits.Remove(key);' 'World-entry permits must be one-time'
Assert-NotContains $authService 'Logger.Info(authToken' 'Authentication tokens must never be logged by Master'
Assert-Contains $authClient 'StoreModernLoginTicket(' 'The AuthServer client proxy must expose ticket storage'
Assert-Contains $authClient 'ConsumeModernLoginSession(' 'The World client proxy must expose permit consumption'

Assert-Ordered $entry @(
    'CommunicationServiceClient.Instance.IsLoginPermitted(',
    'string.Equals(loginPacketParts[7], "thisisgfmode", StringComparison.Ordinal);',
    'AuthentificationServiceClient.Instance.ConsumeModernLoginSession(',
    'Session.InitializeAccount(new Account(account), isCrossServerLogin);'
) 'World must validate Master registration and consume the modern permit before account initialization'

$authCode = [Guid]::NewGuid().ToString('D')
$token = -join ([Text.Encoding]::ASCII.GetBytes($authCode) | ForEach-Object { $_.ToString('x2') })
$installationId = [Guid]::NewGuid().ToString('D')
$verticalTab = [char]0x0B
$md5 = 'A' * 32
$valid0576 = "NoS0576 $token  $installationId 003662BF 5$verticalTab" + '0.9.3.3255' + " 0 $md5"
$valid0577 = $valid0576.Replace('NoS0576', 'NoS0577')

if (-not (Test-ModernPacket $valid0576)) {
    throw 'Modern-login fixture failed: valid NoS0576 packet was rejected.'
}
if (-not (Test-ModernPacket $valid0577)) {
    throw 'Modern-login fixture failed: valid NoS0577 packet was rejected.'
}
if (Test-ModernPacket $valid0576.Replace("$token  $installationId", "$token $installationId")) {
    throw 'Modern-login fixture failed: single-space token separator was accepted.'
}
if (Test-ModernPacket $valid0576.Replace("$token  $installationId", "$token   $installationId")) {
    throw 'Modern-login fixture failed: triple-space token separator was accepted.'
}
if (Test-ModernPacket $valid0576.Replace($verticalTab, ' ')) {
    throw 'Modern-login fixture failed: missing ASCII 0x0B separator was accepted.'
}
if (Test-ModernPacket $valid0576.Substring(0, $valid0576.Length - 1)) {
    throw 'Modern-login fixture failed: invalid MD5 length was accepted.'
}

Write-Host 'NoS0576/NoS0577 parser, ticket broker and World-entry contracts verified.'
