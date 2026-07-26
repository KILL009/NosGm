$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Replace-ExactOnce {
    param(
        [string]$Path,
        [string]$Old,
        [string]$New,
        [string]$Description
    )

    $content = Get-Content -LiteralPath $Path -Raw
    $newLine = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }
    $oldValue = [regex]::Replace($Old, "`r`n|`n|`r", $newLine)
    $newValue = [regex]::Replace($New, "`r`n|`n|`r", $newLine)

    if ($content.Contains($newValue)) {
        Write-Host "Already applied: $Description"
        return
    }

    $first = $content.IndexOf($oldValue, [StringComparison]::Ordinal)
    if ($first -lt 0) {
        throw "Expected source was not found: $Description"
    }

    $second = $content.IndexOf($oldValue, $first + $oldValue.Length, [StringComparison]::Ordinal)
    if ($second -ge 0) {
        throw "Expected exactly one source match: $Description"
    }

    $content = $content.Substring(0, $first) + $newValue + $content.Substring($first + $oldValue.Length)
    [IO.File]::WriteAllText(
        (Resolve-Path -LiteralPath $Path),
        $content,
        (New-Object Text.UTF8Encoding($true)))
    Write-Host "Applied: $Description"
}

Replace-ExactOnce \
    "Data/NosGm.Program/NosGm.Master.Server/CommunicationService.cs" \
    @'
                var currentlyConnectedAccounts =
                    MSManager.Instance.ConnectedAccounts.CountLinq(a => a.ConnectedWorld?.ChannelId == world.ChannelId);
'@ \
    @'
                var currentlyConnectedAccounts =
                    MSManager.Instance.ConnectedAccounts.CountLinq(a => a.ConnectedWorld?.Id == world.Id);
'@ \
    "count channel load by exact World ID"

Replace-ExactOnce \
    "scripts/verify-world-channel-lists.ps1" \
    @'
Assert-Contains $masterSource 'foreach (var world in visibleWorlds)' "Master must build the packet from the deterministic visible-world snapshot"
Assert-Contains $masterSource 'channelPacket += "-1:-1:-1:10000.10000.1";' "Master must retain the terminal world-list sentinel"
'@ \
    @'
Assert-Contains $masterSource 'foreach (var world in visibleWorlds)' "Master must build the packet from the deterministic visible-world snapshot"
Assert-Contains $masterSource 'a.ConnectedWorld?.Id == world.Id' "Channel load must count sessions for the exact World instead of every group sharing the same ChannelId"
Assert-Contains $masterSource 'channelPacket += "-1:-1:-1:10000.10000.1";' "Master must retain the terminal world-list sentinel"
'@ \
    "guard exact World load accounting"

Write-Host "World load accounting fix applied successfully."
