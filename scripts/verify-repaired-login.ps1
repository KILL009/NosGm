param(
    [string]$LoginPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$EntryPath = "Data/NosGm.Handler/PacketHandler/CharScreen/EntryPointPacketHandler.cs",
    [string]$ProgramPath = "Data/NosGm.Program/NosGm.Login/Program.cs",
    [string]$ServicePath = "Data/NosGm.Program/NosGm.Master.Server/AuthentificationService.cs",
    [string]$InterfacePath = "Data/NosGm.Master.Library/Interface/IAuthentificationService.cs",
    [string]$ClientPath = "Data/NosGm.Master.Library/Client/AuthentificationServiceClient.cs",
    [string]$ParserPath = "Data/NosGm.Master.Library/Security/GameforgeLoginPacketParser.cs",
    [string]$StorePath = "Data/NosGm.Master.Library/Security/GameforgeAuthTicketStore.cs",
    [string]$LanguagePath = "Data/NosGm.Core/Language.cs",
    [string]$ConfigurationPath = "Data/NosGm.Configuration/ServerConfiguration.cs"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Source([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { throw "Missing repaired-login file: $Path" }
    return Get-Content -LiteralPath $Path -Raw
}
function Require([string]$Content, [string]$Needle, [string]$Message) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) { throw $Message }
}
function Forbid([string]$Content, [string]$Needle, [string]$Message) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) { throw $Message }
}
function Require-Regex([string]$Content, [string]$Pattern, [string]$Message) {
    if (-not [regex]::IsMatch($Content, $Pattern, [Text.RegularExpressions.RegexOptions]::Singleline)) { throw $Message }
}
function Forbid-Regex([string]$Content, [string]$Pattern, [string]$Message) {
    if ([regex]::IsMatch($Content, $Pattern, [Text.RegularExpressions.RegexOptions]::Singleline)) { throw $Message }
}
function Require-Count([string]$Content, [string]$Needle, [int]$Expected, [string]$Message) {
    $count = ([regex]::Matches($Content, [regex]::Escape($Needle))).Count
    if ($count -ne $Expected) { throw "$Message Expected=$Expected Actual=$count" }
}
function Require-Ordered([string]$Content, [string[]]$Needles, [string]$Message) {
    $position = 0
    foreach ($needle in $Needles) {
        $next = $Content.IndexOf($needle, $position, [StringComparison]::Ordinal)
        if ($next -lt 0) { throw "$Message Missing/out-of-order: $needle" }
        $position = $next + $needle.Length
    }
}
function Require-BalancedRegions([string]$Content, [string]$Name) {
    $starts = ([regex]::Matches($Content, '(?m)^\s*#region\b')).Count
    $ends = ([regex]::Matches($Content, '(?m)^\s*#endregion\b')).Count
    if ($starts -ne $ends) { throw "$Name has unbalanced regions: $starts/$ends" }
}

$login = Read-Source $LoginPath
$entry = Read-Source $EntryPath
$program = Read-Source $ProgramPath
$service = Read-Source $ServicePath
$interface = Read-Source $InterfacePath
$client = Read-Source $ClientPath
$parser = Read-Source $ParserPath
$store = Read-Source $StorePath
$language = Read-Source $LanguagePath
$config = Read-Source $ConfigurationPath

Forbid $program '_port = port;' 'Login Program still contains the abandoned single-port merge fragment.'
Forbid $program 'var networkManager = new NetworkManager' 'Login Program still creates a discarded duplicate listener.'
Require-Count $program 'CommunicationServiceClient.Instance.Authenticate(' 1 'Master communication must authenticate exactly once.'
Require-Count $program 'AntiSpamModule.Instance.RunBlacklistTask();' 1 'AntiSpam must start exactly once.'
Require $program 'Enumerable.Range(ClientRegionMap.BaseLoginPort, ClientRegionMap.RegionCount)' 'Regional Login listeners are missing.'
Require-Regex $program 'AuthentificationServiceClient\.Instance\.Authenticate\(ServerConfiguration\.GameforgeTicketConsumerKey\)' 'Login must authenticate as ticket consumer.'
Forbid-Regex $program 'AuthentificationServiceClient\.Instance\.Authenticate\(ServerConfiguration\.GameforgeTicketIssuerKey\)' 'Login must never authenticate as ticket issuer.'

