param(
    [string]$FixturePath = "tests/fixtures/client-region-map.json",
    [string]$LoginPacketPath = "Data/NosGm.Packets/Packets/ClientPackets/LoginPacket.cs",
    [string]$LoginHandlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs",
    [string]$LanguagePath = "Data/NosGm.Core/Language.cs",
    [string]$LocalizationDocPath = "docs/localization.md"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-RequiredText {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required client-region verification file was not found: $Path"
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
        throw "Client-region contract failed: $Description"
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
        throw "Client-region contract failed: $Description"
    }
}

$fixtureJson = Read-RequiredText $FixturePath
if ([regex]::IsMatch($fixtureJson, '(?i)"(?:username|password|passwordHash|sessionId|accountId|email|token)"')) {
    throw "Client-region fixture contains a forbidden sensitive field."
}

$fixture = $fixtureJson | ConvertFrom-Json
Assert-ExactProperties $fixture @("schemaVersion", "clientRegions") "Client-region fixture root"
Assert-SameValue -Actual ([int]$fixture.schemaVersion) -Expected 1 -Description "Client-region fixture schema version"

$expectedRegions = @(
    [pscustomobject][ordered]@{ code = "en"; regionType = 0 },
    [pscustomobject][ordered]@{ code = "de"; regionType = 1 },
    [pscustomobject][ordered]@{ code = "fr"; regionType = 2 },
    [pscustomobject][ordered]@{ code = "it"; regionType = 3 },
    [pscustomobject][ordered]@{ code = "pl"; regionType = 4 },
    [pscustomobject][ordered]@{ code = "es"; regionType = 5 },
    [pscustomobject][ordered]@{ code = "cs"; regionType = 6 },
    [pscustomobject][ordered]@{ code = "ru"; regionType = 7 },
    [pscustomobject][ordered]@{ code = "ja"; regionType = 8 },
    [pscustomobject][ordered]@{ code = "zh"; regionType = 9 }
)

$actualRegions = @($fixture.clientRegions)
Assert-SameValue -Actual $actualRegions.Count -Expected $expectedRegions.Count -Description "Client-region mapping count"

$seenCodes = @{}
$seenRegionTypes = @{}
for ($index = 0; $index -lt $expectedRegions.Count; $index++) {
    $actual = $actualRegions[$index]
    $expected = $expectedRegions[$index]
    Assert-ExactProperties $actual @("code", "regionType") "Client-region mapping at index $index"

    $code = [string]$actual.code
    $regionType = [int]$actual.regionType

    if ($code -notmatch '^[a-z]{2}$') {
        throw "Client-region code '$code' is not canonical."
    }
    if ($seenCodes.ContainsKey($code)) {
        throw "Duplicate client-region code: $code."
    }
    if ($seenRegionTypes.ContainsKey($regionType)) {
        throw "Duplicate client RegionType: $regionType."
    }
    $seenCodes[$code] = $true
    $seenRegionTypes[$regionType] = $true

    Assert-SameValue -Actual $code -Expected ([string]$expected.code) -Description "Client-region code at index $index"
    Assert-SameValue -Actual $regionType -Expected ([int]$expected.regionType) -Description "RegionType for client '$code'"
}

$languageSource = Read-RequiredText $LanguagePath
$cultureMatch = [regex]::Match(
    $languageSource,
    'private static readonly string\[\] SupportedCultures\s*=\s*\{(?<body>.*?)\};',
    [Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $cultureMatch.Success) {
    throw "Unable to locate Language.SupportedCultures."
}

$supportedCultures = @(
    [regex]::Matches($cultureMatch.Groups["body"].Value, '"(?<code>[a-z]{2})"') |
        ForEach-Object { $_.Groups["code"].Value }
)
foreach ($mapping in $expectedRegions) {
    if ([string]$mapping.code -notin $supportedCultures) {
        throw "Client-region code '$($mapping.code)' is missing from Language.SupportedCultures."
    }
}

$loginPacketSource = Read-RequiredText $LoginPacketPath
$loginHandlerSource = Read-RequiredText $LoginHandlerPath
$localizationDoc = Read-RequiredText $LocalizationDocPath

Assert-Regex $loginPacketSource '\[PacketIndex\(5\)\]\s*public byte RegionType' "RegionType must remain byte field 5 of NoS0575"
Assert-Regex $loginHandlerSource 'BuildServersPacket\s*\(\s*username\s*,\s*loginPacket\.RegionType\s*,\s*newSessionId' "Login must pass RegionType unchanged to Master"
Assert-Contains $localizationDoc '`RegionType` must not be treated as a locale.' "Account.Language must remain independent from the client region byte"

foreach ($mapping in $expectedRegions) {
    $tableToken = "| ``$($mapping.code)`` | ``$($mapping.regionType)`` |"
    Assert-Contains $localizationDoc $tableToken "Localization documentation must include $($mapping.code) = $($mapping.regionType)"
}

Write-Host "Verified official client RegionType map: en=0, de=1, fr=2, it=3, pl=4, es=5, cs=6, ru=7, ja=8, zh=9."
