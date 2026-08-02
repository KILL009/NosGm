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

function Get-Fields([string]$Packet) {
    return @($Packet.Split(
        [char[]]@(" "),
        [StringSplitOptions]::RemoveEmptyEntries))
}

$shoppingPacket = Read-Source "Data/NosGm.Packets/Packets/ClientPackets/ShoppingPacket.cs"
$ncifPacket = Read-Source "Data/NosGm.Packets/Packets/ClientPackets/NcifPacket.cs"
$pdtClosePacket = Read-Source "Data/NosGm.Packets/Packets/ClientPackets/PdtClosePacket.cs"
$shoppingHandler = Read-Source "Data/NosGm.Handler/PacketHandler/Npc/ShoppingPacketHandler.cs"
$pdtseHandler = Read-Source "Data/NosGm.Handler/PacketHandler/Npc/PdtsePacketHandler.cs"
$requestNpcHandler = Read-Source "Data/NosGm.Handler/PacketHandler/Npc/RequestNpcPacketHandler.cs"
$networkClient = Read-Source "Data/NosGm.Core/Networking/NetworkClient.cs"

Assert-True ($shoppingPacket.Contains('[PacketHeader("shopping")]')) `
    "shopping request header is registered"
Assert-True ($shoppingPacket.Contains('[PacketIndex(0)] public byte Type')) `
    "shopping field 0 remains the request type"
Assert-True ($shoppingPacket.Contains('[PacketIndex(3)] public int NpcId')) `
    "shopping field 3 remains the NPC transport id"

Assert-True ($ncifPacket.Contains('[PacketHeader("ncif")]')) `
    "ncif request header is registered"
Assert-True ($ncifPacket.Contains('[PacketIndex(0)] public byte Type')) `
    "ncif field 0 remains the entity type"
Assert-True ($ncifPacket.Contains('[PacketIndex(1)] public long TargetId')) `
    "ncif field 1 remains the entity transport id"

Assert-True ($pdtClosePacket.Contains('[PacketHeader("pdtclose")]')) `
    "pdtclose request header is registered"
Assert-True (-not $pdtClosePacket.Contains('[PacketIndex(')) `
    "pdtclose remains a zero-argument request"

Assert-True ($shoppingHandler.Contains('Session.SendPacket($"n_inv 2 {mapnpc.MapNpcId} 0 {typeshop}{shoplist}")')) `
    "NPC shops emit the observed n_inv 2 header and shop type"
Assert-True ($pdtseHandler.Contains('var recipePacket = $"m_list 3 {recipe.Amount}"')) `
    "recipe details emit produced amount first"
Assert-True ($pdtseHandler.Contains('recipePacket += $" {ite.ItemVNum} {ite.Amount}"')) `
    "recipe details emit ingredient VNum and amount pairs"
Assert-True ($pdtseHandler.Contains('recipePacket += " 0"')) `
    "recipe details retain the official zero terminator"
Assert-True ($requestNpcHandler.Contains('recipelist += " -100"')) `
    "recipe lists retain the official -100 terminator"

Assert-True ($networkClient.Contains('NormalizeOfficialPacketLayout(packet)')) `
    "outgoing packets pass through the official layout normalizer"
foreach ($windowType in @("8", "9", "27", "93")) {
    Assert-True ($networkClient.Contains('"' + $windowType + '"')) `
        "wopen type $windowType is covered by modern four-argument normalization"
}

$officialShopping = Get-Fields "shopping 0 0 2 3067"
Assert-True ($officialShopping.Count -eq 5) "official shopping capture has four payload fields"
Assert-True ($officialShopping[4] -eq "3067") "official shopping NPC id is payload field 3"

$officialNcif = Get-Fields "ncif 2 3067"
Assert-True ($officialNcif.Count -eq 3) "official ncif capture has two payload fields"

foreach ($packet in @(
    "wopen 8 0 0 0",
    "wopen 9 0 0 0",
    "wopen 27 0 0 0",
    "wopen 93 0 0 0")) {
    Assert-True ((Get-Fields $packet).Count -eq 5) `
        "official '$packet' has four payload fields"
}

$officialRecipeList = Get-Fields "m_list 2 1002 1003 1004 1006 -100"
Assert-True ($officialRecipeList[-1] -eq "-100") `
    "official m_list 2 capture ends with -100"

$officialRecipeDetails = Get-Fields "m_list 3 10 2029 3 2097 5 2098 10 2099 5 0"
Assert-True ($officialRecipeDetails[2] -eq "10") `
    "official m_list 3 capture carries produced amount first"
Assert-True ($officialRecipeDetails[-1] -eq "0") `
    "official m_list 3 capture ends with zero"

$officialQnpc = Get-Fields "qnpc 394|88|56 -1|-1|-1 -1|-1|-1 -1|-1|-1 -1|-1|-1 -1|-1|-1 -1|-1|-1 -1|-1|-1 -1|-1|-1 -1|-1|-1"
Assert-True ($officialQnpc.Count -eq 11) `
    "official qnpc capture contains one active entry and nine empty entries"
Assert-True ($officialQnpc[1].Split('|').Count -eq 3) `
    "official qnpc active entry contains NPC VNum, map id and level"

$officialStat = Get-Fields "st 1 8304043 56 0 100 100 2791 3727 2791 3727 3 684.56"
Assert-True ($officialStat.Count -eq 13) `
    "new official st capture contains the base layout plus a two-field tail"
Write-Host "[INFO] st tail semantics are intentionally not rewritten from one capture."
Write-Host "[INFO] qnpc emission timing is intentionally not invented without the associated quest event."
Write-Host "Official shop and crafting packet compatibility contracts passed."
