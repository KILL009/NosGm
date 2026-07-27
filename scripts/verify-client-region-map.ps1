param(
    [string]$FixturePath = "tests/fixtures/client-region-map.json",
    [string]$LanguagePath = "Data/NosGm.Core/Language.cs",
    [string]$ConfigurationPath = "Data/NosGm.Configuration/ServerConfiguration.cs",
    [string]$LoginProgramPath = "Data/NosGm.Program/NosGm.Login/Program.cs",
    [string]$NetworkManagerPath = "Data/NosGm.GameObject/Networking/NetworkManager.cs",
    [string]$ClientSessionPath = "Data/NosGm.GameObject/Networking/ClientSession.cs",
    [string]$LoginHandlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$LoginPacketPath = "Data/NosGm.Packets/Packets/ClientPackets/LoginPacket.cs",
    [string]$AccountInterfacePath = "Data/NosGm.DAL/NosGm.DAL.Interface/IAccountDAO.cs",
    [string]$AccountDaoPath = "Data/NosGm.DAL/NosGm.DAL.DAO/AccountDAO.cs",
    [string]$LocalizationDocPath = "docs/localization.md"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-RequiredText {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required regional-login verification file was not found: $Path"
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Get-PropertyNames {
    param([object]$Value)

    return @($Value.PSObject.Properties | ForEach-Object { $_.Name })
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

function Assert-Contains {
    param(
        [string]$Content,
        [string]$Needle,
        [string]$Description
    )

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Regional-login source contract failed: $Description"
    }
}

function Assert-NotContains {
    param(
        [string]$Content,
        [string]$Needle,
        [string]$Description
    )

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "Regional-login source contract failed: $Description"
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
        throw "Regional-login source contract failed: $Description"
    }
}

$fixtureJson = Read-RequiredText $FixturePath
if ([regex]::IsMatch($fixtureJson, '(?i)"(?:username|password|passwordHash|sessionId|accountId|email|token|ipAddress)"')) {
    throw "Regional-login fixture contains a forbidden sensitive field."
}

$fixture = $fixtureJson | ConvertFrom-Json
Assert-ExactProperties $fixture @("schemaVersion", "baseLoginPort", "regions") "Regional-login fixture root"
Assert-SameValue -Actual ([int]$fixture.schemaVersion) -Expected 1 -Description "Regional-login fixture schema version"
Assert-SameValue -Actual ([int]$fixture.baseLoginPort) -Expected 4000 -Description "Regional-login base port"

$expectedRegions = @(
    [pscustomobject][ordered]@{ code = "en"; regionType = 0; loginPort = 4000 },
    [pscustomobject][ordered]@{ code = "de"; regionType = 1; loginPort = 4001 },
    [pscustomobject][ordered]@{ code = "fr"; regionType = 2; loginPort = 4002 },
    [pscustomobject][ordered]@{ code = "it"; regionType = 3; loginPort = 4003 },
    [pscustomobject][ordered]@{ code = "pl"; regionType = 4; loginPort = 4004 },
    [pscustomobject][ordered]@{ code = "es"; regionType = 5; loginPort = 4005 },
    [pscustomobject][ordered]@{ code = "cs"; regionType = 6; loginPort = 4006 },
    [pscustomobject][ordered]@{ code = "ru"; regionType = 7; loginPort = 4007 },
    [pscustomobject][ordered]@{ code = "ja"; regionType = 8; loginPort = 4008 },
    [pscustomobject][ordered]@{ code = "zh"; regionType = 9; loginPort = 4009 }
)

$actualRegions = @($fixture.regions)
Assert-SameValue -Actual $actualRegions.Count -Expected $expectedRegions.Count -Description "Regional-login mapping count"

$seenCodes = @{}
$seenRegionTypes = @{}
$seenPorts = @{}
for ($index = 0; $index -lt $expectedRegions.Count; $index++) {
    $actual = $actualRegions[$index]
    $expected = $expectedRegions[$index]
    Assert-ExactProperties $actual @("code", "regionType", "loginPort") "Regional-login mapping at index $index"

    $code = [string]$actual.code
    $regionType = [int]$actual.regionType
    $loginPort = [int]$actual.loginPort

    if ($code -notmatch '^[a-z]{2}$') {
        throw "Regional-login culture '$code' is not canonical."
    }
    if ($seenCodes.ContainsKey($code)) {
        throw "Duplicate regional-login culture: $code."
    }
    if ($seenRegionTypes.ContainsKey($regionType)) {
        throw "Duplicate regional-login RegionType: $regionType."
    }
    if ($seenPorts.ContainsKey($loginPort)) {
        throw "Duplicate regional-login port: $loginPort."
    }

    $seenCodes[$code] = $true
    $seenRegionTypes[$regionType] = $true
    $seenPorts[$loginPort] = $true

    Assert-SameValue -Actual $code -Expected ([string]$expected.code) -Description "Regional-login culture at index $index"
    Assert-SameValue -Actual $regionType -Expected ([int]$expected.regionType) -Description "RegionType for '$code'"
    Assert-SameValue -Actual $loginPort -Expected ([int]$expected.loginPort) -Description "Login port for '$code'"
    Assert-SameValue -Actual $loginPort -Expected ([int]$fixture.baseLoginPort + $regionType) -Description "Port suffix for '$code'"
}

$languageSource = Read-RequiredText $LanguagePath
$configurationSource = Read-RequiredText $ConfigurationPath
$loginProgramSource = Read-RequiredText $LoginProgramPath
$networkManagerSource = Read-RequiredText $NetworkManagerPath
$clientSessionSource = Read-RequiredText $ClientSessionPath
$loginHandlerSource = Read-RequiredText $LoginHandlerPath
$loginPacketSource = Read-RequiredText $LoginPacketPath
$accountInterfaceSource = Read-RequiredText $AccountInterfacePath
$accountDaoSource = Read-RequiredText $AccountDaoPath
$localizationDoc = Read-RequiredText $LocalizationDocPath

Assert-Contains $languageSource "public static class ClientRegionMap" "ClientRegionMap must be centralized in NosGm.Core"
Assert-Regex $languageSource 'BaseLoginPort\s*=\s*4000' "regional login base port must remain 4000"
Assert-Regex $languageSource '"en"\s*,\s*"de"\s*,\s*"fr"\s*,\s*"it"\s*,\s*"pl"\s*,\s*"es"\s*,\s*"cs"\s*,\s*"ru"\s*,\s*"ja"\s*,\s*"zh"' "ClientRegionMap culture order must match the official port suffix"
Assert-Contains $configurationSource "public static bool StartAllRegionalLoginPorts = true;" "all ten regional Login listeners must be enabled by default"
Assert-Contains $loginProgramSource "Enumerable.Range(ClientRegionMap.BaseLoginPort, ClientRegionMap.RegionCount)" "Login must start all ten regional ports in one process"
Assert-Contains $loginProgramSource "ClientRegionMap.TryResolveLoginPort" "Login startup must reject unsupported regional ports"
Assert-Regex $loginProgramSource 'portArgIndex.*?args\.Length > portArgIndex \+ 1' "--port parsing must bounds-check its value"
Assert-Contains $networkManagerSource "new ClientSession(client, _listeningPort)" "the accepted local port must be attached to ClientSession"
Assert-NotContains $networkManagerSource "if (port == 4000)" "Login listener diagnostics must support every regional port"
Assert-Regex $clientSessionSource 'public ClientSession\(INetworkClient client, int listeningPort = 0\)' "ClientSession must accept the local listening port"
Assert-Contains $clientSessionSource "public int ListeningPort { get; }" "ClientSession must expose its trusted local listening port"
Assert-Regex $loginPacketSource '\[PacketIndex\(5\)\]\s*public byte RegionType' "NoS0575 RegionType must remain available only for compatibility diagnostics"
Assert-Regex $loginHandlerSource 'TryResolveLoginPort\(\s*_session\.ListeningPort\s*,\s*out byte resolvedRegionType\s*,\s*out string clientCulture\s*\)' "Login must resolve region and culture from the accepted local port"
Assert-Regex $loginHandlerSource 'BuildServersPacket\s*\(\s*username\s*,\s*resolvedRegionType\s*,\s*newSessionId' "NsTeST must use the port-derived RegionType"
Assert-NotContains $loginHandlerSource "BuildServersPacket(`r`n                username,`r`n                loginPacket.RegionType" "Login must not route worlds with the untrusted packet RegionType"
Assert-Contains $loginHandlerSource "DAOFactory.AccountDAO.TryUpdateLanguage" "Login must synchronize Account.Language from the regional port"
Assert-Contains $accountInterfaceSource "bool TryUpdateLanguage(long accountId, string language);" "AccountDAO contract must expose a targeted language update"
Assert-Regex $accountDaoSource 'public bool TryUpdateLanguage\(long accountId, string language\).*?entity\.Language = language;.*?SaveChanges' "AccountDAO must update only the account language field"
Assert-Contains $localizationDoc "The Login listening port is the source of truth" "localization documentation must explain trusted port routing"

foreach ($mapping in $expectedRegions) {
    $tableToken = "| ``$($mapping.code)`` | ``$($mapping.regionType)`` | ``$($mapping.loginPort)`` |"
    Assert-Contains $localizationDoc $tableToken "localization documentation must include $($mapping.code) = $($mapping.regionType) = $($mapping.loginPort)"
}

Write-Host "Verified regional Login routing: en=4000, de=4001, fr=4002, it=4003, pl=4004, es=4005, cs=4006, ru=4007, ja=4008, zh=4009."
