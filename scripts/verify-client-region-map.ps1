param(
    [string]$FixturePath = "tests/fixtures/client-region-map.json",
    [string]$LanguagePath = "Data/NosGm.Core/Language.cs",
    [string]$ConfigurationPath = "Data/NosGm.Configuration/ServerConfiguration.cs",
    [string]$LoginProgramPath = "Data/NosGm.Program/NosGm.Login/Program.cs",
    [string]$NetworkManagerPath = "Data/NosGm.GameObject/Networking/NetworkManager.cs",
    [string]$ClientSessionPath = "Data/NosGm.GameObject/Networking/ClientSession.cs",
    [string]$LoginHandlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$EntryPointHandlerPath = "Data/NosGm.Handler/PacketHandler/CharScreen/EntryPointPacketHandler.cs",
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

function Assert-SameValue {
    param([AllowNull()][object]$Actual, [AllowNull()][object]$Expected, [string]$Description)

    if (-not [object]::Equals($Actual, $Expected)) {
        $actualText = if ($null -eq $Actual) { "<null>" } else { [string]$Actual }
        $expectedText = if ($null -eq $Expected) { "<null>" } else { [string]$Expected }
        throw "$Description. Expected '$expectedText', actual '$actualText'."
    }
}

function Assert-Contains {
    param([string]$Content, [string]$Needle, [string]$Description)

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Regional-login source contract failed: $Description"
    }
}

function Assert-NotContains {
    param([string]$Content, [string]$Needle, [string]$Description)

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "Regional-login source contract failed: $Description"
    }
}

