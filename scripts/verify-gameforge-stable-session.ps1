param(
    [string]$SessionFactoryPath = "Data/NosGm.Core/SessionFactory.cs",
    [string]$LoginPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$EntryPath = "Data/NosGm.Handler/PacketHandler/CharScreen/EntryPointPacketHandler.cs",
    [string]$ClientSessionPath = "Data/NosGm.GameObject/Networking/ClientSession.cs",
    [string]$StorePath = "Data/NosGm.Master.Library/Security/GameforgeAuthTicketStore.cs",
    [string]$AuthInterfacePath = "Data/NosGm.Master.Library/Interface/IAuthentificationService.cs",
    [string]$AuthServicePath = "Data/NosGm.Program/NosGm.Master.Server/AuthentificationService.cs",
    [string]$CommunicationInterfacePath = "Data/NosGm.Master.Library/Interface/ICommunicationService.cs",
    [string]$CommunicationServicePath = "Data/NosGm.Program/NosGm.Master.Server/CommunicationService.cs",
    [string]$CommunicationClientPath = "Data/NosGm.Master.Library/Client/CommunicationServiceClient.cs",
    [string]$ScsCommunicationAdapterPath = "Data/NosGm.Master.Library/Client/ScsClusterCommunicationTransport.cs"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-RequiredText([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Stable-session source file is missing: $Path"
    }
    return Get-Content -LiteralPath $Path -Raw
}

function Assert-Contains([string]$Content, [string]$Needle, [string]$Message) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw $Message
    }
}

function Assert-NotContains([string]$Content, [string]$Needle, [string]$Message) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw $Message
    }
}

