$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$path = "Data/NosGm.Program/NosGm.Master.Server/CommunicationService.cs"
$content = Get-Content -LiteralPath $path -Raw
$newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }

if ($content.Contains("var visibleWorlds = MSManager.Instance.WorldServers") -and
    $content.Contains("World list generated | RegionType=") -and
    -not $content.Contains("Logger.Info(channelPacket);")) {
    Write-Host "World/channel hardening is already applied."
    exit 0
}

function Normalize-NewLines {
    param([string]$Value)

    return [regex]::Replace($Value, "`r`n|`n|`r", $newLine)
}

function Replace-ExactOnce {
    param(
        [string]$Source,
        [string]$Old,
        [string]$New,
        [string]$Description
    )

    $oldValue = Normalize-NewLines $Old
    $newValue = Normalize-NewLines $New
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

$content = Replace-ExactOnce $content @'
            if (!MSManager.Instance.AuthentificatedClients.Any(s => s.Equals(CurrentClient.ClientId)))
            {
                return null;
            }
           
            var characters = DAOFactory.CharacterDAO.LoadByAccount(AccountId);
'@ @'
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
'@ "create a deterministic visible-world snapshot"

$content = Replace-ExactOnce $content @'
            foreach (var world in MSManager.Instance.WorldServers.OrderBy(w => w.WorldGroup))
'@ @'
            foreach (var world in visibleWorlds)
'@ "iterate the deterministic visible-world snapshot"

$content = Replace-RegexCount $content '(?m)^[ \t]*Logger\.Info\("===== NsTeST ====="\);\r?\n' '' 1 "remove the NsTeST banner log" ([Text.RegularExpressions.RegexOptions]::Multiline)
$content = Replace-RegexCount $content '(?m)^[ \t]*Logger\.Info\(channelPacket\);\r?\n' '' 2 "remove raw NsTeST packet logs" ([Text.RegularExpressions.RegexOptions]::Multiline)
$content = Replace-RegexCount $content '(?m)^[ \t]*Logger\.Info\(\$"WorldServers Count = \{MSManager\.Instance\.WorldServers\.Count\}"\);\r?\n' '' 1 "remove unbounded world-count logging" ([Text.RegularExpressions.RegexOptions]::Multiline)
$content = Replace-RegexCount $content '(?ms)^[ \t]*Logger\.Info\(\s*\$"Group=\{world\.WorldGroup\} " \+\s*\$"Channel=\{world\.ChannelId\} " \+\s*\$"IP=\{world\.Endpoint\.IpAddress\} " \+\s*\$"Port=\{world\.Endpoint\.TcpPort\}"\);\s*' '' 1 "remove per-endpoint world logging" ([Text.RegularExpressions.RegexOptions]::Multiline -bor [Text.RegularExpressions.RegexOptions]::Singleline)

$content = Replace-RegexCount $content '(?ms)^[ \t]*if \(world\.ChannelId == 51\)\s*\{\s*continue;\s*\}\s*if \(MSManager\.Instance\.WorldServers\.Count < 1\)\s*\{\s*return null;\s*\}\s*' '' 1 "remove obsolete in-loop visibility checks" ([Text.RegularExpressions.RegexOptions]::Multiline -bor [Text.RegularExpressions.RegexOptions]::Singleline)

$content = Replace-ExactOnce $content @'
            channelPacket += "-1:-1:-1:10000.10000.1";
            return channelPacket;
'@ @'
            channelPacket += "-1:-1:-1:10000.10000.1";
            Logger.Info(
                $"World list generated | RegionType={regionType} Groups={worldCount} Channels={visibleWorlds.Count}");
            return channelPacket;
'@ "add bounded world-list diagnostics"

[IO.File]::WriteAllText(
    (Resolve-Path -LiteralPath $path),
    $content,
    (New-Object Text.UTF8Encoding($true)))

Write-Host "World/channel hardening applied successfully."
