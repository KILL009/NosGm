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

function Read-RequiredText {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required world/channel verification file was not found: $Path"
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Assert-SameValue {
    param([AllowNull()][object]$Actual, [AllowNull()][object]$Expected, [string]$Description)

    if (-not [object]::Equals($Actual, $Expected)) {
        $actualText = if ($null -eq $Actual) { "<null>" } else { [string]$Actual }
        $expectedText = if ($null -eq $Expected) { "<null>" } else { [string]$Expected }
        throw "$Description. Expected '$expectedText', actual '$actualText'."
    }
}

function Assert-StringArray {
    param([object[]]$Actual, [object[]]$Expected, [string]$Description)

    $actualValues = @($Actual | ForEach-Object { [string]$_ })
    $expectedValues = @($Expected | ForEach-Object { [string]$_ })
    if ($actualValues.Count -ne $expectedValues.Count) {
        throw "$Description count changed. Expected $($expectedValues.Count), actual $($actualValues.Count)."
    }

    for ($index = 0; $index -lt $expectedValues.Count; $index++) {
        if (-not [string]::Equals($actualValues[$index], $expectedValues[$index], [StringComparison]::Ordinal)) {
            throw "$Description differs at index $index."
        }
    }
}

function Assert-Contains {
    param([string]$Content, [string]$Needle, [string]$Description)

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "World/channel source contract failed: $Description"
    }
}

function Assert-NotContains {
    param([string]$Content, [string]$Needle, [string]$Description)

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "World/channel source contract failed: $Description"
    }
}

