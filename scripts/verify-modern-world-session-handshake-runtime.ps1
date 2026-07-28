param(
    [string]$AssemblyPath = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ([string]::IsNullOrWhiteSpace($AssemblyPath)) {
    $candidates = @(
        "Data/NosGm.Core/bin/Release/NosGm.Core.dll",
        "bin/Release/World/NosGm.Core.dll",
        "bin/Release/Login/NosGm.Core.dll"
    )
    $AssemblyPath = $candidates |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($AssemblyPath) -or
    -not (Test-Path -LiteralPath $AssemblyPath -PathType Leaf)) {
    throw "Compiled NosGm.Core assembly was not found. Build the solution in Release first."
}

$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath))
$worldCryptographyType = $assembly.GetType(
    "NosGm.Core.WorldCryptography",
    $true,
    $false)
$normalize = $worldCryptographyType.GetMethod(
    "NormalizeInitialWorldHandshake",
    [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Static)
if ($null -eq $normalize) {
    throw "WorldCryptography.NormalizeInitialWorldHandshake was not found in $AssemblyPath"
}

function Assert-Normalized {
    param(
        [string]$Name,
        [AllowEmptyString()][string]$InputValue,
        [AllowEmptyString()][string]$ExpectedValue
    )

    $actual = [string]$normalize.Invoke($null, [object[]]@($InputValue))
    if (-not [string]::Equals($actual, $ExpectedValue, [StringComparison]::Ordinal)) {
        throw "$Name failed. Expected '$ExpectedValue', actual '$actual'."
    }

    Write-Host "[PASS] $Name"
}

Assert-Normalized "Modern one-token session" "4242" "0 4242"
Assert-Normalized "Modern one-token session with surrounding whitespace" "  4242  " "0 4242"
Assert-Normalized "Modern one-token session with metadata suffix" "4242\client" "0 4242"
Assert-Normalized "Legacy packet-id and session pair" "7 4242" "7 4242"
Assert-Normalized "Legacy zero packet-id pair" "0 4242" "0 4242"
Assert-Normalized "Zero is not a valid modern session" "0" "0"
Assert-Normalized "Negative is not a valid modern session" "-1" "-1"
Assert-Normalized "Non-numeric custom parameter" "invalid" "invalid"
Assert-Normalized "Empty custom parameter" "" ""

Write-Host "World session handshake accepts the modern one-token SessionId while preserving the legacy two-token format."
