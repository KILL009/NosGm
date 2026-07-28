param(
    [string]$AssemblyPath = "bin/Release/Login/NosGm.Master.Library.dll"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $AssemblyPath -PathType Leaf)) {
    throw "Compiled Master library was not found: $AssemblyPath"
}

$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath))
$clientType = $assembly.GetType(
    "NosGm.Master.Library.Client.CommunicationServiceClient",
    $true,
    $false)
$normalize = $clientType.GetMethod(
    "NormalizeNsTeSTPacketLayout",
    [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Static)
if ($null -eq $normalize) {
    throw "CommunicationServiceClient.NormalizeNsTeSTPacketLayout was not found in $AssemblyPath"
}

function Invoke-Normalizer([AllowNull()][string]$Packet) {
    return [string]$normalize.Invoke($null, [object[]]@($Packet))
}

function Assert-Equal(
    [AllowNull()][string]$Actual,
    [AllowNull()][string]$Expected,
    [string]$Name) {
    if (-not [string]::Equals($Actual, $Expected, [StringComparison]::Ordinal)) {
        throw "$Name failed.`nExpected: $Expected`nActual:   $Actual"
    }
    Write-Host "[PASS] $Name"
}

$characterSlots = "1 1 -99 0 -99 0 -99 0"
$padding = ((1..56 | ForEach-Object { "-99 0" }) -join " ")
$tail = "$characterSlots $padding 4242 HOST_A:1337:1:1.1.Sumeria -1:-1:-1:10000.10000.1"
$legacy = "NsTeST 5 test1 $tail"
$modern = "NsTeST  5 test1 2 $tail"

Assert-Equal `
    (Invoke-Normalizer $legacy) `
    $modern `
    "Legacy NsTeST receives the leading blank and fixed modern mode field"

Assert-Equal `
    (Invoke-Normalizer $modern) `
    $modern `
    "Modern NsTeST remains unchanged"

Assert-Equal `
    (Invoke-Normalizer "failc 1") `
    "failc 1" `
    "Unrelated packets remain unchanged"

Assert-Equal `
    (Invoke-Normalizer "NsTeST 5") `
    "NsTeST 5" `
    "Incomplete NsTeST remains unchanged"

$normalized = Invoke-Normalizer $legacy
if (-not $normalized.StartsWith("NsTeST  5 test1 2 1 1 ", [StringComparison]::Ordinal)) {
    throw "The normalized packet does not preserve the double space, mode field and first character slot."
}

$tokens = $normalized.Split(
    [char[]]@(' '),
    [StringSplitOptions]::RemoveEmptyEntries)
if ($tokens.Length -le 124 -or $tokens[124] -ne "4242") {
    throw "NsTeST SessionId moved from token 124 after the 4 character-slot and 56 padding pairs."
}
Write-Host "[PASS] NsTeST SessionId remains at token 124."

Write-Host "Modern NsTeST layout normalization is idempotent and preserves the required mode, slots, padding and SessionId."
