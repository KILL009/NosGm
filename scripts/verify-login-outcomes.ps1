param(
    [string]$FixturePath = "tests/fixtures/login-outcomes.json",
    [string]$LoginHandlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$LoginFailTypePath = "Data/NosGm.Domain/LoginFailType.cs"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$stateProperties = @(
    "packetValid",
    "accountFound",
    "exactAccountCase",
    "maintenanceMode",
    "authority",
    "passwordAccepted",
    "versionRequired",
    "serverVersionConfigured",
    "clientVersionAccepted",
    "ipPenalized",
    "staleSessionPersists",
    "accountConnectedRace",
    "hasSelectedCharacter",
    "activeBan",
    "registrationSucceeds",
    "worldListAvailable"
)

$booleanStateProperties = @(
    "packetValid",
    "accountFound",
    "exactAccountCase",
    "maintenanceMode",
    "passwordAccepted",
    "versionRequired",
    "serverVersionConfigured",
    "clientVersionAccepted",
    "ipPenalized",
    "staleSessionPersists",
    "accountConnectedRace",
    "hasSelectedCharacter",
    "activeBan",
    "registrationSucceeds",
    "worldListAvailable"
)

$expectedProperties = @(
    "result",
    "failType",
    "registersMaster",
    "disconnectsMaster",
    "sendsWorldList",
    "disposesPolling"
)

$allowedAuthorities = @("user", "gm", "banned")
$allowedResults = @("silent_drop", "reject", "server_list")
$allowedFailTypes = @(
    "OldClient",
    "Maintenance",
    "AlreadyConnected",
    "AccountOrPasswordWrong",
    "CantConnect",
    "Banned",
    "WrongCaps"
)

$requiredCaseIds = @(
    "success_current_client",
    "maintenance_gm_bypass",
    "malformed_packet",
    "unknown_account",
    "wrong_account_casing",
    "maintenance_user",
    "wrong_credentials",
    "invalid_server_version_configuration",
    "unsupported_client_version",
    "ip_penalty",
    "stale_session_timeout",
    "connected_session_race",
    "active_account_ban",
    "banned_authority",
    "master_registration_failure",
    "world_list_unavailable"
)

function Read-RequiredFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required login fixture file was not found: $Path"
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Assert-ExactProperties {
    param(
        [object]$Value,
        [string[]]$ExpectedNames,
        [string]$Description
    )

    if ($null -eq $Value) {
        throw "$Description must not be null."
    }

    $actualNames = @($Value.PSObject.Properties.Name)
    $unexpected = @($actualNames | Where-Object { $_ -notin $ExpectedNames })
    $missing = @($ExpectedNames | Where-Object { $_ -notin $actualNames })

    if ($unexpected.Count -gt 0) {
        throw "$Description contains forbidden properties: $($unexpected -join ', ')."
    }

    if ($missing.Count -gt 0) {
        throw "$Description is missing required properties: $($missing -join ', ')."
    }
}

function Assert-AllowedProperties {
    param(
        [object]$Value,
        [string[]]$AllowedNames,
        [string]$Description
    )

    if ($null -eq $Value) {
        throw "$Description must not be null."
    }

    $unexpected = @($Value.PSObject.Properties.Name | Where-Object { $_ -notin $AllowedNames })
    if ($unexpected.Count -gt 0) {
        throw "$Description contains forbidden properties: $($unexpected -join ', ')."
    }
}

function Get-PropertyValue {
    param(
        [object]$Value,
        [string]$Name
    )

    return $Value.PSObject.Properties[$Name].Value
}

function Assert-StateTypes {
    param(
        [object]$State,
        [string]$Description
    )

    foreach ($propertyName in $booleanStateProperties) {
        $propertyValue = Get-PropertyValue $State $propertyName
        if ($propertyValue -isnot [bool]) {
            throw "$Description property '$propertyName' must be boolean."
        }
    }

    if ((Get-PropertyValue $State "authority") -notin $allowedAuthorities) {
        throw "$Description authority must be one of: $($allowedAuthorities -join ', ')."
    }
}

function Merge-State {
    param(
        [object]$Defaults,
        [object]$Override
    )

    $merged = [ordered]@{}
    foreach ($propertyName in $stateProperties) {
        $merged[$propertyName] = Get-PropertyValue $Defaults $propertyName
    }

    foreach ($property in $Override.PSObject.Properties) {
        $merged[$property.Name] = $property.Value
    }

    return [pscustomobject]$merged
}

function New-Outcome {
    param(
        [string]$Result,
        [AllowNull()][string]$FailType,
        [bool]$RegistersMaster,
        [bool]$DisconnectsMaster,
        [bool]$SendsWorldList,
        [bool]$DisposesPolling
    )

    return [pscustomobject][ordered]@{
        result = $Result
        failType = $FailType
        registersMaster = $RegistersMaster
        disconnectsMaster = $DisconnectsMaster
        sendsWorldList = $SendsWorldList
        disposesPolling = $DisposesPolling
    }
}

function Get-LoginOutcome {
    param([object]$State)

    if (-not $State.packetValid) {
        return New-Outcome "silent_drop" $null $false $false $false $true
    }

    if (-not $State.accountFound) {
        return New-Outcome "reject" "AccountOrPasswordWrong" $false $false $false $true
    }

    if (-not $State.exactAccountCase) {
        return New-Outcome "reject" "WrongCaps" $false $false $false $true
    }

    if ($State.maintenanceMode -and $State.authority -ne "gm") {
        return New-Outcome "reject" "Maintenance" $false $false $false $true
    }

    if (-not $State.passwordAccepted) {
        return New-Outcome "reject" "AccountOrPasswordWrong" $false $false $false $true
    }

    if ($State.versionRequired) {
        if (-not $State.serverVersionConfigured) {
            return New-Outcome "reject" "CantConnect" $false $false $false $true
        }

        if (-not $State.clientVersionAccepted) {
            return New-Outcome "reject" "OldClient" $false $false $false $true
        }
    }

    if ($State.ipPenalized) {
        return New-Outcome "reject" "CantConnect" $false $false $false $true
    }

    if ($State.staleSessionPersists) {
        return New-Outcome "reject" "AlreadyConnected" $false $false $false $true
    }

    if ($State.accountConnectedRace) {
        $cleansExistingSession = -not $State.hasSelectedCharacter
        return New-Outcome "reject" "AlreadyConnected" $false $cleansExistingSession $false $cleansExistingSession
    }

    if ($State.activeBan -or $State.authority -eq "banned") {
        return New-Outcome "reject" "Banned" $false $false $false $true
    }

    if (-not $State.registrationSucceeds) {
        return New-Outcome "reject" "CantConnect" $false $false $false $true
    }

    if (-not $State.worldListAvailable) {
        return New-Outcome "reject" "CantConnect" $true $true $false $true
    }

    return New-Outcome "server_list" $null $true $false $true $true
}

function Assert-EqualValue {
    param(
        [object]$Actual,
        [object]$Expected,
        [string]$Description
    )

    if ($null -eq $Actual -and $null -eq $Expected) {
        return
    }

    if ($null -eq $Actual -or $null -eq $Expected -or -not $Actual.Equals($Expected)) {
        throw "$Description. Expected '$Expected', actual '$Actual'."
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
        throw "Login source contract failed: $Description"
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
            throw "Login source contract failed: $Description. Missing or out-of-order token: $needle"
        }

        $position = $next + $needle.Length
    }
}

