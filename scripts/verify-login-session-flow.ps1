param(
    [string]$LoginHandlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$MasterServicePath = "Data/NosGm.Program/NosGm.Master.Server/CommunicationService.cs",
    [string]$ClientSessionPath = "Data/NosGm.GameObject/Networking/ClientSession.cs",
    [string]$EntryHandlerPath = "Data/NosGm.Handler/PacketHandler/CharScreen/EntryPointPacketHandler.cs",
    [string]$EntryPacketPath = "Data/NosGm.Packets/Packets/ClientPackets/EntryPointPacket.cs",
    [string]$SelectHandlerPath = "Data/NosGm.Handler/PacketHandler/CharScreen/SelectCharacterPacketHandler.cs"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Source {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required session-flow source file not found: $Path"
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
        throw "Session-flow contract failed: $Description"
    }
}

function Assert-NotContains {
    param(
        [string]$Content,
        [string]$Needle,
        [string]$Description
    )

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "Session-flow contract failed: $Description"
    }
}

function Assert-Regex {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Description
    )

    if (-not [regex]::IsMatch(
            $Content,
            $Pattern,
            [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "Session-flow contract failed: $Description"
    }
}

function Assert-NotRegex {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Description
    )

    if ([regex]::IsMatch(
            $Content,
            $Pattern,
            [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "Session-flow contract failed: $Description"
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
            throw "Session-flow contract failed: $Description. Missing or out-of-order token: $needle"
        }

        $position = $next + $needle.Length
    }
}

function Get-Section {
    param(
        [string]$Content,
        [string]$StartMarker,
        [string]$EndMarker,
        [string]$Description
    )

    $start = $Content.IndexOf($StartMarker, [StringComparison]::Ordinal)
    if ($start -lt 0) {
        throw "Session-flow contract failed: $Description start marker was not found."
    }

    $end = $Content.IndexOf($EndMarker, $start + $StartMarker.Length, [StringComparison]::Ordinal)
    if ($end -lt 0) {
        throw "Session-flow contract failed: $Description end marker was not found."
    }

    return $Content.Substring($start, $end - $start)
}

$login = Read-Source $LoginHandlerPath
$master = Read-Source $MasterServicePath
$clientSession = Read-Source $ClientSessionPath
$entry = Read-Source $EntryHandlerPath
$entryPacket = Read-Source $EntryPacketPath
$select = Read-Source $SelectHandlerPath

$loginFlow = Get-Section -Content $login -StartMarker "public async Task VerifyLoginAsync(LoginPacket loginPacket)" -EndMarker "private async Task<bool> CheckIsConnectedAsync" -Description "Login handler"

Assert-Ordered $loginFlow @(
    "int newSessionId = SessionFactory.Instance.GenerateSessionId();",
    "CommunicationServiceClient.Instance.RegisterAccountLogin(",
    "string serversPacket = BuildServersPacket(",
    "_session.SendPacket(serversPacket);",
    "DisposeLoginPolling();"
) "Login must register the generated session before retrieving and sending the world list"

Assert-Regex $loginFlow 'RegisterAccountLogin\s*\(\s*loadedAccount\.AccountId\s*,\s*newSessionId\s*,\s*ipAddress\s*\)' "Login must register the account with the generated session ID and normalized IP"
Assert-Regex $loginFlow 'BuildServersPacket\s*\(\s*username\s*,\s*loginPacket\.RegionType\s*,\s*newSessionId\s*,\s*ignoreUserName\s*,\s*loadedAccount\.AccountId\s*\)' "World-list generation must use the same generated session ID"
Assert-Contains $loginFlow "CommunicationServiceClient.Instance.DisconnectAccount(loadedAccount.AccountId);" "Failed world-list generation must roll back the Master account registration"

Assert-Regex $master 'public void RegisterAccountLogin\s*\(long accountId, int sessionId, string ipAddress\).*?ConnectedAccounts\.RemoveAll\(a => a\.AccountId\.Equals\(accountId\)\).*?ConnectedAccounts\.Add\(new AccountConnection\(accountId, sessionId, ipAddress\)\);' "Master must replace stale account registrations with the new account/session/IP tuple"
Assert-Regex $master 'public bool IsLoginPermitted\s*\(long accountId, int sessionId\).*?AccountId\.Equals\(accountId\).*?SessionId\.Equals\(sessionId\).*?ConnectedWorld == null' "Master permission checks must bind both account ID and session ID before World attachment"
Assert-Regex $master 'public bool ConnectAccount\s*\(Guid worldId, long accountId, int sessionId\).*?AccountId\.Equals\(accountId\) && a\.SessionId\.Equals\(sessionId\).*?account\.ConnectedWorld =' "World attachment must resolve the same account/session pair"

$receiveFlow = Get-Section -Content $clientSession -StartMarker "private bool ProcessReceivedMessage(byte[] packetData)" -EndMarker "private void OnNetworkClientMessageReceived" -Description "World session bootstrap"
Assert-Ordered $receiveFlow @(
    "SessionId = sessid;",
    'TriggerHandler("NosGm.EntryPoint", string.Empty, false);'
) "World must assign the decrypted session ID before starting the entry-point packet bundle"

$initializeAccount = Get-Section -Content $clientSession -StartMarker "public void InitializeAccount(Account account, bool crossServer = false)" -EndMarker "public void ReceivePacket" -Description "Account initialization"
Assert-Ordered $initializeAccount @(
    "CommunicationServiceClient.Instance.ConnectAccount(ServerManager.Instance.WorldId, account.AccountId, SessionId);",
    "IsAuthenticated = true;"
) "Normal account initialization must attach the same World session before authentication is exposed"

Assert-Contains $entryPacket '[PacketHeader("NosGm.EntryPoint", IsCharScreen = true, Amount = 3)]' "The entry-point bundle must continue waiting for exactly three client packets"
Assert-Contains $entry "? Array.Empty<string>()" "Missing entry packet data must use a safe empty-array fallback"
Assert-Contains $entry ": packet.PacketData.Split(' ');" "Entry parsing must preserve historical empty-token field positions"
Assert-Regex $entry 'IsLoginPermitted\s*\(\s*account\.AccountId\s*,\s*Session\.SessionId\s*\)' "World entry authorization must use the decrypted session ID owned by ClientSession"
Assert-NotRegex $entry 'IsLoginPermitted\s*\(\s*account\.AccountId\s*,\s*loginPacketParts' "World entry must never trust a session ID supplied inside the packet payload"
Assert-Regex $entry 'loginPacketParts\.Length <= 8.*?loginPacketParts\[8\].*?CrossServerAuthenticate' "Cross-server authentication must validate index 8 before reading the marker"
Assert-Regex $entry 'PasswordHashService\.VerifyPassword\s*\(\s*account\.Password\s*,\s*loginPacketParts\[7\]\s*,\s*true' "Normal World entry must verify the expected SHA-512 credential field"
Assert-Ordered $entry @(
    "Session.InitializeAccount(new Account(account), isCrossServerLogin);",
    "ServerManager.Instance.CharacterScreenSessions[Session.Account.AccountId] = Session;"
) "The account must be initialized before registering the character-screen session"
Assert-Ordered $entry @(
    'Session.SendPacket("clist_start 0");',
    'Session.SendPacket($"clist ',
    'Session.SendPacket("clist_end");'
) "Character-list packets must retain their start, item and end ordering"
Assert-NotContains $entry "Logger.Info(packet.PacketData);" "Character entry must not log the credential-bearing raw packet"

Assert-Contains $select "if (Session?.Account == null || Session.HasSelectedCharacter)" "Character selection must reject missing accounts and duplicate selection"
Assert-Ordered $select @(
    "DAOFactory.CharacterDAO.LoadBySlot(Session.Account.AccountId, selectPacket.Slot);",
    "character.Initialize();",
    "Session.SetCharacter(character);",
    'Session.SendPacket("OK");',
    "CommunicationServiceClient.Instance.ConnectCharacter(ServerManager.Instance.WorldId, character.CharacterId);"
) "Character selection must initialize state, acknowledge the client and then register the character"

Write-Host "Login -> Master -> World -> character-selection contracts verified."