function Assert-Regex([string]$Content, [string]$Pattern, [string]$Message) {
    if (-not [regex]::IsMatch(
            $Content,
            $Pattern,
            [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw $Message
    }
}

$sessionFactory = Read-RequiredText $SessionFactoryPath
$login = Read-RequiredText $LoginPath
$entry = Read-RequiredText $EntryPath
$clientSession = Read-RequiredText $ClientSessionPath
$store = Read-RequiredText $StorePath
$authInterface = Read-RequiredText $AuthInterfacePath
$authService = Read-RequiredText $AuthServicePath
$communicationInterface = Read-RequiredText $CommunicationInterfacePath
$communicationService = Read-RequiredText $CommunicationServicePath
$communicationClient = Read-RequiredText $CommunicationClientPath
$scsCommunicationAdapter = Read-RequiredText $ScsCommunicationAdapterPath

Assert-Contains $store "[Serializable]" "The remote ticket consumption result must be serializable."
Assert-Contains $store "public sealed class GameforgeAuthTicketConsumption" "The stable ticket consumption DTO is missing."
Assert-Contains $store "public int ConsumptionNumber" "The renewable modern entry number is missing."
Assert-Contains $store "public int SessionId" "The stable modern SessionId is missing."
Assert-Regex $store 'if \(ticket\.SessionId <= 0\).*?ticket\.SessionId = proposedSessionId;.*?ticket\.ExpiresAtUtc = nowUtc\.Add\(MaximumActiveSessionLifetime\);' "The first valid entry must bind its proposed SessionId and convert the short ticket into a bounded active-session lease."
Assert-Contains $store "SessionId = ticket.SessionId" "Every ticket entry must return the bound SessionId."
Assert-Contains $store "public static readonly TimeSpan MaximumActiveSessionLifetime = TimeSpan.FromHours(24);" "The active-session lease must remain explicitly bounded to 24 hours."
Assert-Contains $store "ticket.ConsumptionCount++;" "Repeated character-selection entries must advance the session entry counter."
Assert-NotContains $store "MaximumConsumptionsPerTicket" "The obsolete three-entry ticket limit returned."
Assert-NotContains $store "RemainingConsumptions" "The obsolete remaining-consumption counter returned."
Assert-Contains $sessionFactory "Interlocked.Add(ref _sessionCounter, 2)" "Concurrent Login entries could generate duplicate proposed SessionIds."

Assert-Regex $authInterface 'ConsumeGameforgeAuthTicket\s*\(\s*string authToken,\s*string installationId,\s*byte countryId,\s*int proposedSessionId\s*\)' "The authentication contract does not carry the proposed SessionId."
Assert-Regex $authService 'TryConsume\s*\(\s*authToken,\s*parsedInstallationId,\s*countryId,\s*proposedSessionId,\s*out GameforgeAuthTicketConsumption consumption\s*\)' "Master does not atomically bind and return the modern SessionId."

Assert-Contains $communicationInterface "bool IsAccountSessionRegistered(long accountId, int sessionId);" "The exact account/session query is missing."
Assert-Contains $communicationInterface "void DisconnectAccount(long accountId, int sessionId = 0, bool preserveSessionRegistration = false);" "The disconnect contract cannot preserve an exact Gameforge session registration."
Assert-Regex $communicationService 'IsAccountSessionRegistered\s*\(long accountId, int sessionId\).*?AccountId\.Equals\(accountId\).*?SessionId\.Equals\(sessionId\)' "The exact account/session query is not tuple-bound."
Assert-Regex $communicationService 'RegisterAccountLogin\s*\(long accountId, int sessionId, string ipAddress\).*?lock \(MSManager\.Instance\.ConnectedAccounts\).*?existing != null\) return;' "Repeated modern entries could replace an AccountConnection that World already attached."
Assert-Regex $communicationService 'DisconnectAccount\s*\(\s*long accountId,\s*int sessionId = 0,\s*bool preserveSessionRegistration = false\s*\).*?preserveSessionRegistration && sessionId > 0.*?AccountId\.Equals\(accountId\).*?SessionId\.Equals\(sessionId\).*?CharacterId = 0;.*?ConnectedWorld = null;.*?LastPulse = DateTime\.Now;.*?return;' "World disconnect does not preserve and detach only the exact Gameforge account/session tuple."
Assert-Contains $communicationClient "_communicationTransport.DisconnectAccountAsync(" "The communication client drops the Gameforge preservation flag before transport dispatch."
Assert-Regex $scsCommunicationAdapter '_serviceProxy\(\)\.DisconnectAccount\(\s*accountId,\s*sessionId,\s*preserveSessionRegistration\s*\)' "The selected SCS transport drops the Gameforge preservation flag."
Assert-Contains $communicationService "private const int NsTeSTPadding = 56;" "The fixed NsTeST padding changed."
Assert-Contains $communicationClient 'return $"{header}  {region} {account} 2 {remainder}";' "Login does not add the required modern NsTeST header and single mode field."
Assert-Regex $communicationClient 'NormalizeNsTeSTPacketLayout.*?return \$"\{header\}\s\s\{region\} \{account\} 2 \{remainder\}";' "The NsTeST normalizer can no longer prove the modern packet layout."
if ($communicationClient.IndexOf(
        'return $"{header}  {region} {account} 2 0 0 0 0 0 0 {remainder}";',
        [StringComparison]::Ordinal) -ge 0) {
    throw "The obsolete six-zero NsTeST insertion shifts the World SessionId."
}

Assert-Contains $login "gameforgeTicket?.SessionId ?? SessionFactory.Instance.GenerateSessionId()" "Login does not distinguish stable modern sessions from generated legacy sessions."
Assert-Contains $login "IsAccountSessionRegistered(loadedAccount.AccountId, newSessionId)" "A later modern entry cannot recognize its already registered session."
Assert-Contains $login "!gameforgeTicket.IsFirstConsumption" "Login no longer distinguishes the initial entry from active-session continuations."
Assert-Contains $login "Gameforge session is no longer active" "A stale authorization could recreate a disconnected Master session."
Assert-Contains $login "bool issueGameforgeWorldPermit = gameforgeTicket != null;" "Every accepted modern Login entry must issue a fresh one-use World permit."
if ($login.IndexOf(
        'bool issueGameforgeWorldPermit = gameforgeTicket?.IsFirstConsumption == true;',
        [StringComparison]::Ordinal) -ge 0) {
    throw "Character reselection would reuse an already consumed entry-one World permit."
}
Assert-Contains $login "if (accountRegistered && ownsAccountRegistration)" "A later modern entry could disconnect the shared account during rollback."
Assert-Regex $login 'RegisterGameforgeWorldPermit\(loadedAccount\.AccountId, newSessionId, ipAddress\).*?BuildServersPacket\(' "A fresh one-use World permit must exist before every modern server list is sent."

Assert-Regex $entry 'Session\.InitializeAccount\(\s*new Account\(account\),\s*isCrossServerLogin,\s*isGameforgePasswordlessLogin\s*\)' "Validated Gameforge mode is not carried into the World session lifecycle."
Assert-Contains $clientSession "public bool PreserveAccountRegistrationOnDisconnect { get; private set; }" "ClientSession does not remember the validated Gameforge lifecycle."
Assert-Regex $clientSession 'InitializeAccount\s*\(\s*Account account,\s*bool crossServer = false,\s*bool preserveAccountRegistrationOnDisconnect = false\s*\).*?PreserveAccountRegistrationOnDisconnect = preserveAccountRegistrationOnDisconnect;' "Account initialization does not bind the Gameforge preservation flag."
Assert-Regex $clientSession 'DisconnectAccount\(\s*Account\.AccountId,\s*SessionId,\s*PreserveAccountRegistrationOnDisconnect\s*\)' "World teardown still deletes the active Gameforge Master registration."

Write-Host "[PASS] Modern NoS0577 entries share one stable SessionId, preserve the exact Master registration across World reconnects and issue a fresh one-use permit for every character reselection."
