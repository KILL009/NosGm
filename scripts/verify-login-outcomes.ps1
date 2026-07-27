param(
    [string]$FixturePath = "tests/fixtures/login-outcomes.json",
    [string]$LoginHandlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$LoginFailTypePath = "Data/NosGm.Domain/LoginFailType.cs"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$stateFields = @(
    "packetValid", "accountFound", "exactAccountCase", "maintenanceMode",
    "authority", "passwordAccepted", "versionRequired", "serverVersionConfigured",
    "clientVersionAccepted", "ipPenalized", "staleSessionPersists",
    "accountConnectedRace", "hasSelectedCharacter", "activeBan",
    "registrationSucceeds", "worldListAvailable"
)

$booleanStateFields = @(
    "packetValid", "accountFound", "exactAccountCase", "maintenanceMode",
    "passwordAccepted", "versionRequired", "serverVersionConfigured",
    "clientVersionAccepted", "ipPenalized", "staleSessionPersists",
    "accountConnectedRace", "hasSelectedCharacter", "activeBan",
    "registrationSucceeds", "worldListAvailable"
)

$expectedFields = @(
    "result", "failType", "registersMaster", "disconnectsMaster",
    "sendsWorldList", "disposesPolling"
)

$allowedAuthorities = @("user", "gm", "banned")
$allowedResults = @("silent_drop", "reject", "server_list")
$allowedFailTypes = @(
    "OldClient", "Maintenance", "AlreadyConnected", "AccountOrPasswordWrong",
    "CantConnect", "Banned", "WrongCaps"
)

$requiredCaseIds = @(
    "success_current_client", "maintenance_gm_bypass", "malformed_packet",
    "unknown_account", "wrong_account_casing", "maintenance_user",
    "wrong_credentials", "invalid_server_version_configuration",
    "unsupported_client_version", "ip_penalty", "stale_session_timeout",
    "connected_session_race", "active_account_ban", "banned_authority",
    "master_registration_failure", "world_list_unavailable"
)

function Read-RequiredText {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required login verification file not found: $Path"
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Get-PropertyNames {
    param([object]$Value)
    return @($Value.PSObject.Properties | ForEach-Object { $_.Name })
}

function Get-PropertyValue {
    param([object]$Value, [string]$Name)
    return $Value.PSObject.Properties[$Name].Value
}

function Assert-ExactProperties {
    param([object]$Value, [string[]]$ExpectedNames, [string]$Description)

    if ($null -eq $Value) {
        throw "$Description must not be null."
    }

    $actualNames = Get-PropertyNames $Value
    $unexpected = @($actualNames | Where-Object { $_ -notin $ExpectedNames })
    $missing = @($ExpectedNames | Where-Object { $_ -notin $actualNames })
    if ($unexpected.Count -gt 0) {
        throw "$Description contains forbidden properties: $($unexpected -join ', ')."
    }
    if ($missing.Count -gt 0) {
        throw "$Description is missing properties: $($missing -join ', ')."
    }
}

function Assert-AllowedProperties {
    param([object]$Value, [string[]]$AllowedNames, [string]$Description)

    if ($null -eq $Value) {
        throw "$Description must not be null."
    }

    $unexpected = @(Get-PropertyNames $Value | Where-Object { $_ -notin $AllowedNames })
    if ($unexpected.Count -gt 0) {
        throw "$Description contains forbidden properties: $($unexpected -join ', ')."
    }
}

function Assert-StateTypes {
    param([object]$State, [string]$Description)

    foreach ($field in $booleanStateFields) {
        if ((Get-PropertyValue $State $field) -isnot [bool]) {
            throw "$Description field '$field' must be boolean."
        }
    }

    if ((Get-PropertyValue $State "authority") -notin $allowedAuthorities) {
        throw "$Description authority must be one of: $($allowedAuthorities -join ', ')."
    }
}

function Merge-State {
    param([object]$Defaults, [object]$Override)

    $merged = [ordered]@{}
    foreach ($field in $stateFields) {
        $merged[$field] = Get-PropertyValue $Defaults $field
    }
    foreach ($property in $Override.PSObject.Properties) {
        $merged[$property.Name] = $property.Value
    }
    return [pscustomobject]$merged
}

function New-Outcome {
    param(
        [string]$Result,
        [AllowNull()][object]$FailType,
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
        return (New-Outcome "silent_drop" $null $false $false $false $true)
    }
    if (-not $State.accountFound) {
        return (New-Outcome "reject" "AccountOrPasswordWrong" $false $false $false $true)
    }
    if (-not $State.exactAccountCase) {
        return (New-Outcome "reject" "WrongCaps" $false $false $false $true)
    }
    if ($State.maintenanceMode -and $State.authority -ne "gm") {
        return (New-Outcome "reject" "Maintenance" $false $false $false $true)
    }
    if (-not $State.passwordAccepted) {
        return (New-Outcome "reject" "AccountOrPasswordWrong" $false $false $false $true)
    }
    if ($State.versionRequired -and -not $State.serverVersionConfigured) {
        return (New-Outcome "reject" "CantConnect" $false $false $false $true)
    }
    if ($State.versionRequired -and -not $State.clientVersionAccepted) {
        return (New-Outcome "reject" "OldClient" $false $false $false $true)
    }
    if ($State.ipPenalized) {
        return (New-Outcome "reject" "CantConnect" $false $false $false $true)
    }
    if ($State.staleSessionPersists) {
        return (New-Outcome "reject" "AlreadyConnected" $false $false $false $true)
    }
    if ($State.accountConnectedRace) {
        $cleansMaster = -not $State.hasSelectedCharacter
        return (New-Outcome "reject" "AlreadyConnected" $false $cleansMaster $false $cleansMaster)
    }
    if ($State.activeBan -or $State.authority -eq "banned") {
        return (New-Outcome "reject" "Banned" $false $false $false $true)
    }
    if (-not $State.registrationSucceeds) {
        return (New-Outcome "reject" "CantConnect" $false $false $false $true)
    }
    if (-not $State.worldListAvailable) {
        return (New-Outcome "reject" "CantConnect" $true $true $false $true)
    }

    return (New-Outcome "server_list" $null $true $false $true $true)
}

function Assert-SameValue {
    param(
        [AllowNull()][object]$Actual,
        [AllowNull()][object]$Expected,
        [string]$Description
    )

    if (-not [object]::Equals($Actual, $Expected)) {
        $actualText = if ($null -eq $Actual) { "<null>" } else { [string]$Actual }
        $expectedText = if ($null -eq $Expected) { "<null>" } else { [string]$Expected }
        throw "$Description. Expected '$expectedText', actual '$actualText'."
    }
}

function Assert-Regex {
    param([string]$Content, [string]$Pattern, [string]$Description)

    if (-not [regex]::IsMatch(
            $Content,
            $Pattern,
            [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "Login source contract failed: $Description"
    }
}

function Assert-Ordered {
    param([string]$Content, [string[]]$Needles, [string]$Description)

    $position = 0
    foreach ($needle in $Needles) {
        $next = $Content.IndexOf($needle, $position, [StringComparison]::Ordinal)
        if ($next -lt 0) {
            throw "Login source contract failed: $Description. Missing or out-of-order token: $needle"
        }
        $position = $next + $needle.Length
    }
}

function Get-Section {
    param([string]$Content, [string]$StartMarker, [string]$EndMarker, [string]$Description)

    $start = $Content.IndexOf($StartMarker, [StringComparison]::Ordinal)
    if ($start -lt 0) {
        throw "Login source contract failed: $Description start marker missing."
    }

    $end = $Content.IndexOf($EndMarker, $start + $StartMarker.Length, [StringComparison]::Ordinal)
    if ($end -lt 0) {
        throw "Login source contract failed: $Description end marker missing."
    }

    return $Content.Substring($start, $end - $start)
}

$fixture = (Read-RequiredText $FixturePath) | ConvertFrom-Json
Assert-ExactProperties $fixture @("schemaVersion", "defaults", "cases") "Login fixture root"
if ($fixture.schemaVersion -ne 1) {
    throw "Unsupported login fixture schema version: $($fixture.schemaVersion)."
}

Assert-ExactProperties $fixture.defaults $stateFields "Login fixture defaults"
Assert-StateTypes $fixture.defaults "Login fixture defaults"

$seen = @{}
foreach ($fixtureCase in @($fixture.cases)) {
    Assert-ExactProperties $fixtureCase @("id", "override", "expected") "Login fixture case"
    Assert-AllowedProperties $fixtureCase.override $stateFields "Fixture override '$($fixtureCase.id)'"
    Assert-ExactProperties $fixtureCase.expected $expectedFields "Fixture expectation '$($fixtureCase.id)'"

    if ($fixtureCase.id -notmatch '^[a-z][a-z0-9_]{2,63}$') {
        throw "Fixture ID '$($fixtureCase.id)' is not a sanitized symbolic identifier."
    }
    if ($seen.ContainsKey($fixtureCase.id)) {
        throw "Duplicate fixture ID: $($fixtureCase.id)."
    }
    $seen[$fixtureCase.id] = $true

    $state = Merge-State $fixture.defaults $fixtureCase.override
    Assert-StateTypes $state "Merged state '$($fixtureCase.id)'"

    if ($fixtureCase.expected.result -notin $allowedResults) {
        throw "Fixture '$($fixtureCase.id)' has unsupported result '$($fixtureCase.expected.result)'."
    }
    if ($null -ne $fixtureCase.expected.failType -and
        $fixtureCase.expected.failType -notin $allowedFailTypes) {
        throw "Fixture '$($fixtureCase.id)' has unsupported fail type '$($fixtureCase.expected.failType)'."
    }
    if ($fixtureCase.expected.result -eq "reject" -and $null -eq $fixtureCase.expected.failType) {
        throw "Reject fixture '$($fixtureCase.id)' must declare a fail type."
    }
    if ($fixtureCase.expected.result -ne "reject" -and $null -ne $fixtureCase.expected.failType) {
        throw "Non-reject fixture '$($fixtureCase.id)' must not declare a fail type."
    }

    foreach ($field in @("registersMaster", "disconnectsMaster", "sendsWorldList", "disposesPolling")) {
        if ((Get-PropertyValue $fixtureCase.expected $field) -isnot [bool]) {
            throw "Fixture '$($fixtureCase.id)' expected field '$field' must be boolean."
        }
    }

    $actual = Get-LoginOutcome $state
    foreach ($field in $expectedFields) {
        Assert-SameValue `
            -Actual (Get-PropertyValue $actual $field) `
            -Expected (Get-PropertyValue $fixtureCase.expected $field) `
            -Description "Fixture '$($fixtureCase.id)' failed for '$field'"
    }
}

foreach ($requiredId in $requiredCaseIds) {
    if (-not $seen.ContainsKey($requiredId)) {
        throw "Required sanitized login fixture is missing: $requiredId."
    }
}
if ($seen.Count -ne $requiredCaseIds.Count) {
    throw "Unexpected fixture count. Update the required-case contract intentionally."
}

$handler = Read-RequiredText $LoginHandlerPath
$failTypes = Read-RequiredText $LoginFailTypePath
$legacyEntry = Get-Section $handler "public async Task VerifyLoginAsync(LoginPacket loginPacket)" '[Packet("NoS0576", "NoS0577")]' "legacy Login entry"
$accountLoading = Get-Section $handler "private bool TryLoadAccount(" "private bool ValidateClientVersion(" "account loading"
$versionValidation = Get-Section $handler "private bool ValidateClientVersion(" "private async Task<bool> CheckIsConnectedAsync" "version validation"
$completion = Get-Section $handler "private async Task CompleteLoginAsync(" "private bool TryLoadAccount(" "shared completion"

Assert-Ordered $legacyEntry @(
    "if (loginPacket == null || string.IsNullOrWhiteSpace(loginPacket.Name) ||",
    "if (!TryLoadAccount(username, out AccountDTO loadedAccount))",
    "if (!PasswordHashService.VerifyLoginPayload(",
    "if (!ValidateClientVersion(hasClientVersion, clientVersion))",
    "await CompleteLoginAsync("
) "NoS0575 decision order must remain deterministic"

Assert-Ordered $accountLoading @(
    "loadedAccount = DAOFactory.AccountDAO.LoadByName(username);",
    "if (loadedAccount == null)",
    "if (!string.Equals(loadedAccount.Name, username, StringComparison.Ordinal))",
    "if (ServerConfiguration.MaintenanceMode && loadedAccount.Authority < AuthorityType.GM)"
) "account lookup, casing and maintenance order"

Assert-Ordered $completion @(
    "if (DAOFactory.PenaltyLogDAO.LoadByIp(ipAddress).Any())",
    "if (await CheckIsConnectedAsync(loadedAccount.AccountId).ConfigureAwait(false))",
    "if (CommunicationServiceClient.Instance.IsAccountConnected(loadedAccount.AccountId))",
    "if (penalty != null || loadedAccount.Authority == AuthorityType.Banned)",
    "CommunicationServiceClient.Instance.RegisterAccountLogin(",
    "string serversPacket = BuildServersPacket(",
    "CommunicationServiceClient.Instance.DisconnectAccount(loadedAccount.AccountId);",
    "_session.SendPacket(serversPacket);"
) "shared completion order"

Assert-Regex $accountLoading 'loadedAccount == null.*?Reject\(LoginFailType\.AccountOrPasswordWrong, "Session removed\. Reason: Unknown account"\)' "unknown account mapping"
Assert-Regex $accountLoading '!string\.Equals\(loadedAccount\.Name, username, StringComparison\.Ordinal\).*?Reject\(LoginFailType\.WrongCaps' "account casing mapping"
Assert-Regex $accountLoading 'MaintenanceMode && loadedAccount\.Authority < AuthorityType\.GM.*?Reject\(LoginFailType\.Maintenance' "maintenance mapping"
Assert-Regex $legacyEntry '!PasswordHashService\.VerifyLoginPayload\(.*?Reject\(LoginFailType\.AccountOrPasswordWrong, "Session removed\. Reason: Wrong credentials"\)' "wrong credential mapping"
Assert-Regex $versionValidation '!TryParseVersion\(ServerConfiguration\.GameVersion, out Version requiredVersion\).*?Reject\(LoginFailType\.CantConnect' "invalid server-version mapping"
Assert-Regex $versionValidation '!hasClientVersion \|\| !requiredVersion\.Equals\(clientVersion\).*?Reject\(LoginFailType\.OldClient' "unsupported client mapping"
Assert-Regex $completion 'PenaltyLogDAO\.LoadByIp\(ipAddress\)\.Any\(\).*?Reject\(LoginFailType\.CantConnect' "IP penalty mapping"
Assert-Regex $completion 'CheckIsConnectedAsync\(loadedAccount\.AccountId\).*?Reject\(LoginFailType\.AlreadyConnected' "stale session mapping"
Assert-Regex $completion 'IsAccountConnected\(loadedAccount\.AccountId\).*?LoginFailType\.AlreadyConnected.*?DisconnectAccount\(loadedAccount\.AccountId\).*?DisposeLoginPolling\(\)' "duplicate-session race cleanup"
Assert-Regex $completion 'penalty != null \|\| loadedAccount\.Authority == AuthorityType\.Banned.*?Reject\(LoginFailType\.Banned' "ban mapping"
Assert-Regex $completion 'catch \(Exception ex\).*?Reject\(LoginFailType\.CantConnect, "Session removed\. Reason: Login registration failed"\)' "Master registration failure mapping"
Assert-Regex $handler 'Client has been removed\. Reason: World Server not found.*?LoginFailType\.CantConnect' "missing World mapping"
Assert-Regex $completion 'string\.IsNullOrWhiteSpace\(serversPacket\).*?DisconnectAccount\(loadedAccount\.AccountId\).*?DisposeLoginPolling\(\)' "missing World rollback"
Assert-Regex $completion '_session\.SendPacket\(serversPacket\).*?Server list sent.*?DisposeLoginPolling\(\)' "successful world-list delivery"

Assert-Regex $failTypes 'OldClient\s*=\s*1' "OldClient failc code changed"
Assert-Regex $failTypes 'Maintenance\s*=\s*3' "Maintenance failc code changed"
Assert-Regex $failTypes 'AlreadyConnected\s*=\s*4' "AlreadyConnected failc code changed"
Assert-Regex $failTypes 'AccountOrPasswordWrong\s*=\s*5' "AccountOrPasswordWrong failc code changed"
Assert-Regex $failTypes 'CantConnect\s*=\s*6' "CantConnect failc code changed"
Assert-Regex $failTypes 'Banned\s*=\s*7' "Banned failc code changed"
Assert-Regex $failTypes 'WrongCaps\s*=\s*9' "WrongCaps failc code changed"

Write-Host "Verified $($seen.Count) sanitized login outcome fixtures and production source contracts."
