param(
    [string]$FixturePath = "tests/fixtures/world-channel-lists.json",
    [string]$MasterServicePath = "Data/NosGm.Program/NosGm.Master.Server/CommunicationService.cs",
    [string]$LoginHandlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$LoginPacketPath = "Data/NosGm.Packets/Packets/ClientPackets/LoginPacket.cs",
    [string]$LanguagePath = "Data/NosGm.Core/Language.cs",
    [string]$LocalizationDocPath = "docs/localization.md",
    [string]$WorldResourceDirectory = "Data/NosGm.Program/NosGm.World/Resource"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$rootFields = @(
    "schemaVersion",
    "protocol",
    "supportedCultures",
    "regionTypes",
    "characterPrefixes",
    "cases"
)
$protocolFields = @("header", "paddingPairs", "sentinel")
$prefixFields = @("characterCount", "value")
$caseFields = @("id", "regionType", "characterCount", "worlds", "expected")
$worldFields = @("group", "channelId", "host", "port", "accountLimit", "connectedAccounts")
$expectedFields = @("packetAvailable", "worldEntries")
$requiredCaseIds = @(
    "single_channel_region_0",
    "four_characters_region_5",
    "sorted_groups_and_channels",
    "hidden_act4_does_not_shift_groups",
    "only_hidden_act4",
    "no_registered_worlds"
)

function Read-RequiredText {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required world/channel verification file was not found: $Path"
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Get-PropertyNames {
    param([object]$Value)

    return @($Value.PSObject.Properties | ForEach-Object { $_.Name })
}

function Get-PropertyValue {
    param(
        [object]$Value,
        [string]$Name
    )

    return $Value.PSObject.Properties[$Name].Value
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

function Assert-StringArray {
    param(
        [object[]]$Actual,
        [object[]]$Expected,
        [string]$Description
    )

    $actualValues = @($Actual | ForEach-Object { [string]$_ })
    $expectedValues = @($Expected | ForEach-Object { [string]$_ })

    if ($actualValues.Count -ne $expectedValues.Count) {
        throw "$Description count changed. Expected $($expectedValues.Count), actual $($actualValues.Count)."
    }

    for ($index = 0; $index -lt $expectedValues.Count; $index++) {
        if (-not [string]::Equals($actualValues[$index], $expectedValues[$index], [StringComparison]::Ordinal)) {
            throw "$Description differs at index $index. Expected '$($expectedValues[$index])', actual '$($actualValues[$index])'."
        }
    }
}

function Assert-Contains {
    param(
        [string]$Content,
        [string]$Needle,
        [string]$Description
    )

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "World/channel source contract failed: $Description"
    }
}

function Assert-NotContains {
    param(
        [string]$Content,
        [string]$Needle,
        [string]$Description
    )

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "World/channel source contract failed: $Description"
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
        throw "World/channel source contract failed: $Description"
    }
}

function Get-CharacterPrefix {
    param(
        [object[]]$Prefixes,
        [int]$CharacterCount
    )

    $match = @($Prefixes | Where-Object { [int]$_.characterCount -eq $CharacterCount })
    if ($match.Count -ne 1) {
        throw "Character prefix fixture must contain exactly one entry for count $CharacterCount."
    }

    return [string]$match[0].value
}

function New-WorldListModel {
    param(
        [object]$Protocol,
        [string]$CharacterPrefix,
        [int]$RegionType,
        [object[]]$Worlds
    )

    $visibleWorlds = @(
        $Worlds |
            Where-Object { [int]$_.channelId -ne 51 } |
            Sort-Object @{ Expression = { [string]$_.group }; Ascending = $true },
                        @{ Expression = { [int]$_.channelId }; Ascending = $true }
    )

    if ($visibleWorlds.Count -eq 0) {
        return [pscustomobject][ordered]@{
            packet = $null
            entries = @()
        }
    }

    $tokens = New-Object System.Collections.Generic.List[string]
    $tokens.Add([string]$Protocol.header)
    $tokens.Add($RegionType.ToString([Globalization.CultureInfo]::InvariantCulture))
    $tokens.Add("<USERNAME>")

    foreach ($token in @($CharacterPrefix.Split(' ', [StringSplitOptions]::RemoveEmptyEntries))) {
        $tokens.Add($token)
    }

    for ($index = 0; $index -lt [int]$Protocol.paddingPairs; $index++) {
        $tokens.Add("-99")
        $tokens.Add("0")
    }

    $tokens.Add("<SESSION>")

    $entries = New-Object System.Collections.Generic.List[string]
    $lastGroup = $null
    $worldCount = 0

    foreach ($world in $visibleWorlds) {
        if ([int]$world.accountLimit -le 0) {
            throw "World fixture '$($world.host)' must use a positive account limit."
        }

        if (-not [string]::Equals($lastGroup, [string]$world.group, [StringComparison]::Ordinal)) {
            $worldCount++
            $lastGroup = [string]$world.group
        }

        $loadRatio = [double]$world.connectedAccounts / [double]$world.accountLimit
        $channelColor = [int][Math]::Round($loadRatio * 20) + 1
        $entry = "{0}:{1}:{2}:{3}.{4}.{5}" -f @(
            [string]$world.host,
            [int]$world.port,
            $channelColor,
            $worldCount,
            [int]$world.channelId,
            [string]$world.group
        )
        $entries.Add($entry)
        $tokens.Add($entry)
    }

    $tokens.Add([string]$Protocol.sentinel)

    return [pscustomobject][ordered]@{
        packet = [string]::Join(" ", $tokens)
        entries = @($entries)
    }
}

$fixtureJson = Read-RequiredText $FixturePath

$forbiddenTerms = @(
    '"username"',
    '"password"',
    '"passwordHash"',
    '"sessionId"',
    '"accountId"',
    '"email"',
    '"token"'
)
foreach ($forbiddenTerm in $forbiddenTerms) {
    if ($fixtureJson.IndexOf($forbiddenTerm, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "World/channel fixtures contain a forbidden sensitive field: $forbiddenTerm"
    }
}
if ([regex]::IsMatch($fixtureJson, '(?<![0-9])(?:[0-9]{1,3}\.){3}[0-9]{1,3}(?![0-9])')) {
    throw "World/channel fixtures must use symbolic hosts instead of IP addresses."
}

$fixture = $fixtureJson | ConvertFrom-Json
Assert-ExactProperties $fixture $rootFields "World/channel fixture root"
Assert-ExactProperties $fixture.protocol $protocolFields "World/channel protocol"

Assert-SameValue -Actual ([int]$fixture.schemaVersion) -Expected 1 -Description "Fixture schema version"
Assert-SameValue -Actual ([string]$fixture.protocol.header) -Expected "NsTeST" -Description "World-list header"
Assert-SameValue -Actual ([int]$fixture.protocol.paddingPairs) -Expected 56 -Description "NsTeST padding pair count"
Assert-SameValue -Actual ([string]$fixture.protocol.sentinel) -Expected "-1:-1:-1:10000.10000.1" -Description "World-list sentinel"

$expectedCultures = @("en", "es", "de", "fr", "it", "pl", "cs", "ru", "ja", "zh")
Assert-StringArray -Actual @($fixture.supportedCultures) -Expected $expectedCultures -Description "Supported culture fixture"

$expectedRegions = @(0, 1, 2, 3, 4, 5, 6, 7, 8, 9)
$actualRegions = @($fixture.regionTypes | ForEach-Object { [int]$_ })
Assert-StringArray -Actual $actualRegions -Expected $expectedRegions -Description "Protocol region byte fixture"

$seenPrefixCounts = @{}
foreach ($prefix in @($fixture.characterPrefixes)) {
    Assert-ExactProperties $prefix $prefixFields "Character prefix fixture"
    $characterCount = [int]$prefix.characterCount
    if ($characterCount -lt 0 -or $characterCount -gt 4) {
        throw "Character prefix count must be between 0 and 4."
    }
    if ($seenPrefixCounts.ContainsKey($characterCount)) {
        throw "Duplicate character prefix count: $characterCount."
    }
    $seenPrefixCounts[$characterCount] = $true

    $prefixTokens = @(([string]$prefix.value).Split(' ', [StringSplitOptions]::RemoveEmptyEntries))
    if ($prefixTokens.Count -ne 8) {
        throw "Character prefix $characterCount must contain exactly four value pairs."
    }
}
for ($characterCount = 0; $characterCount -le 4; $characterCount++) {
    if (-not $seenPrefixCounts.ContainsKey($characterCount)) {
        throw "Missing character prefix fixture for count $characterCount."
    }
}

$seenCaseIds = @{}
$positiveCase = $null
foreach ($fixtureCase in @($fixture.cases)) {
    Assert-ExactProperties $fixtureCase $caseFields "World/channel fixture case"
    Assert-ExactProperties $fixtureCase.expected $expectedFields "World/channel expected result"

    if ($fixtureCase.id -notmatch '^[a-z][a-z0-9_]{2,63}$') {
        throw "Fixture case ID '$($fixtureCase.id)' is not a sanitized symbolic identifier."
    }
    if ($seenCaseIds.ContainsKey($fixtureCase.id)) {
        throw "Duplicate world/channel fixture ID: $($fixtureCase.id)."
    }
    $seenCaseIds[$fixtureCase.id] = $true

    $regionType = [int]$fixtureCase.regionType
    if ($regionType -notin $expectedRegions) {
        throw "Fixture '$($fixtureCase.id)' uses unsupported protocol region byte $regionType."
    }

    $characterCount = [int]$fixtureCase.characterCount
    $characterPrefix = Get-CharacterPrefix @($fixture.characterPrefixes) $characterCount

    foreach ($world in @($fixtureCase.worlds)) {
        Assert-ExactProperties $world $worldFields "World fixture '$($fixtureCase.id)'"
        if ([string]$world.host -notmatch '^HOST_[A-Z0-9_]{1,24}$') {
            throw "World fixture '$($fixtureCase.id)' must use a symbolic HOST_* endpoint."
        }
        if ([string]$world.group -notmatch '^[A-Za-z][A-Za-z0-9_-]{0,31}$') {
            throw "World fixture '$($fixtureCase.id)' contains an invalid symbolic group."
        }
        if ([int]$world.channelId -lt 1 -or [int]$world.channelId -gt 51) {
            throw "World fixture '$($fixtureCase.id)' contains an invalid channel ID."
        }
        if ([int]$world.port -lt 1 -or [int]$world.port -gt 65535) {
            throw "World fixture '$($fixtureCase.id)' contains an invalid port."
        }
        if ([int]$world.connectedAccounts -lt 0) {
            throw "World fixture '$($fixtureCase.id)' contains a negative connected-account count."
        }
    }

    if ((Get-PropertyValue $fixtureCase.expected "packetAvailable") -isnot [bool]) {
        throw "Fixture '$($fixtureCase.id)' packetAvailable must be boolean."
    }

    $model = New-WorldListModel $fixture.protocol $characterPrefix $regionType @($fixtureCase.worlds)
    $packetAvailable = $null -ne $model.packet
    Assert-SameValue -Actual $packetAvailable -Expected ([bool]$fixtureCase.expected.packetAvailable) -Description "Fixture '$($fixtureCase.id)' packet availability"
    Assert-StringArray -Actual @($model.entries) -Expected @($fixtureCase.expected.worldEntries) -Description "Fixture '$($fixtureCase.id)' world entries"

    if ($packetAvailable) {
        if ($null -eq $positiveCase) {
            $positiveCase = $fixtureCase
        }

        $packetTokens = @(([string]$model.packet).Split(' ', [StringSplitOptions]::RemoveEmptyEntries))
        $expectedPreambleTokenCount = 3 + 8 + ([int]$fixture.protocol.paddingPairs * 2) + 1
        $expectedTokenCount = $expectedPreambleTokenCount + @($model.entries).Count + 1
        Assert-SameValue -Actual $packetTokens.Count -Expected $expectedTokenCount -Description "Fixture '$($fixtureCase.id)' packet token count"
        Assert-SameValue -Actual $packetTokens[0] -Expected ([string]$fixture.protocol.header) -Description "Fixture '$($fixtureCase.id)' header"
        Assert-SameValue -Actual $packetTokens[1] -Expected ([string]$regionType) -Description "Fixture '$($fixtureCase.id)' region byte passthrough"
        Assert-SameValue -Actual $packetTokens[2] -Expected "<USERNAME>" -Description "Fixture '$($fixtureCase.id)' username placeholder"

        $prefixTokens = @($characterPrefix.Split(' ', [StringSplitOptions]::RemoveEmptyEntries))
        Assert-StringArray -Actual @($packetTokens[3..10]) -Expected $prefixTokens -Description "Fixture '$($fixtureCase.id)' character prefix"

        $paddingStart = 11
        for ($paddingIndex = 0; $paddingIndex -lt [int]$fixture.protocol.paddingPairs; $paddingIndex++) {
            Assert-SameValue -Actual $packetTokens[$paddingStart + ($paddingIndex * 2)] -Expected "-99" -Description "Fixture '$($fixtureCase.id)' padding marker $paddingIndex"
            Assert-SameValue -Actual $packetTokens[$paddingStart + ($paddingIndex * 2) + 1] -Expected "0" -Description "Fixture '$($fixtureCase.id)' padding value $paddingIndex"
        }

        Assert-SameValue -Actual $packetTokens[$expectedPreambleTokenCount - 1] -Expected "<SESSION>" -Description "Fixture '$($fixtureCase.id)' session placeholder position"
        Assert-SameValue -Actual $packetTokens[$packetTokens.Count - 1] -Expected ([string]$fixture.protocol.sentinel) -Description "Fixture '$($fixtureCase.id)' sentinel"
    }
}

foreach ($requiredCaseId in $requiredCaseIds) {
    if (-not $seenCaseIds.ContainsKey($requiredCaseId)) {
        throw "Required world/channel fixture is missing: $requiredCaseId."
    }
}
if ($seenCaseIds.Count -ne $requiredCaseIds.Count) {
    throw "Unexpected world/channel fixture count. Update the required-case contract intentionally."
}
if ($null -eq $positiveCase) {
    throw "At least one fixture must produce a visible world list."
}

$positivePrefix = Get-CharacterPrefix @($fixture.characterPrefixes) ([int]$positiveCase.characterCount)
foreach ($regionType in $expectedRegions) {
    $regionModel = New-WorldListModel $fixture.protocol $positivePrefix $regionType @($positiveCase.worlds)
    $regionTokens = @(([string]$regionModel.packet).Split(' ', [StringSplitOptions]::RemoveEmptyEntries))
    Assert-SameValue -Actual $regionTokens[1] -Expected ([string]$regionType) -Description "Protocol region byte $regionType must pass through unchanged"
}

foreach ($characterCount in 0..4) {
    $prefix = Get-CharacterPrefix @($fixture.characterPrefixes) $characterCount
    $prefixModel = New-WorldListModel $fixture.protocol $prefix 0 @($positiveCase.worlds)
    $prefixTokens = @(([string]$prefixModel.packet).Split(' ', [StringSplitOptions]::RemoveEmptyEntries))
    Assert-StringArray -Actual @($prefixTokens[3..10]) -Expected @($prefix.Split(' ', [StringSplitOptions]::RemoveEmptyEntries)) -Description "Character-count prefix $characterCount"
}

$masterSource = Read-RequiredText $MasterServicePath
$loginHandlerSource = Read-RequiredText $LoginHandlerPath
$loginPacketSource = Read-RequiredText $LoginPacketPath
$languageSource = Read-RequiredText $LanguagePath
$localizationDoc = Read-RequiredText $LocalizationDocPath

Assert-Regex $loginPacketSource '\[PacketIndex\(5\)\]\s*public byte RegionType' "RegionType must remain byte field 5 of NoS0575 for compatibility diagnostics"
Assert-Regex $loginHandlerSource 'TryResolveLoginPort\s*\(\s*_session\.ListeningPort\s*,\s*out byte resolvedRegionType\s*,\s*out string clientCulture\s*\)' "Login must derive RegionType and culture from the accepted local port"
Assert-Regex $loginHandlerSource 'BuildServersPacket\s*\(\s*username\s*,\s*resolvedRegionType\s*,\s*newSessionId' "Login must pass the port-derived RegionType to Master"
Assert-NotContains $loginHandlerSource 'BuildServersPacket(`r`n                username,`r`n                loginPacket.RegionType' "Login must not pass the untrusted packet RegionType to Master"
Assert-Regex $masterSource 'private const int NsTeSTPadding\s*=\s*56;' "NsTeST padding must remain 56 pairs"
Assert-Contains $masterSource '$"NsTeST {regionType} {username}' "Master must emit the supplied protocol region byte"
Assert-Regex $masterSource 'visibleWorlds\s*=.*?Where\(w => w\.ChannelId != 51\).*?OrderBy\(w => w\.WorldGroup\).*?ThenBy\(w => w\.ChannelId\).*?ToList\(\);' "visible worlds must exclude channel 51 and sort by group then channel"
Assert-Regex $masterSource 'if \(visibleWorlds\.Count == 0\)\s*\{\s*return null;\s*\}' "Master must reject an empty visible world list"
Assert-Contains $masterSource 'foreach (var world in visibleWorlds)' "Master must build the packet from the deterministic visible-world snapshot"
Assert-Contains $masterSource 'a.ConnectedWorld?.Id == world.Id' "Channel load must count sessions for the exact World instead of every group sharing the same ChannelId"
Assert-Contains $masterSource 'channelPacket += "-1:-1:-1:10000.10000.1";' "Master must retain the terminal world-list sentinel"
Assert-NotContains $masterSource 'Logger.Info(channelPacket);' "Master must not log username and SessionId through the raw NsTeST packet"
Assert-Contains $masterSource 'World list generated | RegionType=' "Master must emit only bounded world-list diagnostics"

$cultureMatch = [regex]::Match(
    $languageSource,
    'private static readonly string\[\] SupportedCultures\s*=\s*\{(?<body>.*?)\};',
    [Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $cultureMatch.Success) {
    throw "Unable to locate Language.SupportedCultures."
}
$sourceCultures = @(
    [regex]::Matches($cultureMatch.Groups["body"].Value, '"(?<code>[a-z]{2})"') |
        ForEach-Object { $_.Groups["code"].Value }
)
Assert-StringArray -Actual $sourceCultures -Expected $expectedCultures -Description "Language.SupportedCultures"
Assert-Contains $languageSource "public static class ClientRegionMap" "The official client region map must remain centralized"
Assert-Contains $localizationDoc "The Login listening port is the source of truth" "Localization documentation must keep trusted port routing explicit"

$neutralResource = Join-Path $WorldResourceDirectory "LocalizedResources.resx"
if (-not (Test-Path -LiteralPath $neutralResource)) {
    throw "Missing neutral English World resource: $neutralResource"
}
foreach ($culture in $expectedCultures | Where-Object { $_ -ne "en" }) {
    $satelliteResource = Join-Path $WorldResourceDirectory "LocalizedResources.$culture.resx"
    if (-not (Test-Path -LiteralPath $satelliteResource)) {
        throw "Missing World satellite resource for culture '$culture': $satelliteResource"
    }

    $cultureTableToken = "| ``$culture`` |"
    if ($localizationDoc.IndexOf($cultureTableToken, [StringComparison]::Ordinal) -lt 0) {
        throw "Localization documentation is missing canonical culture '$culture'."
    }
}

Write-Host "Verified $($seenCaseIds.Count) world/channel fixtures, 10 protocol region bytes and 10 independent server cultures."
