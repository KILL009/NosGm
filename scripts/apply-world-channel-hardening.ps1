$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$sourcePath = "Data/NosGm.Program/NosGm.Master.Server/CommunicationService.cs"
$verifierPath = "scripts/verify-world-channel-lists.ps1"
$sourceContent = Get-Content -LiteralPath $sourcePath -Raw
$verifierContent = Get-Content -LiteralPath $verifierPath -Raw
$sourceNewLine = if ($sourceContent.Contains("`r`n")) { "`r`n" } else { "`n" }
$verifierNewLine = if ($verifierContent.Contains("`r`n")) { "`r`n" } else { "`n" }

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

function Replace-RegexCount {
    param(
        [string]$Source,
        [string]$Pattern,
        [string]$Replacement,
        [int]$ExpectedCount,
        [string]$Description,
        [Text.RegularExpressions.RegexOptions]$Options
    )

    $matches = [regex]::Matches($Source, $Pattern, $Options)
    if ($matches.Count -ne $ExpectedCount) {
        throw "Expected $ExpectedCount source matches for '$Description', found $($matches.Count)."
    }

    Write-Host "Applied: $Description"
    return [regex]::Replace($Source, $Pattern, $Replacement, $Options)
}

$sourceApplied = $sourceContent.Contains("var visibleWorlds = MSManager.Instance.WorldServers") -and
    $sourceContent.Contains("World list generated | RegionType=") -and
    -not $sourceContent.Contains("Logger.Info(channelPacket);")

if (-not $sourceApplied) {
    $visibleWorldReplacement = Normalize-NewLines @'
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId)))
            {
                return null;
            }

            var visibleWorlds = MSManager.Instance.WorldServers
                .Where(w => w.ChannelId != 51)
                .OrderBy(w => w.WorldGroup)
                .ThenBy(w => w.ChannelId)
                .ToList();
            if (visibleWorlds.Count == 0)
            {
                return null;
            }

            var characters = DAOFactory.CharacterDAO.LoadByAccount(AccountId);
'@ $sourceNewLine

    $sourceContent = Replace-RegexCount $sourceContent '(?ms)^[ \t]*if \(!MSManager\.Instance\.AuthentificatedClients\.Any\(s => s\.Equals\(CurrentClient\.ClientId\)\)\)\s*\{\s*return null;\s*\}\s*var characters = DAOFactory\.CharacterDAO\.LoadByAccount\(AccountId\);' $visibleWorldReplacement 1 "create a deterministic visible-world snapshot" ([Text.RegularExpressions.RegexOptions]::Multiline -bor [Text.RegularExpressions.RegexOptions]::Singleline)

    $sourceContent = Replace-ExactOnce $sourceContent @'
            foreach (var world in MSManager.Instance.WorldServers.OrderBy(w => w.WorldGroup))
'@ @'
            foreach (var world in visibleWorlds)
'@ "iterate the deterministic visible-world snapshot" $sourceNewLine

    $sourceContent = Replace-RegexCount $sourceContent '(?m)^[ \t]*Logger\.Info\("===== NsTeST ====="\);\r?\n' '' 2 "remove legacy NsTeST banner logs" ([Text.RegularExpressions.RegexOptions]::Multiline)
    $sourceContent = Replace-RegexCount $sourceContent '(?m)^[ \t]*Logger\.Info\(\$"IP registrada = \{worldServer\.EndPointIP\}"\);\r?\n' '' 1 "remove registered endpoint logging" ([Text.RegularExpressions.RegexOptions]::Multiline)
    $sourceContent = Replace-RegexCount $sourceContent '(?m)^[ \t]*Logger\.Info\(channelPacket\);\r?\n' '' 2 "remove raw NsTeST packet logs" ([Text.RegularExpressions.RegexOptions]::Multiline)
    $sourceContent = Replace-RegexCount $sourceContent '(?m)^[ \t]*Logger\.Info\(\$"WorldServers Count = \{MSManager\.Instance\.WorldServers\.Count\}"\);\r?\n' '' 1 "remove unbounded world-count logging" ([Text.RegularExpressions.RegexOptions]::Multiline)
    $sourceContent = Replace-RegexCount $sourceContent '(?ms)^[ \t]*Logger\.Info\(\s*\$"Group=\{world\.WorldGroup\} " \+\s*\$"Channel=\{world\.ChannelId\} " \+\s*\$"IP=\{world\.Endpoint\.IpAddress\} " \+\s*\$"Port=\{world\.Endpoint\.TcpPort\}"\);\s*' '' 1 "remove per-endpoint world logging" ([Text.RegularExpressions.RegexOptions]::Multiline -bor [Text.RegularExpressions.RegexOptions]::Singleline)

    $sourceContent = Replace-RegexCount $sourceContent '(?ms)^[ \t]*if \(world\.ChannelId == 51\)\s*\{\s*continue;\s*\}\s*if \(MSManager\.Instance\.WorldServers\.Count < 1\)\s*\{\s*return null;\s*\}\s*' '' 1 "remove obsolete in-loop visibility checks" ([Text.RegularExpressions.RegexOptions]::Multiline -bor [Text.RegularExpressions.RegexOptions]::Singleline)

    $sourceContent = Replace-ExactOnce $sourceContent @'
            channelPacket += "-1:-1:-1:10000.10000.1";
            return channelPacket;
'@ @'
            channelPacket += "-1:-1:-1:10000.10000.1";
            Logger.Info(
                $"World list generated | RegionType={regionType} Groups={worldCount} Channels={visibleWorlds.Count}");
            return channelPacket;
'@ "add bounded world-list diagnostics" $sourceNewLine
}
else {
    Write-Host "World/channel source hardening is already applied."
}

$unindentedGroupCheck = $sourceNewLine + "if (lastGroup != world.WorldGroup)"
if ($sourceContent.Contains($unindentedGroupCheck)) {
    $sourceContent = Replace-ExactOnce $sourceContent $unindentedGroupCheck ($sourceNewLine + "                if (lastGroup != world.WorldGroup)") "indent the generated group check" $sourceNewLine
}

$unindentedPacketAppend = $sourceNewLine + "channelPacket +="
if ($sourceContent.Contains($unindentedPacketAppend)) {
    $sourceContent = Replace-ExactOnce $sourceContent $unindentedPacketAppend ($sourceNewLine + "                channelPacket +=") "indent the generated channel append" $sourceNewLine
}

[IO.File]::WriteAllText(
    (Resolve-Path -LiteralPath $sourcePath),
    $sourceContent,
    (New-Object Text.UTF8Encoding($true)))

if (-not $verifierContent.Contains('$cultureTableToken = "| ``$culture`` |"')) {
    $verifierContent = Replace-ExactOnce $verifierContent @'
    if ($localizationDoc -notmatch "\| `$culture` \|") {
        throw "Localization documentation is missing canonical culture '$culture'."
    }
'@ @'
    $cultureTableToken = "| ``$culture`` |"
    if ($localizationDoc.IndexOf($cultureTableToken, [StringComparison]::Ordinal) -lt 0) {
        throw "Localization documentation is missing canonical culture '$culture'."
    }
'@ "make localization-table verification PowerShell-safe" $verifierNewLine

    [IO.File]::WriteAllText(
        (Resolve-Path -LiteralPath $verifierPath),
        $verifierContent,
        (New-Object Text.UTF8Encoding($true)))
}
else {
    Write-Host "World/channel verifier hardening is already applied."
}

Write-Host "World/channel hardening applied successfully."
