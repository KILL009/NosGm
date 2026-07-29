param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required source file was not found: $RelativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Assert-True([bool]$Condition, [string]$Name) {
    if (-not $Condition) {
        throw "$Name failed."
    }

    Write-Host "[PASS] $Name"
}

function Assert-Equal($Actual, $Expected, [string]$Name) {
    if ($Actual -ne $Expected) {
        throw "$Name failed. Expected '$Expected', received '$Actual'."
    }

    Write-Host "[PASS] $Name"
}

function Get-PacketFields([string]$Packet) {
    $tokens = $Packet.Split(
        [char[]]@(" "),
        [StringSplitOptions]::RemoveEmptyEntries)
    return @($tokens | Select-Object -Skip 1)
}

$equipmentPacket = Read-Source "Data/NosGm.Packets/Packets/ClientPackets/EquipmentInfoPacket.cs"
$character = Read-Source "Data/NosGm.GameObject/Character.cs"
$mate = Read-Source "Data/NosGm.GameObject/Mate.cs"
$ncifHandler = Read-Source "Data/NosGm.Handler/PacketHandler/Basic/NcifPacketHandler.cs"
$itemInstance = Read-Source "Data/NosGm.GameObject/Item/Instance/ItemInstance.cs"

Assert-True `
    ($equipmentPacket.Contains('[PacketHeader("eqinfo", "eginfo")]')) `
    "Both legacy eqinfo and current eginfo requests are accepted"

$statSources = @($character, $mate, $ncifHandler)
$modernStatMarkers = 0
foreach ($source in $statSources) {
    $modernStatMarkers += [regex]::Matches(
        $source,
        '\{(?:BattleEntity\.MpMax|MaxMp|npc\.MaxMp|monster\.MaxMp)\} 0\{').Count
}
Assert-Equal $modernStatMarkers 4 "All player, mate, NPC and monster st packets include field 10"

Assert-True `
    ($character.Contains('$"{ArenaWinner} 0 -1";')) `
    "c_info emits the current final sentinel"

$reqInfoStart = $character.IndexOf("public string GenerateReqInfo()", [StringComparison]::Ordinal)
$reqInfoEnd = $character.IndexOf("public string GenerateRest()", $reqInfoStart, [StringComparison]::Ordinal)
Assert-True ($reqInfoStart -ge 0 -and $reqInfoEnd -gt $reqInfoStart) "GenerateReqInfo source is discoverable"
$reqInfo = $character.Substring($reqInfoStart, $reqInfoEnd - $reqInfoStart)

Assert-True `
    ($reqInfo.Contains("(UseSp ? Morph : -1)")) `
    "tc_info emits -1 when no specialist card is active"
Assert-True `
    ($reqInfo.Contains('TalentSurrender} 0 {MasterPoints} {Compliment} {Act4Points}')) `
    "tc_info preserves the modern fixed, Master Point, compliment and Act 4 fields"
Assert-True `
    ($reqInfo.Contains('Language.Instance.GetMessageFromKey("NO_PREZ_MESSAGE")')) `
    "tc_info supplies a localized empty biography"
Assert-True `
    ($reqInfo.Contains("Biography.Replace('\r', ' ').Replace('\n', ' ')")) `
    "tc_info keeps biographies on one packet line"
Assert-True `
    (-not $reqInfo.Contains("Duel Won:")) `
    "tc_info no longer injects multiline text ahead of the biography"

Assert-True `
    ([regex]::IsMatch(
        $itemInstance,
        'ShellEffects\.Count\.ToString\(\),\s*\"0\"',
        [Text.RegularExpressions.RegexOptions]::Singleline)) `
    "e_info includes the reserved option field after the shell count"
Assert-True `
    ($itemInstance.Contains('{level}.{effectId}.{effect.Value}.{effect.Upgrade}')) `
    "e_info emits all four modern shell option components"
Assert-True `
    ($itemInstance.Contains("if (ShellEffects.Count > 0 || hasRuneData)")) `
    "e_info appends rune data only when the modern extension is present"
Assert-Equal `
    ([regex]::Matches($itemInstance, '\{weaponOptions\}').Count) `
    4 `
    "Every weapon e_info branch uses the modern option serializer"
Assert-Equal `
    ([regex]::Matches($itemInstance, '\{armorOptions\}').Count) `
    2 `
    "Every armor e_info branch uses the modern option serializer"

# Captures supplied from the current official server/client. These assertions
# freeze the observed field positions so a future edit cannot silently regress.
$officialCInfo = Get-PacketFields `
    "c_info ElMaYorClaSiCo - -1 -1 - 2391245 0 0 1 13 2 -6 0 0 0 0 0 0 0 -1"
Assert-Equal $officialCInfo.Count 20 "Official c_info has 20 fields"
Assert-Equal $officialCInfo[19] "-1" "Official c_info field 19 is the modern sentinel"

$officialStat = Get-PacketFields "st 3 2887 1 0 100 100 156 10 156 10 0"
Assert-Equal $officialStat.Count 11 "Official st has 11 base fields"
Assert-Equal $officialStat[10] "0" "Official st field 10 is reserved"

$officialArmor = Get-PacketFields "e_info 2 769 2 0 0 74 152 198 184 335 103156 -1 0 0 0 0"
Assert-Equal $officialArmor.Count 16 "Official armor e_info has the expected empty-option layout"
Assert-Equal $officialArmor[14] "0" "Official armor shell count is field 14"
Assert-Equal $officialArmor[15] "0" "Official armor reserved option field is field 15"

$officialWeapon = Get-PacketFields "e_info 1 757 5 0 0 73 349 442 346 6 100 60 100 98039 -1 0 0 0 0"
Assert-Equal $officialWeapon.Count 19 "Official weapon e_info has the expected empty-option layout"
Assert-Equal $officialWeapon[17] "0" "Official weapon shell count is field 17"
Assert-Equal $officialWeapon[18] "0" "Official weapon reserved option field is field 18"

$officialShellWeapon = Get-PacketFields `
    "e_info 0 4008 4 2 1 80 332 367 380 15 270 0 100 120000 -1 4 2391245 5 0 1.16.48.0 2.9.11.0 2.26.8.0 10.36.6.0 10.34.7.0 0"
Assert-Equal $officialShellWeapon[17] "5" "Official shelled weapon exposes five options"
Assert-Equal $officialShellWeapon[18] "0" "Official shelled weapon retains the reserved option field"
foreach ($option in $officialShellWeapon[19..23]) {
    Assert-True `
        ($option -match '^\d+\.\d+\.-?\d+\.-?\d+$') `
        "Official shell option '$option' has four components"
}
Assert-Equal $officialShellWeapon[24] "0" "Official shelled weapon carries the zero rune amount"

$officialTargetInfo = Get-PacketFields `
    "tc_info 85 ElMaYorClaSiCo 2 44 2 0 -1 - 14 6 1 6 5 1 4 2 1 2 0 0 0 54825 0 0 0 -1 388 561 16 0 10 0 0 0 0 0 1 0 No hay autodescripción"
Assert-Equal $officialTargetInfo[25] "-1" "Official tc_info no-SP morph sentinel is -1"
Assert-Equal $officialTargetInfo[29] "0" "Official tc_info reserved field remains zero"
Assert-Equal $officialTargetInfo[30] "10" "Official tc_info Master Points remain at field 30"
Assert-Equal $officialTargetInfo[36] "1" "Official tc_info Hero Level remains at field 36"
Assert-Equal $officialTargetInfo[37] "0" "Official tc_info fairy level remains at field 37"

Write-Host "Modern official packet compatibility contracts passed."
