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

$tail = "-99 0 -99 0 4242 HOST_A:1337:1:1.1.Sumeria -1:-1:-1:10000.10000.1"
$legacy = "NsTeST 5 test1 $tail"
$modern = "NsTeST  5 test1 2 0 0 0 0 0 0 $tail"

Assert-Equal `
    (Invoke-Normalizer $legacy) `
    $modern `
    "Legacy NsTeST receives the leading blank and seven-field preamble"

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

if (-not (Invoke-Normalizer $legacy).StartsWith("NsTeST  5 test1 2 0 0 0 0 0 0 ", [StringComparison]::Ordinal)) {
    throw "The normalized packet does not preserve the literal double space after the NsTeST header."
}

Write-Host "Modern NsTeST layout normalization is idempotent and preserves the required leading blank and preamble."
