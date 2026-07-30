[CmdletBinding()]
param(
    [string]$MasterExecutable = "bin\Release\Master\NosGm.Master.Server.exe"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$repositoryRoot =
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$executablePath = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot $MasterExecutable))
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "The compiled Master executable was not found: $executablePath"
}

$assemblyDirectory = Split-Path -Parent $executablePath
$resolveHandler = [ResolveEventHandler] {
    param($sender, $eventArgs)

    $simpleName = ([Reflection.AssemblyName]::new($eventArgs.Name)).Name
    $candidate = Join-Path $assemblyDirectory ($simpleName + ".dll")
    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
        return [Reflection.Assembly]::LoadFrom($candidate)
    }
    return $null
}

[AppDomain]::CurrentDomain.add_AssemblyResolve($resolveHandler)
try {
    $assembly = [Reflection.Assembly]::LoadFrom($executablePath)
    $serviceType = $assembly.GetType(
        "NosGm.Master.Server.MirroredCommunicationService",
        $true,
        $false)
    $interfaceType = $serviceType.GetInterface(
        "NosGm.Master.Library.Interface.ICommunicationService",
        $true)
    if ($null -eq $interfaceType) {
        throw "MirroredCommunicationService does not expose ICommunicationService."
    }

    $interfaceMap = $serviceType.GetInterfaceMap($interfaceType)
    $expectedMirrorTargets = @(
        "ConnectCharacter",
        "DisconnectCharacter",
        "KickSession",
        "RefreshPenalty",
        "Restart",
        "RunGlobalEvent",
        "Shutdown",
        "UpdateBazaar",
        "UpdateFamily",
        "UpdateRelation"
    )
    $expectedLegacyTargets = @(
        "SendMessageToCharacter"
    )

    function Assert-TargetType {
        param(
            [Parameter(Mandatory = $true)][string]$MethodName,
            [Parameter(Mandatory = $true)][string]$ExpectedDeclaringType
        )

        $matches = for ($index = 0; $index -lt $interfaceMap.InterfaceMethods.Length; $index++) {
            if ($interfaceMap.InterfaceMethods[$index].Name -eq $MethodName) {
                [pscustomobject]@{
                    InterfaceMethod = $interfaceMap.InterfaceMethods[$index]
                    TargetMethod = $interfaceMap.TargetMethods[$index]
                }
            }
        }
        if (-not $matches) {
            throw "ICommunicationService method was not found: $MethodName"
        }

        foreach ($match in $matches) {
            $actualType = $match.TargetMethod.DeclaringType.FullName
            if ($actualType -ne $ExpectedDeclaringType) {
                throw (
                    "Interface dispatch for {0} targets {1}.{2}, expected {3}." -f
                    $match.InterfaceMethod,
                    $actualType,
                    $match.TargetMethod.Name,
                    $ExpectedDeclaringType)
            }
        }
        Write-Host (
            "[PASS] {0} dispatches to {1}" -f
            $MethodName,
            $ExpectedDeclaringType) -ForegroundColor Green
    }

    foreach ($methodName in $expectedMirrorTargets) {
        Assert-TargetType `
            -MethodName $methodName `
            -ExpectedDeclaringType "NosGm.Master.Server.MirroredCommunicationService"
    }
    foreach ($methodName in $expectedLegacyTargets) {
        Assert-TargetType `
            -MethodName $methodName `
            -ExpectedDeclaringType "NosGm.Master.Server.CommunicationService"
    }

    Write-Host `
        "NosGM compiled SCS callback mirror interface dispatch passed." `
        -ForegroundColor Green
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolveHandler)
}