Require-Count $login '[Packet("NoS0576", "NoS0577")]' 1 'Exactly one handler must own NoS0576 and NoS0577.'
Forbid $login 'VerifyModernLoginAsync' 'The duplicate modern-login handler returned.'
Forbid $login 'ConsumeModernLoginTicket' 'The superseded generic token broker returned.'
Forbid $login 'RegisterModernLoginSession' 'The superseded generic World permit returned.'
Forbid $login 'Logger.Info(rawPacket' 'Raw authentication packets must not be logged.'
Require-Ordered $login @(
    'GameforgeLoginPacketParser.TryParse(',
    'TryResolveClientRegion(out byte listenerRegionType',
    'GameforgeLoginPacketParser.TryGetCulture(payload.CountryId',
    'ConsumeGameforgeAuthTicket(',
    'RegisterAccountLogin(',
    'RegisterGameforgeWorldPermit(',
    'string serversPacket = BuildServersPacket(',
    '_session.SendPacket(serversPacket);'
) 'The Gameforge Login flow is not ordered safely.'
Require $login 'Gameforge region selected by ticket-bound packet' 'Modern Login does not record listener/packet region differences safely.'
Require-Regex $login 'ConsumeGameforgeAuthTicket\(\s*payload\.AuthToken,\s*payload\.InstallationId\.ToString\("D"\),\s*payload\.CountryId\)' 'Modern tickets must be consumed against the packet country that Master cryptographically bound at issue time.'
Forbid-Regex $login 'ConsumeGameforgeAuthTicket\(\s*payload\.AuthToken,\s*payload\.InstallationId\.ToString\("D"\),\s*resolvedRegionType\)' 'Modern Login must not derive the ticket region from a fixed listener port.'
Require-Regex $login 'CompleteLoginAsync\(\s*loadedAccount,\s*loadedAccount\.Name,\s*payload\.CountryId,\s*clientCulture' 'The authenticated ticket country must drive the effective region and culture.'
Forbid $login 'Gameforge CountryId overridden by trusted Login port' 'The obsolete trusted-port region model returned.'
Forbid $login 'Session removed. Reason: Gameforge country does not match the trusted Login port' 'Modern clients must not be rejected merely because they use the base Login listener.'
Require $login 'PasswordHashService.VerifyLoginPayload(' 'Legacy NoS0575 password verification was removed.'
Require $login 'ClientRegionMap.TryResolveLoginPort(_session.ListeningPort' 'Login no longer validates that the accepted local port is configured.'
Require $login 'CommunicationServiceClient.Instance.DisconnectAccount(loadedAccount.AccountId);' 'Failed Login registration no longer rolls Master back.'

Require-Ordered $entry @(
    'IsLoginPermitted(',
    'string.Equals(loginPacketParts[7], "thisisgfmode", StringComparison.Ordinal);',
    'ConsumeGameforgeWorldPermit(',
    'Session.InitializeAccount(new Account(account), isCrossServerLogin);'
) 'World entry does not consume the one-use permit after normal session authorization.'
Require $entry 'isGameforgePasswordlessLogin ||' 'World password bypass is not restricted to validated Gameforge entry.'
Require $entry 'PasswordHashService.VerifyPassword(account.Password, loginPacketParts[7], true, out _)' 'Normal World password verification disappeared.'

Require $interface 'RegisterGameforgeAuthTicket' 'Ticket issuer contract is missing.'
Require $interface 'ConsumeGameforgeAuthTicket' 'Ticket consumer contract is missing.'
Require $interface 'RegisterGameforgeWorldPermit' 'World permit registration contract is missing.'
Require $interface 'ConsumeGameforgeWorldPermit' 'World permit consumption contract is missing.'
Forbid $interface '#endregion' 'Authentication interface still contains an orphaned preprocessor region.'
Require $client 'RevokeGameforgeWorldPermit' 'Client proxy cannot roll a permit back.'