$fixtureJson = Read-RequiredFile $FixturePath
$fixture = $fixtureJson | ConvertFrom-Json
Assert-ExactProperties $fixture @("schemaVersion", "defaults", "cases") "Login fixture root"

if ($fixture.schemaVersion -ne 1) {
    throw "Unsupported login fixture schema version: $($fixture.schemaVersion)."
}

Assert-ExactProperties $fixture.defaults $stateProperties "Login fixture defaults"
Assert-StateTypes $fixture.defaults "Login fixture defaults"

$seenCaseIds = @{}
foreach ($case in @($fixture.cases)) {
    Assert-ExactProperties $case @("id", "override", "expected") "Login fixture case"

    if ($case.id -notmatch '^[a-z][a-z0-9_]{2,63}$') {
        throw "Login fixture case ID '$($case.id)' is not a sanitized symbolic identifier."
    }

    if ($seenCaseIds.ContainsKey($case.id)) {
        throw "Duplicate login fixture case ID: $($case.id)."
    }
    $seenCaseIds[$case.id] = $true

    Assert-AllowedProperties $case.override $stateProperties "Login fixture override '$($case.id)'"
    Assert-ExactProperties $case.expected $expectedProperties "Login fixture expectation '$($case.id)'"

    $state = Merge-State $fixture.defaults $case.override
    Assert-StateTypes $state "Merged login state '$($case.id)'"

    if ($case.expected.result -notin $allowedResults) {
        throw "Fixture '$($case.id)' uses unsupported result '$($case.expected.result)'."
    }

    if ($null -ne $case.expected.failType -and $case.expected.failType -notin $allowedFailTypes) {
        throw "Fixture '$($case.id)' uses unsupported fail type '$($case.expected.failType)'."
    }

    foreach ($booleanName in @("registersMaster", "disconnectsMaster", "sendsWorldList", "disposesPolling")) {
        if ((Get-PropertyValue $case.expected $booleanName) -isnot [bool]) {
            throw "Fixture '$($case.id)' expected property '$booleanName' must be boolean."
        }
    }

    $actual = Get-LoginOutcome $state
    foreach ($propertyName in $expectedProperties) {
        Assert-EqualValue \
            (Get-PropertyValue $actual $propertyName) \
            (Get-PropertyValue $case.expected $propertyName) \
            "Fixture '$($case.id)' failed for '$propertyName'"
    }
}

