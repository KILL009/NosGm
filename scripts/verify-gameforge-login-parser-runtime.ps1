param(
    [string]$AssemblyPath = "bin/Release/Login/NosGm.Master.Library.dll"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $AssemblyPath -PathType Leaf)) {
    throw "Compiled Login parser assembly was not found: $AssemblyPath"
}

$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath))
$parserType = $assembly.GetType(
    "NosGm.Master.Library.Interface.GameforgeLoginPacketParser",
    $true,
    $false)
$tryParse = $parserType.GetMethod(
    "TryParse",
    [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::Static)
if ($null -eq $tryParse) {
    throw "GameforgeLoginPacketParser.TryParse was not found in $AssemblyPath"
}

function Invoke-Parser([string]$Packet) {
    $arguments = [object[]]@($Packet, $null, $null)
    $success = [bool]$tryParse.Invoke($null, $arguments)
    return [pscustomobject]@{
        Success = $success
        Payload = $arguments[1]
        ErrorCode = [string]$arguments[2]
    }
}

function Assert-Accepted([string]$Name, [string]$Packet) {
    $result = Invoke-Parser $Packet
    if (-not $result.Success) {
        throw "$Name should be accepted, but failed with '$($result.ErrorCode)'."
    }
    if ($null -eq $result.Payload) {
        throw "$Name was accepted without a payload."
    }
    if ([byte]$result.Payload.CountryId -ne 5) {
        throw "$Name parsed the wrong country: $($result.Payload.CountryId)"
    }
    if ($result.Payload.ClientVersion.ToString() -ne "0.9.3.3256") {
        throw "$Name parsed the wrong version: $($result.Payload.ClientVersion)"
    }
    Write-Host "[PASS] $Name"
}

function Assert-Rejected([string]$Name, [string]$Packet, [string]$ExpectedError) {
    $result = Invoke-Parser $Packet
    if ($result.Success) {
        throw "$Name should be rejected."
    }
    if ($result.ErrorCode -ne $ExpectedError) {
        throw "$Name returned '$($result.ErrorCode)' instead of '$ExpectedError'."
    }
    Write-Host "[PASS] $Name -> $ExpectedError"
}

$token = [Guid]::NewGuid().ToString("D")
$installationId = [Guid]::NewGuid().ToString("D")
$packet = "NoS0577 $token  $installationId 0123ABCD 5`v0.9.3.3256 0 0123456789ABCDEF0123456789ABCDEF"

if ($packet.Length -ne 139) {
    throw "The canonical NoS0577 fixture changed unexpectedly: Length=$($packet.Length)"
}

Assert-Accepted "Canonical NoS0577" $packet
Assert-Accepted "NoS0577 with one terminal NUL" ($packet + [char]0)
Assert-Accepted "NoS0577 with one terminal CR" ($packet + [char]13)
Assert-Accepted "NoS0577 with one terminal LF" ($packet + [char]10)
Assert-Rejected "NoS0577 with embedded NUL" ($packet.Insert(20, [string][char]0)) "UnexpectedControlCharacter"
Assert-Rejected "NoS0577 with embedded CR" ($packet.Insert(20, [string][char]13)) "UnexpectedControlCharacter"
Assert-Rejected "NoS0577 with embedded LF" ($packet.Insert(20, [string][char]10)) "UnexpectedControlCharacter"
Assert-Rejected "NoS0577 with two terminal NULs" ($packet + [char]0 + [char]0) "UnexpectedControlCharacter"
Assert-Rejected "NoS0577 with terminal CRLF" ($packet + [char]13 + [char]10) "UnexpectedControlCharacter"
Assert-Rejected "NoS0577 with terminal LFCR" ($packet + [char]10 + [char]13) "UnexpectedControlCharacter"

Write-Host "Gameforge NoS0577 runtime parser accepts one terminal NUL, CR or LF framing delimiter and rejects embedded or repeated controls."