Require $service 'IsGameforgeIssuerClient()' 'Master does not separate issuer role.'
Require $service 'IsGameforgeConsumerClient()' 'Master does not separate consumer role.'
Require $service 'IsLegacyAuthClient()' 'World authentication role is missing.'
Require $service 'GameforgeAuthTicketStore.Instance.TryIssue' 'Master does not issue central tickets.'
Require $service 'GameforgeAuthTicketStore.Instance.TryConsume' 'Master does not atomically consume tickets.'
Require $service 'GameforgeWorldPermitStore.Instance.TryIssue' 'Master does not issue World permits.'
Require $service 'GameforgeWorldPermitStore.Instance.TryConsume' 'Master does not consume World permits.'

Require $parser 'MaximumCountryId = 9' 'Parser does not accept all ten region bytes.'
Require $parser 'rawPacket.IndexOf("  ", tokenStart, StringComparison.Ordinal)' 'Parser lost the mandatory double-space token boundary.'
Require $parser 'int verticalTabIndex = countryAndVersion.IndexOf' 'Parser lost the ASCII 0x0B country/version boundary.'
Require $parser 'case 8: culture = "ja";' 'Region 8 is not Japanese.'
Require $parser 'case 9: culture = "zh";' 'Region 9 is not Chinese.'
Require $store 'SHA256.Create()' 'Raw tickets are not reduced to SHA-256 lookup keys.'
Require $store 'public const int MaximumConsumptionsPerTicket = 3;' 'Modern Login tickets must allow exactly the client language-list, regional-selection and channel-selection stages.'
Require $store 'RemainingConsumptions = MaximumConsumptionsPerTicket' 'Issued tickets do not initialize the bounded consumption count.'
Require $store 'lock (ticket)' 'Ticket consumption is not serialized per credential.'
Require $store 'ticket.RemainingConsumptions--;' 'Ticket consumption no longer decrements the bounded use count.'
Require $store 'if (ticket.RemainingConsumptions == 0)' 'Fully consumed tickets are not removed.'
Require $store 'ticket.InstallationId != installationId' 'Tickets are no longer bound to InstallationId.'
Require $store 'ticket.CountryId != countryId' 'Tickets are no longer bound to the region supplied at issue time.'
Require $store '_permits.TryRemove(' 'World permits are not one-use.'
Require $store 'string.Equals(permit.IpAddress, normalizedIp' 'World permits are not bound to IP.'

$profiles = @(
    'new ClientLanguageProfile(0, 4000, "EN", "UK", "en")',
    'new ClientLanguageProfile(1, 4001, "DE", "DE", "de")',
    'new ClientLanguageProfile(2, 4002, "FR", "FR", "fr")',
    'new ClientLanguageProfile(3, 4003, "IT", "IT", "it")',
    'new ClientLanguageProfile(4, 4004, "PL", "PL", "pl")',
    'new ClientLanguageProfile(5, 4005, "ES", "ES", "es")',
    'new ClientLanguageProfile(6, 4006, "CZ", "CZ", "cs")',
    'new ClientLanguageProfile(7, 4007, "RU", "RU", "ru")',
    'new ClientLanguageProfile(8, 4008, "JP", "JP", "ja")',
    'new ClientLanguageProfile(9, 4009, "CN", "CN", "zh")'
)
foreach ($profile in $profiles) { Require $language $profile "Missing regional profile: $profile" }
Forbid $language 'new ClientLanguageProfile(8, 4008, "TR"' 'Turkish incorrectly replaced the Japanese regional slot.'
Require $config 'public static bool EnableGameforgeTokenLogin = false;' 'Modern login must remain disabled until keys and bridge are configured.'
Require $config 'public static int GameforgeWorldPermitTtlSeconds = 120;' 'World permit TTL is missing.'

foreach ($item in @(
    @{ Name = 'Program.cs'; Content = $program },
    @{ Name = 'LoginPacketHandler.cs'; Content = $login },
    @{ Name = 'EntryPointPacketHandler.cs'; Content = $entry },
    @{ Name = 'AuthentificationService.cs'; Content = $service },
    @{ Name = 'AuthentificationServiceClient.cs'; Content = $client }
)) { Require-BalancedRegions $item.Content $item.Name }

Write-Host 'Repaired Login -> Master -> World, bounded three-stage tickets, ticket-bound modern region and ten-language contracts verified.'