function Assert-Regex {
    param([string]$Content, [string]$Pattern, [string]$Description)

    if (-not [regex]::IsMatch(
            $Content,
            $Pattern,
            [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "Regional-login source contract failed: $Description"
    }
}

function Assert-NotRegex {
    param([string]$Content, [string]$Pattern, [string]$Description)

    if ([regex]::IsMatch(
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
Assert-SameValue -Actual ([int]$fixture.schemaVersion) -Expected 2 -Description "Regional-login fixture schema version"
Assert-SameValue -Actual ([int]$fixture.baseLoginPort) -Expected 4000 -Description "Regional-login base port"

$expectedRegions = @(
    [pscustomobject][ordered]@{ serverCulture = "en"; regionType = 0; loginPort = 4000; protocolPrefix = "EN"; clientFileSuffix = "UK" },
    [pscustomobject][ordered]@{ serverCulture = "de"; regionType = 1; loginPort = 4001; protocolPrefix = "DE"; clientFileSuffix = "DE" },
    [pscustomobject][ordered]@{ serverCulture = "fr"; regionType = 2; loginPort = 4002; protocolPrefix = "FR"; clientFileSuffix = "FR" },
    [pscustomobject][ordered]@{ serverCulture = "it"; regionType = 3; loginPort = 4003; protocolPrefix = "IT"; clientFileSuffix = "IT" },
    [pscustomobject][ordered]@{ serverCulture = "pl"; regionType = 4; loginPort = 4004; protocolPrefix = "PL"; clientFileSuffix = "PL" },
    [pscustomobject][ordered]@{ serverCulture = "es"; regionType = 5; loginPort = 4005; protocolPrefix = "ES"; clientFileSuffix = "ES" },
    [pscustomobject][ordered]@{ serverCulture = "cs"; regionType = 6; loginPort = 4006; protocolPrefix = "CZ"; clientFileSuffix = "CZ" },
    [pscustomobject][ordered]@{ serverCulture = "ru"; regionType = 7; loginPort = 4007; protocolPrefix = "RU"; clientFileSuffix = "RU" },
    [pscustomobject][ordered]@{ serverCulture = "ja"; regionType = 8; loginPort = 4008; protocolPrefix = "JP"; clientFileSuffix = "JP" },
    [pscustomobject][ordered]@{ serverCulture = "zh"; regionType = 9; loginPort = 4009; protocolPrefix = "CN"; clientFileSuffix = "CN" }
)

$actualRegions = @($fixture.regions)
Assert-SameValue -Actual $actualRegions.Count -Expected $expectedRegions.Count -Description "Regional-login mapping count"

$seenCultures = @{}
$seenRegionTypes = @{}
$seenPorts = @{}
$seenPrefixes = @{}
$seenFileSuffixes = @{}
for ($index = 0; $index -lt $expectedRegions.Count; $index++) {
    $actual = $actualRegions[$index]
    $expected = $expectedRegions[$index]
    Assert-ExactProperties $actual @("serverCulture", "regionType", "loginPort", "protocolPrefix", "clientFileSuffix") "Regional-login mapping at index $index"

    $serverCulture = [string]$actual.serverCulture
    $regionType = [int]$actual.regionType
    $loginPort = [int]$actual.loginPort
    $protocolPrefix = [string]$actual.protocolPrefix
    $clientFileSuffix = [string]$actual.clientFileSuffix

    if ($serverCulture -notmatch '^[a-z]{2}$') {
        throw "Regional-login culture '$serverCulture' is not canonical."
    }
    if ($protocolPrefix -notmatch '^[A-Z]{2}$' -or $clientFileSuffix -notmatch '^[A-Z]{2}$') {
        throw "Regional-login profile '$serverCulture' contains an invalid two-letter client code."
    }
    if ($seenCultures.ContainsKey($serverCulture) -or
        $seenRegionTypes.ContainsKey($regionType) -or
        $seenPorts.ContainsKey($loginPort) -or
        $seenPrefixes.ContainsKey($protocolPrefix) -or
        $seenFileSuffixes.ContainsKey($clientFileSuffix)) {
        throw "Regional-login profile '$serverCulture' duplicates a unique mapping field."
    }

    $seenCultures[$serverCulture] = $true
    $seenRegionTypes[$regionType] = $true
    $seenPorts[$loginPort] = $true
    $seenPrefixes[$protocolPrefix] = $true
    $seenFileSuffixes[$clientFileSuffix] = $true

    foreach ($property in @("serverCulture", "regionType", "loginPort", "protocolPrefix", "clientFileSuffix")) {
        Assert-SameValue -Actual $actual.$property -Expected $expected.$property -Description "Regional-login $property at index $index"
    }
    Assert-SameValue -Actual $loginPort -Expected ([int]$fixture.baseLoginPort + $regionType) -Description "Login port suffix for '$serverCulture'"
}

$languageSource = Read-RequiredText $LanguagePath
$configurationSource = Read-RequiredText $ConfigurationPath
$loginProgramSource = Read-RequiredText $LoginProgramPath
$networkManagerSource = Read-RequiredText $NetworkManagerPath
$clientSessionSource = Read-RequiredText $ClientSessionPath
$loginHandlerSource = Read-RequiredText $LoginHandlerPath
$entryPointHandlerSource = Read-RequiredText $EntryPointHandlerPath
$loginPacketSource = Read-RequiredText $LoginPacketPath
$accountInterfaceSource = Read-RequiredText $AccountInterfacePath
$accountDaoSource = Read-RequiredText $AccountDaoPath
$localizationDoc = Read-RequiredText $LocalizationDocPath

Assert-Contains $languageSource "public sealed class ClientLanguageProfile" "client language metadata must use explicit profiles"
Assert-Contains $languageSource "public static class ClientRegionMap" "ClientRegionMap must be centralized in NosGm.Core"
Assert-Regex $languageSource 'BaseLoginPort\s*=\s*4000' "regional Login base port must remain 4000"
Assert-Regex $languageSource 'new ClientLanguageProfile\(5,\s*4005,\s*"ES",\s*"ES",\s*"es"\)' "Spanish profile must map RegionType 5, ES protocol prefix and NSlangData_ES"
Assert-Regex $languageSource 'new ClientLanguageProfile\(8,\s*4008,\s*"JP",\s*"JP",\s*"ja"\)' "Japanese must map to RegionType 8 and port 4008"
Assert-Regex $languageSource 'new ClientLanguageProfile\(9,\s*4009,\s*"CN",\s*"CN",\s*"zh"\)' "Chinese must map to RegionType 9 and port 4009"
Assert-NotContains $languageSource 'new ClientLanguageProfile(8, 4008, "TR"' "The ten-language map must not replace Japanese with Turkish"
Assert-Contains $configurationSource "public static bool StartAllRegionalLoginPorts = true;" "all regional Login listeners must be enabled by default"
Assert-Regex $loginProgramSource 'Enumerable\.Range\s*\(\s*ClientRegionMap\.BaseLoginPort\s*,\s*ClientRegionMap\.RegionCount\s*\)' "Login must start every regional port in one process"
Assert-Contains $loginProgramSource "ClientRegionMap.TryResolveLoginPort" "Login startup must reject unsupported regional ports"
Assert-Regex $loginProgramSource 'args\.Length\s*<=\s*portArgIndex\s*\+\s*1\s*\|\|' "--port parsing must reject a missing value before reading it"
Assert-Contains $networkManagerSource "new ClientSession(client, _listeningPort)" "the accepted local port must be attached to ClientSession"
Assert-NotContains $networkManagerSource "if (port == 4000)" "Login listener diagnostics must support every regional port"
Assert-Regex $clientSessionSource 'public ClientSession\(INetworkClient client, int listeningPort = 0\)' "ClientSession must accept the local listening port"
Assert-Contains $clientSessionSource "public int ListeningPort { get; }" "ClientSession must expose its trusted local listening port"
Assert-Regex $loginPacketSource '\[PacketIndex\(5\)\]\s*public byte RegionType' "NoS0575 RegionType must remain available for compatibility diagnostics"
Assert-Regex $loginHandlerSource 'TryResolveLoginPort\(\s*_session\.ListeningPort\s*,\s*out regionType\s*,\s*out culture\s*\)' "Login must resolve region and culture from the accepted local port"
Assert-Regex $loginHandlerSource 'BuildServersPacket\s*\(\s*protocolUsername\s*,\s*regionType\s*,\s*newSessionId' "NsTeST must preserve the supplied protocol username and use the resolved RegionType"
Assert-NotRegex $loginHandlerSource 'BuildServersPacket\s*\([^;]*loginPacket\.RegionType' "Login must not route worlds with the packet RegionType"
Assert-Contains $loginHandlerSource "LoadAccountByLoginName" "Login must support an optional regional account alias"
Assert-Contains $loginHandlerSource "ClientRegionMap.IsProtocolUsernameForAccount" "regional aliases must retain exact casing checks"
Assert-Contains $entryPointHandlerSource "LoadAccountByProtocolName" "World entry must resolve the same optional regional alias"
Assert-Contains $entryPointHandlerSource "IsLoginPermitted(" "regional aliases must still require Master AccountId and SessionId authorization"
Assert-Contains $loginHandlerSource "DAOFactory.AccountDAO.TryUpdateLanguage" "Login must synchronize Account.Language from the regional port"
Assert-Contains $accountInterfaceSource "bool TryUpdateLanguage(long accountId, string language);" "AccountDAO contract must expose a targeted language update"
Assert-Regex $accountDaoSource 'public bool TryUpdateLanguage\(long accountId, string language\).*?entity\.Language = language;.*?SaveChanges' "AccountDAO must update only the account language field"
Assert-Contains $localizationDoc "The Login listening port is the source of truth" "localization documentation must explain trusted port routing"
Assert-Contains $localizationDoc "World endpoint ports are independent" "documentation must distinguish Login language ports from World channel ports"

foreach ($mapping in $expectedRegions) {
    $tableToken = "| ``$($mapping.protocolPrefix)`` | ``$($mapping.regionType)`` | ``$($mapping.loginPort)`` | ``$($mapping.clientFileSuffix)`` | ``$($mapping.serverCulture)`` |"
    Assert-Contains $localizationDoc $tableToken "localization documentation must include the complete profile for $($mapping.protocolPrefix)"
}

Write-Host "Verified ten-language regional Login routing: EN/UK=4000 through CN=4009, optional protocol account prefixes and independent World channel ports."
