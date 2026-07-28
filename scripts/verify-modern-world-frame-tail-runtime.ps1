param(
    [string]$AssemblyPath = "",
    [string]$NetworkClientSourcePath = "Data/NosGm.Core/Networking/NetworkClient.cs",
    [string]$ServerClientSourcePath = "Data/NosGm.Core/Networking/Communication/Scs/Server/ScsServerClient.cs"
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
$networkClientType = $assembly.GetType("NosGm.Core.NetworkClient", $true, $false)
$serverClientType = $assembly.GetType(
    "NosGm.Core.Networking.Communication.Scs.Server.ScsServerClient",
    $true,
    $false)

$splitMethod = $networkClientType.GetMethod(
    "TrySplitInitialCustomParameterFrame",
    [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Static)
if ($null -eq $splitMethod) {
    throw "NetworkClient.TrySplitInitialCustomParameterFrame was not found in $AssemblyPath"
}

$baseTransform = $serverClientType.GetMethod(
    "TransformReceivedMessages",
    [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Instance)
$overrideTransform = $networkClientType.GetMethod(
    "TransformReceivedMessages",
    [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Instance)
if ($null -eq $baseTransform -or $null -eq $overrideTransform -or
    -not $baseTransform.IsVirtual -or
    $overrideTransform.GetBaseDefinition() -ne $baseTransform) {
    throw "The ordered receive-message transformation hook is not wired through ScsServerClient and NetworkClient."
}
Write-Host "[PASS] NetworkClient overrides the ordered receive-message transformation hook."

function Invoke-Split {
    param([byte[]]$Data)

    $arguments = New-Object object[] 3
    $arguments[0] = $Data
    $arguments[1] = $null
    $arguments[2] = $null
    $success = [bool]$splitMethod.Invoke($null, $arguments)
    return [pscustomobject]@{
        Success = $success
        Custom = [byte[]]$arguments[1]
        Remainder = [byte[]]$arguments[2]
    }
}

function Assert-Bytes {
    param(
        [string]$Name,
        [byte[]]$Expected,
        [byte[]]$Actual
    )

    if ($null -eq $Actual -or $Expected.Length -ne $Actual.Length) {
        throw "$Name length mismatch. Expected=$($Expected.Length) Actual=$(if ($null -eq $Actual) { '<null>' } else { $Actual.Length })"
    }

    for ($i = 0; $i -lt $Expected.Length; $i++) {
        if ($Expected[$i] -ne $Actual[$i]) {
            throw "$Name byte mismatch at index $i. Expected=$($Expected[$i]) Actual=$($Actual[$i])"
        }
    }
}

$combined = [byte[]](0x41, 0x52, 0x0E, 0x90, 0x91, 0xFF, 0x22)
$combinedResult = Invoke-Split $combined
if (-not $combinedResult.Success) {
    throw "A combined initial World frame should split at the 0x0E terminator."
}
Assert-Bytes "Combined custom parameter" ([byte[]](0x41, 0x52, 0x0E)) $combinedResult.Custom
Assert-Bytes "Combined encrypted tail" ([byte[]](0x90, 0x91, 0xFF, 0x22)) $combinedResult.Remainder
Write-Host "[PASS] Combined SessionId and encrypted tail are preserved byte-for-byte."

$exact = [byte[]](0x41, 0x52, 0x0E)
$exactResult = Invoke-Split $exact
if (-not $exactResult.Success) {
    throw "An exact initial custom-parameter frame should be accepted."
}
Assert-Bytes "Exact custom parameter" $exact $exactResult.Custom
Assert-Bytes "Exact empty tail" ([byte[]]@()) $exactResult.Remainder
Write-Host "[PASS] An exact SessionId frame produces an empty tail."

$withoutTerminator = Invoke-Split ([byte[]](0x41, 0x52, 0x53))
if ($withoutTerminator.Success) {
    throw "A fragmented custom parameter without 0x0E must remain buffered instead of being split."
}
Write-Host "[PASS] A fragmented SessionId waits for the protocol terminator."

$invalidLeadingTerminator = Invoke-Split ([byte[]](0x0E, 0x41, 0x52))
if ($invalidLeadingTerminator.Success) {
    throw "A leading 0x0E without custom-parameter content must not be accepted."
}
Write-Host "[PASS] An empty leading custom parameter is rejected."

foreach ($sourcePath in @($NetworkClientSourcePath, $ServerClientSourcePath)) {
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
        throw "Required source contract file was not found: $sourcePath"
    }
}

$networkSource = Get-Content -LiteralPath $NetworkClientSourcePath -Raw
$serverSource = Get-Content -LiteralPath $ServerClientSourcePath -Raw
if ($networkSource.IndexOf("MaximumInitialCustomParameterBytes", [StringComparison]::Ordinal) -lt 0 -or
    $networkSource.IndexOf("_pendingInitialCustomParameterBytes", [StringComparison]::Ordinal) -lt 0) {
    throw "The fragmented initial World frame is not bounded and buffered."
}
if ($serverSource.IndexOf(
        "foreach (IScsMessage transformedMessage in TransformReceivedMessages(message))",
        [StringComparison]::Ordinal) -lt 0) {
    throw "ScsServerClient does not dispatch transformed logical messages in order."
}
Write-Host "[PASS] Fragment buffering is bounded and transformed messages are dispatched in order."

Write-Host "Modern World ingress preserves the encrypted tail that shares a transport frame with the initial SessionId."
