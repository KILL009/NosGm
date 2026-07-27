param(
    [string]$LoginHandlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$EntryHandlerPath = "Data/NosGm.Handler/PacketHandler/CharScreen/EntryPointPacketHandler.cs",
    [string]$LoginProgramPath = "Data/NosGm.Program/NosGm.Login/Program.cs",
    [string]$AuthServicePath = "Data/NosGm.Program/NosGm.Master.Server/AuthentificationService.cs",
    [string]$AuthInterfacePath = "Data/NosGm.Master.Library/Interface/IAuthentificationService.cs",
    [string]$AuthClientPath = "Data/NosGm.Master.Library/Client/AuthentificationServiceClient.cs",
    [string]$PacketParserPath = "Data/NosGm.Master.Library/Security/GameforgeLoginPacketParser.cs",
    [string]$TicketStorePath = "Data/NosGm.Master.Library/Security/GameforgeAuthTicketStore.cs",
    [string]$PermitStorePath = "Data/NosGm.Master.Library/Security/GameforgeWorldPermitStore.cs",
    [string]$ProjectPath = "Data/NosGm.Master.Library/NosGm.Master.Library.csproj",
    [string]$ConfigurationPath = "Data/NosGm.Configuration/ServerConfiguration.cs"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Source {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required Gameforge login file not found: $Path"
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Assert-Contains {
    param([string]$Content, [string]$Needle, [string]$Description)

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Gameforge login contract failed: $Description"
    }
}

function Assert-NotContains {
    param([string]$Content, [string]$Needle, [string]$Description)

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "Gameforge login contract failed: $Description"
    }
}

