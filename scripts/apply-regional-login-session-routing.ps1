$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Normalize-NewLines {
    param(
        [string]$Value,
        [string]$NewLine
    )

    return [regex]::Replace($Value, "`r`n|`n|`r", $NewLine)
}

function Replace-ExactOnce {
    param(
        [string]$Source,
        [string]$Old,
        [string]$New,
        [string]$Description,
        [string]$NewLine
    )

    $oldValue = Normalize-NewLines $Old $NewLine
    $newValue = Normalize-NewLines $New $NewLine
    $first = $Source.IndexOf($oldValue, [StringComparison]::Ordinal)
    if ($first -lt 0) {
        throw "Expected source was not found: $Description"
    }

    $second = $Source.IndexOf($oldValue, $first + $oldValue.Length, [StringComparison]::Ordinal)
    if ($second -ge 0) {
        throw "Expected exactly one source match: $Description"
    }

    Write-Host "Applied: $Description"
    return $Source.Substring(0, $first) + $newValue + $Source.Substring($first + $oldValue.Length)
}

function Write-Utf8Bom {
    param(
        [string]$Path,
        [string]$Content
    )

    [IO.File]::WriteAllText(
        (Resolve-Path -LiteralPath $Path),
        $Content,
        (New-Object Text.UTF8Encoding($true)))
}

$sessionPath = "Data/NosGm.GameObject/Networking/ClientSession.cs"
$sessionContent = Get-Content -LiteralPath $sessionPath -Raw
$sessionNewLine = if ($sessionContent.Contains("`r`n")) { "`r`n" } else { "`n" }

if (-not $sessionContent.Contains("public int ListeningPort { get; }")) {
    $sessionContent = Replace-ExactOnce $sessionContent @'
        public ClientSession(INetworkClient client)
'@ @'
        public ClientSession(INetworkClient client, int listeningPort = 0)
'@ "accept the trusted local listening port" $sessionNewLine

    $sessionContent = Replace-ExactOnce $sessionContent @'
            // initialize network client
            _client = client;

            // absolutely new instantiated Client has no SessionId
'@ @'
            // initialize network client
            _client = client;
            ListeningPort = listeningPort;

            // absolutely new instantiated Client has no SessionId
'@ "store the trusted local listening port" $sessionNewLine

    $sessionContent = Replace-ExactOnce $sessionContent @'
        public long ClientId => _client.ClientId;

        public MapInstance CurrentMapInstance { get; set; }
'@ @'
        public long ClientId => _client.ClientId;

        public int ListeningPort { get; }

        public MapInstance CurrentMapInstance { get; set; }
'@ "expose the trusted local listening port" $sessionNewLine

    Write-Utf8Bom $sessionPath $sessionContent
}
else {
    Write-Host "ClientSession listening-port routing is already applied."
}

$worldVerifierPath = "scripts/verify-world-channel-lists.ps1"
$worldVerifierContent = Get-Content -LiteralPath $worldVerifierPath -Raw
$worldVerifierNewLine = if ($worldVerifierContent.Contains("`r`n")) { "`r`n" } else { "`n" }
$worldVerifierChanged = $false

if ($worldVerifierContent.Contains("Login must pass RegionType unchanged to Master")) {
    $worldVerifierContent = Replace-ExactOnce $worldVerifierContent @'
Assert-Regex $loginPacketSource '\[PacketIndex\(5\)\]\s*public byte RegionType' "RegionType must remain byte field 5 of NoS0575"
Assert-Regex $loginHandlerSource 'BuildServersPacket\s*\(\s*username\s*,\s*loginPacket\.RegionType\s*,\s*newSessionId' "Login must pass RegionType unchanged to Master"
'@ @'
Assert-Regex $loginPacketSource '\[PacketIndex\(5\)\]\s*public byte RegionType' "RegionType must remain byte field 5 of NoS0575 for compatibility diagnostics"
Assert-Regex $loginHandlerSource 'TryResolveLoginPort\s*\(\s*_session\.ListeningPort\s*,\s*out byte resolvedRegionType\s*,\s*out string clientCulture\s*\)' "Login must derive RegionType and culture from the accepted local port"
Assert-Regex $loginHandlerSource 'BuildServersPacket\s*\(\s*username\s*,\s*resolvedRegionType\s*,\s*newSessionId' "Login must pass the port-derived RegionType to Master"
Assert-NotContains $loginHandlerSource 'BuildServersPacket(`r`n                username,`r`n                loginPacket.RegionType' "Login must not pass the untrusted packet RegionType to Master"
'@ "replace packet-derived world routing with port-derived routing" $worldVerifierNewLine

    $worldVerifierContent = Replace-ExactOnce $worldVerifierContent @'
Assert-StringArray -Actual $sourceCultures -Expected $expectedCultures -Description "Language.SupportedCultures"
Assert-NotContains $languageSource "RegionType" "Language selection must remain independent from the login protocol region byte"
Assert-Contains $localizationDoc '`RegionType` must not be treated as a locale.' "Localization documentation must keep region and culture separate"
'@ @'
Assert-StringArray -Actual $sourceCultures -Expected $expectedCultures -Description "Language.SupportedCultures"
Assert-Contains $languageSource "public static class ClientRegionMap" "The official client region map must remain centralized"
Assert-Contains $localizationDoc "The Login listening port is the source of truth" "Localization documentation must keep trusted port routing explicit"
'@ "update language boundary verification for the official client region map" $worldVerifierNewLine

    $worldVerifierChanged = $true
}

if (-not $worldVerifierContent.Contains("function Assert-NotRegex")) {
    $worldVerifierContent = Replace-ExactOnce $worldVerifierContent @'
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

'@ @'
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
        throw "World/channel source contract failed: $Description"
    }
}

'@ "add a format-independent negative source assertion" $worldVerifierNewLine
    $worldVerifierChanged = $true
}

if ($worldVerifierContent.Contains("Assert-NotContains `$loginHandlerSource 'BuildServersPacket(``r``n")) {
    $worldVerifierContent = Replace-ExactOnce $worldVerifierContent @'
Assert-NotContains $loginHandlerSource 'BuildServersPacket(`r`n                username,`r`n                loginPacket.RegionType' "Login must not pass the untrusted packet RegionType to Master"
'@ @'
Assert-NotRegex $loginHandlerSource 'BuildServersPacket\s*\(\s*username\s*,\s*loginPacket\.RegionType' "Login must not pass the untrusted packet RegionType to Master"
'@ "make the packet-RegionType prohibition format independent" $worldVerifierNewLine
    $worldVerifierChanged = $true
}

if ($worldVerifierChanged) {
    Write-Utf8Bom $worldVerifierPath $worldVerifierContent
}
else {
    Write-Host "World/channel verifier already uses hardened port-derived routing."
}

Write-Host "Regional Login session routing applied successfully."
