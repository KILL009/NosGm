param(
    [string]$SessionFactoryPath = "Data/NosGm.Core/SessionFactory.cs",
    [string]$LoginPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$StorePath = "Data/NosGm.Master.Library/Security/GameforgeAuthTicketStore.cs",
    [string]$AuthInterfacePath = "Data/NosGm.Master.Library/Interface/IAuthentificationService.cs",
    [string]$AuthServicePath = "Data/NosGm.Program/NosGm.Master.Server/AuthentificationService.cs",
    [string]$CommunicationInterfacePath = "Data/NosGm.Master.Library/Interface/ICommunicationService.cs",
    [string]$CommunicationServicePath = "Data/NosGm.Program/NosGm.Master.Server/CommunicationService.cs",
    [string]$CommunicationClientPath = "Data/NosGm.Master.Library/Client/CommunicationServiceClient.cs"
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
$store = Read-RequiredText $StorePath
$authInterface = Read-RequiredText $AuthInterfacePath
$authService = Read-RequiredText $AuthServicePath
$communicationInterface = Read-RequiredText $CommunicationInterfacePath
$communicationService = Read-RequiredText $CommunicationServicePath
$communicationClient = Read-RequiredText $CommunicationClientPath

Assert-Contains $store "[Serializable]" "The remote ticket consumption result must be serializable."
Assert-Contains $store "public sealed class GameforgeAuthTicketConsumption" "The stable ticket consumption DTO is missing."
Assert-Contains $store "public int ConsumptionNumber" "The bounded modern stage number is missing."
Assert-Contains $store "public int SessionId" "The stable modern SessionId is missing."
Assert-Contains $store "if (ticket.SessionId <= 0) ticket.SessionId = proposedSessionId;" "The first valid ticket stage must bind its proposed SessionId exactly once."
Assert-Contains $store "SessionId = ticket.SessionId" "Every ticket stage must return the bound SessionId."
Assert-Contains $store "if (ticket.RemainingConsumptions == 0)" "The ticket must still disappear after the third stage."
Assert-Contains $sessionFactory "Interlocked.Add(ref _sessionCounter, 2)" "Concurrent Login stages could generate duplicate proposed SessionIds."

Assert-Regex $authInterface 'ConsumeGameforgeAuthTicket\s*\(\s*string authToken,\s*string installationId,\s*byte countryId,\s*int proposedSessionId\s*\)' "The authentication contract does not carry the proposed SessionId."
Assert-Regex $authService 'TryConsume\s*\(\s*authToken,\s*parsedInstallationId,\s*countryId,\s*proposedSessionId,\s*out GameforgeAuthTicketConsumption consumption\s*\)' "Master does not atomically bind and return the modern SessionId."

Assert-Contains $communicationInterface "bool IsAccountSessionRegistered(long accountId, int sessionId);" "The exact account/session query is missing."
Assert-Regex $communicationService 'IsAccountSessionRegistered\s*\(long accountId, int sessionId\).*?AccountId\.Equals\(accountId\).*?SessionId\.Equals\(sessionId\)' "The exact account/session query is not tuple-bound."
Assert-Regex $communicationService 'RegisterAccountLogin\s*\(long accountId, int sessionId, string ipAddress\).*?lock \(MSManager\.Instance\.ConnectedAccounts\).*?existing != null\) return;' "Repeated modern stages could replace an AccountConnection that World already attached."
Assert-Contains $communicationService "private const int NsTeSTPadding = 56;" "The fixed NsTeST padding changed and would move the client SessionId."
Assert-Contains $communicationClient 'return $"{header}  {region} {account} 2 {remainder}";' "Login does not add the required modern NsTeST header and single mode field."
Assert-Regex $communicationClient 'NormalizeNsTeSTPacketLayout.*?return \$"\{header\}\s\s\{region\} \{account\} 2 \{remainder\}";' "The NsTeST normalizer can no longer prove the modern packet layout."
if ($communicationClient.IndexOf(
        'return $"{header}  {region} {account} 2 0 0 0 0 0 0 {remainder}";',
        [StringComparison]::Ordinal) -ge 0) {
    throw "The obsolete six-zero NsTeST insertion shifts the World SessionId."
}

Assert-Contains $login "gameforgeTicket?.SessionId ?? SessionFactory.Instance.GenerateSessionId()" "Login does not distinguish stable modern sessions from generated legacy sessions."
Assert-Contains $login "IsAccountSessionRegistered(loadedAccount.AccountId, newSessionId)" "A later modern stage cannot recognize its already registered session."
Assert-Contains $login "gameforgeTicket?.IsFirstConsumption == true" "World permit issuance is not owned exclusively by stage one."
Assert-Contains $login "if (accountRegistered && ownsAccountRegistration)" "A later modern stage could disconnect the shared account during rollback."
Assert-Regex $login 'RegisterGameforgeWorldPermit\(loadedAccount\.AccountId, newSessionId, ipAddress\).*?BuildServersPacket\(' "The one-use World permit must exist before the first server list is sent."

Write-Host "[PASS] Modern NoS0577 stages share one stable SessionId, preserve an attached World session and issue one bounded World permit."