function Assert-Regex {
    param([string]$Content, [string]$Pattern, [string]$Description)

    if (-not [regex]::IsMatch(
            $Content,
            $Pattern,
            [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "Gameforge login contract failed: $Description"
    }
}

function Assert-NotRegex {
    param([string]$Content, [string]$Pattern, [string]$Description)

    if ([regex]::IsMatch(
            $Content,
            $Pattern,
            [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "Gameforge login contract failed: $Description"
    }
}

function Assert-Count {
    param([string]$Content, [string]$Needle, [int]$Expected, [string]$Description)

    $count = ([regex]::Matches($Content, [regex]::Escape($Needle))).Count
    if ($count -ne $Expected) {
        throw "Gameforge login contract failed: $Description. Expected $Expected, found $count."
    }
}

function Assert-Ordered {
    param([string]$Content, [string[]]$Needles, [string]$Description)

    $position = 0
    foreach ($needle in $Needles) {
        $next = $Content.IndexOf($needle, $position, [StringComparison]::Ordinal)
        if ($next -lt 0) {
            throw "Gameforge login contract failed: $Description. Missing or out-of-order token: $needle"
        }

        $position = $next + $needle.Length
    }
}

function Assert-RegionBalance {
    param([string]$Content, [string]$Description)

    $starts = ([regex]::Matches($Content, '(?m)^\s*#region\b')).Count
    $ends = ([regex]::Matches($Content, '(?m)^\s*#endregion\b')).Count
    if ($starts -ne $ends) {
        throw "Gameforge login contract failed: $Description has unbalanced regions ($starts starts, $ends ends)."
    }
}

$login = Read-Source $LoginHandlerPath
$entry = Read-Source $EntryHandlerPath
$program = Read-Source $LoginProgramPath
$service = Read-Source $AuthServicePath
$interface = Read-Source $AuthInterfacePath
$client = Read-Source $AuthClientPath
$parser = Read-Source $PacketParserPath
$tickets = Read-Source $TicketStorePath
$permits = Read-Source $PermitStorePath
$project = Read-Source $ProjectPath
$configuration = Read-Source $ConfigurationPath

Assert-Count $login '[Packet("NoS0576", "NoS0577")]' 1 'Exactly one raw modern login handler must own both packet headers'
Assert-NotContains $login 'VerifyModernLoginAsync' 'The superseded second modern-login implementation must not return'
Assert-NotContains $login 'StoreModernLoginTicket' 'The superseded generic modern ticket broker must not return'
Assert-NotContains $login 'ConsumeModernLoginTicket' 'The superseded generic modern ticket consumer must not return'
Assert-NotContains $login 'RegisterModernLoginSession' 'The superseded generic World-permit API must not return'
Assert-Contains $login 'GameforgeLoginPacketParser.TryParse(' 'Login must use the single strict Gameforge parser'
Assert-Contains $login 'payload.CountryId != resolvedRegionType' 'The packet country must match the trusted regional Login port'
Assert-Ordered $login @(
    'ConsumeGameforgeAuthTicket(',
    'RegisterAccountLogin(',
    'RegisterGameforgeWorldPermit(',
    'string serversPacket = BuildServersPacket(',
    '_session.SendPacket(serversPacket);'
) 'The one-time ticket and World permit must precede server-list delivery'
Assert-NotContains $login 'Logger.Info(rawPacket' 'Raw token-bearing packets must never be logged'
Assert-NotContains $login '$"{rawPacket}' 'Raw token-bearing packets must never be interpolated into logs'

Assert-Ordered $entry @(
    'IsLoginPermitted(',
    'string.Equals(loginPacketParts[7], "thisisgfmode", StringComparison.Ordinal);',
    'ConsumeGameforgeWorldPermit(',
    'Session.InitializeAccount(new Account(account), isCrossServerLogin);'
) 'World must verify the normal session and consume the one-use permit before account initialization'
Assert-Contains $entry 'isGameforgePasswordlessLogin ||' 'Password bypass must be limited to a validated Gameforge permit'

Assert-Count $program 'CommunicationServiceClient.Instance.Authenticate(' 1 'Login must authenticate the Master communication service once'
Assert-Count $program 'AntiSpamModule.Instance.RunBlacklistTask();' 1 'Login must start the blacklist task once'
Assert-NotContains $program 'var networkManager = new NetworkManager' 'No discarded duplicate listener may be created'
Assert-Regex $program 'AuthentificationServiceClient\.Instance\.Authenticate\s*\(\s*ServerConfiguration\.GameforgeTicketConsumerKey\s*\)' 'Login must authenticate as the Gameforge ticket consumer'
Assert-NotRegex $program 'AuthentificationServiceClient\.Instance\.Authenticate\s*\(\s*ServerConfiguration\.GameforgeTicketIssuerKey\s*\)' 'Login must never authenticate as the ticket issuer'
Assert-Contains $program 'Enumerable.Range(' 'Regional listeners 4000-4009 must remain supported'

Assert-Contains $service 'IsGameforgeIssuerClient()' 'Only the bridge role may issue authentication tickets'
Assert-Contains $service 'IsGameforgeConsumerClient()' 'Only Login may consume tickets and create permits'
Assert-Contains $service 'IsLegacyAuthClient()' 'World must authenticate through the legacy service role'
Assert-Contains $service 'GameforgeAuthTicketStore.Instance.TryIssue(' 'Master must store authentication tickets centrally'
Assert-Contains $service 'GameforgeAuthTicketStore.Instance.TryConsume(' 'Master must consume authentication tickets atomically'
Assert-Contains $service 'GameforgeWorldPermitStore.Instance.TryIssue(' 'Master must issue a separate World permit'
Assert-Contains $service 'GameforgeWorldPermitStore.Instance.TryConsume(' 'Master must consume the World permit once'

Assert-Contains $interface 'RegisterGameforgeAuthTicket(' 'The issuer contract must exist'
Assert-Contains $interface 'ConsumeGameforgeAuthTicket(' 'The Login ticket-consumption contract must exist'
Assert-Contains $interface 'RegisterGameforgeWorldPermit(' 'The World-permit registration contract must exist'
Assert-Contains $interface 'ConsumeGameforgeWorldPermit(' 'The World-permit consumption contract must exist'
Assert-Contains $client 'RevokeGameforgeWorldPermit(' 'The client proxy must expose permit rollback'

Assert-Contains $parser 'rawPacket.IndexOf("  ", tokenStart, StringComparison.Ordinal)' 'The mandatory double-space boundary must be preserved'
Assert-Contains $parser 'int verticalTabIndex = countryAndVersion.IndexOf' 'The ASCII 0x0B country/version delimiter must be required'
Assert-Contains $parser 'case 8:' 'Japanese region support must remain present'
Assert-Contains $parser 'culture = "ja";' 'Region 8 must map to Japanese'
Assert-Contains $parser 'case 9:' 'Chinese region support must remain present'
Assert-Contains $parser 'culture = "zh";' 'Region 9 must map to Chinese'

Assert-Contains $tickets 'SHA256.Create()' 'Raw authentication tokens must be stored only as SHA-256 lookup keys'
Assert-Contains $tickets '_tickets.TryRemove(' 'Authentication tickets must be consumed atomically'
Assert-Contains $permits '_permits.TryRemove(' 'World permits must be one-use'
Assert-Contains $permits 'string.Equals(permit.IpAddress, normalizedIp' 'World permits must be bound to client IP'

Assert-Contains $project 'Security\GameforgeAuthTicketStore.cs' 'The ticket store must be compiled'
Assert-Contains $project 'Security\GameforgeLoginPacketParser.cs' 'The packet parser must be compiled'
Assert-Contains $project 'Security\GameforgeWorldPermitStore.cs' 'The World permit store must be compiled'
Assert-Contains $configuration 'public static bool EnableGameforgeTokenLogin = false;' 'Modern login must remain disabled until secure keys and the bridge are configured'
Assert-Contains $configuration 'public static bool StartAllRegionalLoginPorts = true;' 'Regional listener support must remain enabled'

foreach ($item in @(
    @{ Content = $login; Name = 'LoginPacketHandler.cs' },
    @{ Content = $entry; Name = 'EntryPointPacketHandler.cs' },
    @{ Content = $program; Name = 'Program.cs' },
    @{ Content = $service; Name = 'AuthentificationService.cs' },
    @{ Content = $interface; Name = 'IAuthentificationService.cs' },
    @{ Content = $client; Name = 'AuthentificationServiceClient.cs' }
)) {
    Assert-RegionBalance $item.Content $item.Name
}

Write-Host 'Gameforge Login -> Master -> World integration contracts verified.'