foreach ($requiredCaseId in $requiredCaseIds) {
    if (-not $seenCaseIds.ContainsKey($requiredCaseId)) {
        throw "Required sanitized login fixture is missing: $requiredCaseId."
    }
}

if ($seenCaseIds.Count -ne $requiredCaseIds.Count) {
    throw "Unexpected login fixtures were added without updating the required-case contract."
}

$handler = Read-RequiredFile $LoginHandlerPath
$failTypes = Read-RequiredFile $LoginFailTypePath

Assert-Ordered $handler @(
    "if (loginPacket == null || string.IsNullOrWhiteSpace(loginPacket.Name) || string.IsNullOrWhiteSpace(loginPacket.Password))",
    "AccountDTO loadedAccount = DAOFactory.AccountDAO.LoadByName(username);",
    "if (loadedAccount == null)",
    "if (!string.Equals(loadedAccount.Name, username, StringComparison.Ordinal))",
    "if (ServerConfiguration.MaintenanceMode && loadedAccount.Authority < AuthorityType.GM)",
    "if (!PasswordHashService.VerifyLoginPayload(",
    "if (ServerConfiguration.GameVersionRequired)",
    "if (DAOFactory.PenaltyLogDAO.LoadByIp(ipAddress).Any())",
    "if (await CheckIsConnectedAsync(loadedAccount.AccountId).ConfigureAwait(false))",
    "if (CommunicationServiceClient.Instance.IsAccountConnected(loadedAccount.AccountId))",
    "if (penalty != null || loadedAccount.Authority == AuthorityType.Banned)",
    "CommunicationServiceClient.Instance.RegisterAccountLogin(",
    "string serversPacket = BuildServersPacket(",
    "CommunicationServiceClient.Instance.DisconnectAccount(loadedAccount.AccountId);",
    "_session.SendPacket(serversPacket);"
) "Login decision order must remain deterministic"