function Assert-Regex {
    param([string]$Content, [string]$Pattern, [string]$Description)

    if (-not [regex]::IsMatch(
            $Content,
            $Pattern,
            [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "World/channel source contract failed: $Description"
    }
}

function Assert-NotRegex {
    param([string]$Content, [string]$Pattern, [string]$Description)

    if ([regex]::IsMatch(
            $Content,
            $Pattern,
            [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "World/channel source contract failed: $Description"
    }
}

function Get-CharacterPrefix {
    param([object[]]$Prefixes, [int]$CharacterCount)

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
        return [pscustomobject]@{ packet = $null; entries = @() }
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
    $groupNumber = 0
    foreach ($world in $visibleWorlds) {
        if (-not [string]::Equals($lastGroup, [string]$world.group, [StringComparison]::Ordinal)) {
            $groupNumber++
            $lastGroup = [string]$world.group
        }

        $color = [int][Math]::Round(([double]$world.connectedAccounts / [double]$world.accountLimit) * 20) + 1
        $entry = "{0}:{1}:{2}:{3}.{4}.{5}" -f @(
            [string]$world.host,
            [int]$world.port,
            $color,
            $groupNumber,
            [int]$world.channelId,
            [string]$world.group)
        $entries.Add($entry)
        $tokens.Add($entry)
    }
    $tokens.Add([string]$Protocol.sentinel)

    return [pscustomobject]@{
        packet = [string]::Join(" ", $tokens)
        entries = @($entries)
    }
}

$fixtureJson = Read-RequiredText $FixturePath
if ([regex]::IsMatch($fixtureJson, '(?i)"(?:username|password|passwordHash|sessionId|accountId|email|token|ipAddress)"')) {
    throw "World/channel fixtures contain a forbidden sensitive field."
}
$fixture = $fixtureJson | ConvertFrom-Json

Assert-SameValue ([int]$fixture.schemaVersion) 1 "Fixture schema version"
Assert-SameValue ([string]$fixture.protocol.header) "NsTeST" "World-list header"
Assert-SameValue ([int]$fixture.protocol.paddingPairs) 56 "NsTeST padding pair count"
Assert-SameValue ([string]$fixture.protocol.sentinel) "-1:-1:-1:10000.10000.1" "World-list sentinel"

$expectedCultures = @("en", "es", "de", "fr", "it", "pl", "cs", "ru", "ja", "zh")
$expectedRegions = @(0, 1, 2, 3, 4, 5, 6, 7, 8, 9)
Assert-StringArray @($fixture.supportedCultures) $expectedCultures "Supported culture fixture"
Assert-StringArray @($fixture.regionTypes | ForEach-Object { [int]$_ }) $expectedRegions "Protocol region byte fixture"

foreach ($characterCount in 0..4) {
    $prefix = Get-CharacterPrefix @($fixture.characterPrefixes) $characterCount
    $prefixTokens = @($prefix.Split(' ', [StringSplitOptions]::RemoveEmptyEntries))
    Assert-SameValue $prefixTokens.Count 8 "Character prefix token count $characterCount"
}

$requiredCaseIds = @(
    "single_channel_region_0",
    "four_characters_region_5",
    "sorted_groups_and_channels",
    "hidden_act4_does_not_shift_groups",
    "only_hidden_act4",
    "no_registered_worlds"
)
$seen = @{}
$positiveCase = $null
foreach ($fixtureCase in @($fixture.cases)) {
    if ($fixtureCase.id -notmatch '^[a-z][a-z0-9_]{2,63}$' -or $seen.ContainsKey($fixtureCase.id)) {
        throw "Invalid or duplicate world/channel fixture ID: $($fixtureCase.id)."
    }
    $seen[$fixtureCase.id] = $true

    $regionType = [int]$fixtureCase.regionType
    if ($regionType -notin $expectedRegions) {
        throw "Fixture '$($fixtureCase.id)' uses unsupported region $regionType."
    }
    foreach ($world in @($fixtureCase.worlds)) {
        if ([int]$world.accountLimit -le 0 || [int]$world.connectedAccounts -lt 0) {
            throw "Fixture '$($fixtureCase.id)' has invalid load values."
        }
    }

    $prefix = Get-CharacterPrefix @($fixture.characterPrefixes) ([int]$fixtureCase.characterCount)
    $model = New-WorldListModel $fixture.protocol $prefix $regionType @($fixtureCase.worlds)
    Assert-SameValue ($null -ne $model.packet) ([bool]$fixtureCase.expected.packetAvailable) "Fixture '$($fixtureCase.id)' packet availability"
    Assert-StringArray @($model.entries) @($fixtureCase.expected.worldEntries) "Fixture '$($fixtureCase.id)' world entries"
    if ($null -ne $model.packet -and $null -eq $positiveCase) {
        $positiveCase = $fixtureCase
    }
}
foreach ($requiredId in $requiredCaseIds) {
    if (-not $seen.ContainsKey($requiredId)) {
        throw "Required world/channel fixture is missing: $requiredId."
    }
}
Assert-SameValue $seen.Count $requiredCaseIds.Count "World/channel fixture count"

$positivePrefix = Get-CharacterPrefix @($fixture.characterPrefixes) ([int]$positiveCase.characterCount)
foreach ($regionType in $expectedRegions) {
    $model = New-WorldListModel $fixture.protocol $positivePrefix $regionType @($positiveCase.worlds)
    $tokens = @($model.packet.Split(' ', [StringSplitOptions]::RemoveEmptyEntries))
    Assert-SameValue $tokens[1] ([string]$regionType) "Protocol region byte $regionType passthrough"
}

$masterSource = Read-RequiredText $MasterServicePath
$loginHandlerSource = Read-RequiredText $LoginHandlerPath
$loginPacketSource = Read-RequiredText $LoginPacketPath
$languageSource = Read-RequiredText $LanguagePath
$localizationDoc = Read-RequiredText $LocalizationDocPath

Assert-Regex $loginPacketSource '\[PacketIndex\(5\)\]\s*public byte RegionType' "RegionType must remain byte field 5 of NoS0575"
Assert-Regex $loginHandlerSource 'TryResolveLoginPort\(\s*_session\.ListeningPort\s*,\s*out regionType\s*,\s*out culture\s*\)' "Login must derive region and culture from the trusted port"
Assert-Regex $loginHandlerSource 'BuildServersPacket\s*\(\s*protocolUsername\s*,\s*regionType\s*,\s*newSessionId' "Login must pass the resolved region to Master"
Assert-NotRegex $loginHandlerSource 'BuildServersPacket\s*\([^;]*loginPacket\.RegionType' "Login must not pass the untrusted packet region to Master"
Assert-Regex $masterSource 'private const int NsTeSTPadding\s*=\s*56;' "NsTeST padding must remain 56 pairs"
Assert-Contains $masterSource '$"NsTeST {regionType} {username}' "Master must emit the supplied region byte"
Assert-Regex $masterSource 'visibleWorlds\s*=.*?Where\(w => w\.ChannelId != 51\).*?OrderBy\(w => w\.WorldGroup\).*?ThenBy\(w => w\.ChannelId\).*?ToList\(\);' "visible worlds must exclude channel 51 and be sorted"
Assert-Regex $masterSource 'if \(visibleWorlds\.Count == 0\)\s*\{\s*return null;\s*\}' "Master must reject an empty visible world list"
Assert-Contains $masterSource 'a.ConnectedWorld?.Id == world.Id' "Channel load must use the exact World ID"
Assert-Contains $masterSource 'channelPacket += "-1:-1:-1:10000.10000.1";' "Master must retain the sentinel"
Assert-NotContains $masterSource 'Logger.Info(channelPacket);' "Master must not log raw NsTeST packets"

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
Assert-StringArray $sourceCultures $expectedCultures "Language.SupportedCultures"
Assert-Contains $localizationDoc "The Login listening port is the source of truth" "Localization documentation must keep trusted port routing explicit"

$neutralResource = Join-Path $WorldResourceDirectory "LocalizedResources.resx"
if (-not (Test-Path -LiteralPath $neutralResource)) {
    throw "Missing neutral English World resource."
}
foreach ($culture in $expectedCultures | Where-Object { $_ -ne "en" }) {
    $resource = Join-Path $WorldResourceDirectory "LocalizedResources.$culture.resx"
    if (-not (Test-Path -LiteralPath $resource)) {
        throw "Missing World satellite resource for culture '$culture'."
    }
    Assert-Contains $localizationDoc "| ``$culture`` |" "Localization documentation is missing culture '$culture'"
}

Write-Host "Verified $($seen.Count) world/channel fixtures, ten protocol regions and ten server cultures."