Assert-Regex $handler 'loadedAccount == null.*?Reject\(LoginFailType\.AccountOrPasswordWrong, "Session removed\. Reason: Unknown account"\)' "unknown accounts must use AccountOrPasswordWrong"
Assert-Regex $handler '!string\.Equals\(loadedAccount\.Name, username, StringComparison\.Ordinal\).*?Reject\(LoginFailType\.WrongCaps, "Session removed\. Reason: Wrong account casing"\)' "wrong account casing must use WrongCaps"
Assert-Regex $handler 'MaintenanceMode && loadedAccount\.Authority < AuthorityType\.GM.*?Reject\(LoginFailType\.Maintenance, "Session removed\. Reason: Maintenance mode"\)' "maintenance must reject non-GM accounts"
Assert-Regex $handler '!PasswordHashService\.VerifyLoginPayload\(.*?Reject\(LoginFailType\.AccountOrPasswordWrong, "Session removed\. Reason: Wrong credentials"\)' "invalid credentials must use AccountOrPasswordWrong"
Assert-Regex $handler '!TryParseVersion\(ServerConfiguration\.GameVersion, out Version requiredVersion\).*?Reject\(LoginFailType\.CantConnect, "Session removed\. Reason: Invalid server version configuration"\)' "invalid server version configuration must use CantConnect"
Assert-Regex $handler '!hasClientVersion \|\| !requiredVersion\.Equals\(clientVersion\).*?Reject\(LoginFailType\.OldClient, "Session removed\. Reason: Unsupported client version"\)' "unsupported clients must use OldClient"
Assert-Regex $handler 'PenaltyLogDAO\.LoadByIp\(ipAddress\)\.Any\(\).*?Reject\(LoginFailType\.CantConnect, "Session removed\. Reason: IP penalty"\)' "IP penalties must use CantConnect"
Assert-Regex $handler 'CheckIsConnectedAsync\(loadedAccount\.AccountId\).*?Reject\(LoginFailType\.AlreadyConnected, "Session removed\. Reason: Already connected"\)' "persistent sessions must use AlreadyConnected"
Assert-Regex $handler 'IsAccountConnected\(loadedAccount\.AccountId\).*?_session\.SendPacket\(\$"failc \{\(byte\)LoginFailType\.AlreadyConnected\}"\).*?DisconnectAccount\(loadedAccount\.AccountId\).*?DisposeLoginPolling\(\)' "the duplicate-session race must reject and clean the old Master registration"
Assert-Regex $handler 'penalty != null \|\| loadedAccount\.Authority == AuthorityType\.Banned.*?Reject\(LoginFailType\.Banned, "Session removed\. Reason: Banned"\)' "active bans must use Banned"
Assert-Regex $handler 'catch \(Exception ex\).*?Reject\(LoginFailType\.CantConnect, "Session removed\. Reason: Login registration failed"\)' "Master registration failures must use CantConnect"
Assert-Regex $handler 'Client has been removed\. Reason: World Server not found.*?LoginFailType\.CantConnect' "missing World lists must use CantConnect"
Assert-Regex $handler 'string\.IsNullOrWhiteSpace\(serversPacket\).*?DisconnectAccount\(loadedAccount\.AccountId\).*?DisposeLoginPolling\(\)' "missing World lists must roll back Master registration"
Assert-Regex $handler '_session\.SendPacket\(serversPacket\).*?Server list sent.*?DisposeLoginPolling\(\)' "successful logins must send the world list and stop polling"

Assert-Regex $failTypes 'OldClient\s*=\s*1' "OldClient failc code changed"
Assert-Regex $failTypes 'Maintenance\s*=\s*3' "Maintenance failc code changed"
Assert-Regex $failTypes 'AlreadyConnected\s*=\s*4' "AlreadyConnected failc code changed"
Assert-Regex $failTypes 'AccountOrPasswordWrong\s*=\s*5' "AccountOrPasswordWrong failc code changed"
Assert-Regex $failTypes 'CantConnect\s*=\s*6' "CantConnect failc code changed"
Assert-Regex $failTypes 'Banned\s*=\s*7' "Banned failc code changed"
Assert-Regex $failTypes 'WrongCaps\s*=\s*9' "WrongCaps failc code changed"

Write-Host "Verified $($seenCaseIds.Count) sanitized login outcome fixtures and their production source contracts."
