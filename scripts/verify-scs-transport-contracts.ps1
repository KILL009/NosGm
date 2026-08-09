[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$manifestPath = Join-Path $repositoryRoot "contracts\cluster\v1\legacy-scs-surface.json"

function Assert-True {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Description
    )

    if (-not $Condition) {
        throw "SCS transport contract verification failed: $Description"
    }
    Write-Host "[PASS] $Description" -ForegroundColor Green
}

function Read-RequiredFile {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required contract file was not found: $Path"
    }
    return [System.IO.File]::ReadAllText($Path)
}

function Get-InterfaceMethodNames {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$InterfaceName
    )

    $withoutComments = [regex]::Replace(
        $Source,
        '//.*?$|/\*.*?\*/',
        '',
        [Text.RegularExpressions.RegexOptions]::Multiline -bor
        [Text.RegularExpressions.RegexOptions]::Singleline)
    $interfaceMatch = [regex]::Match(
        $withoutComments,
        ('public\s+interface\s+' + [regex]::Escape($InterfaceName) + '\b[^{]*\{(?<body>.*?)\r?\n\s*\}'),
        [Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $interfaceMatch.Success) {
        throw "Unable to locate interface '$InterfaceName'."
    }

    $methods = New-Object System.Collections.Generic.List[string]
    foreach ($statement in $interfaceMatch.Groups["body"].Value.Split(';')) {
        if ($statement.IndexOf('(') -lt 0 -or $statement.IndexOf(')') -lt 0) {
            continue
        }
        $nameMatch = [regex]::Match($statement, '([A-Za-z_][A-Za-z0-9_]*)\s*\(')
        if ($nameMatch.Success) {
            $methods.Add($nameMatch.Groups[1].Value)
        }
    }
    return @($methods | Sort-Object -Unique)
}

$manifest = (Read-RequiredFile $manifestPath) | ConvertFrom-Json
Assert-True ($manifest.schemaVersion -eq 2) "Remaining legacy SCS manifest schema is version 2"
Assert-True ($manifest.sourceTransport -eq "SCS") "Manifest identifies SCS as the remaining legacy transport"
Assert-True ($manifest.targetTransport -eq "gRPC + Protobuf") "Manifest identifies the target transport"
Assert-True ($manifest.services.Count -eq 7) "Exactly seven non-Configuration SCS interfaces remain inventoried"

$interfaceNames = @($manifest.services | ForEach-Object { $_.interface })
Assert-True (($interfaceNames | Sort-Object -Unique).Count -eq $interfaceNames.Count) "Remaining SCS interface entries are unique"
Assert-True (@($interfaceNames | Where-Object { $_ -like 'IConfiguration*' }).Count -eq 0) "Configuration is absent from the remaining SCS inventory"

$manifestMethodCount = 0
foreach ($service in $manifest.services) {
    $sourcePath = Join-Path $repositoryRoot $service.source
    $source = Read-RequiredFile $sourcePath
    $actualMethods = @(Get-InterfaceMethodNames -Source $source -InterfaceName $service.interface)
    $expectedMethods = @($service.methods | Sort-Object -Unique)

    Assert-True ($expectedMethods.Count -eq $service.methods.Count) "$($service.interface) contains no duplicate method names"
    Assert-True (($actualMethods -join "`n") -ceq ($expectedMethods -join "`n")) "$($service.interface) matches the remaining SCS inventory"

    if ($null -ne $service.legacyVersion) {
        $versionMarker = "[ScsService(Version = `"$($service.legacyVersion)`")]"
        Assert-True ($source.Contains($versionMarker)) "$($service.interface) preserves legacy version $($service.legacyVersion)"
    }
    $manifestMethodCount += $expectedMethods.Count
}
Assert-True ($manifestMethodCount -eq 94) "All 94 remaining non-Configuration SCS methods are accounted for"

$communicationClientSource = Read-RequiredFile (Join-Path $repositoryRoot "Data\NosGm.Master.Library\Interface\ICommunicationClient.cs")
$communicationClientMethods = @(Get-InterfaceMethodNames -Source $communicationClientSource -InterfaceName "ICommunicationClient")
Assert-True ($communicationClientMethods -notcontains "UpdatePenaltyLog") "PenaltyRefresh is absent from the SCS callback interface"

foreach ($retiredPath in @(
    "Data\NosGm.Master.Library\Interface\IConfigurationService.cs",
    "Data\NosGm.Master.Library\Interface\IConfigurationClient.cs",
    "Data\NosGm.Program\NosGm.Master.Server\ConfigurationService.cs",
    "Data\NosGm.Master.Library\Client\ConfigurationClient.cs",
    "Data\NosGm.Master.Library\Client\ConfigurationRollbackTransport.cs"
)) {
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $retiredPath))) "Retired Configuration SCS surface is absent: $retiredPath"
}

$controlProto = Read-RequiredFile (Join-Path $repositoryRoot "contracts\cluster\v1\cluster_control.proto")
Assert-True ($controlProto.Contains('syntax = "proto3";')) "Cluster control wire contract uses proto3"
Assert-True ($controlProto.Contains("service ClusterControl")) "Typed cluster control service is declared"
Assert-True (-not [regex]::IsMatch($controlProto, '\brpc\s+Invoke\b')) "Cluster control introduces no generic Invoke endpoint"
Assert-True (-not [regex]::IsMatch($controlProto, '\bbytes\s+payload\b')) "Cluster control introduces no untyped object payload"

$authenticationMap = (Read-RequiredFile (Join-Path $repositoryRoot "contracts\cluster\v1\authentication-migration-map.json")) | ConvertFrom-Json
Assert-True ($authenticationMap.legacyInterface -eq "IAuthentificationService") "Authentication migration still maps the remaining legacy authentication interface"
Assert-True ($authenticationMap.targetService -eq "GameforgeAuthentication") "Authentication migration targets the typed Gameforge service"
$legacyAuthentication = @($manifest.services | Where-Object { $_.interface -eq "IAuthentificationService" })
Assert-True ($legacyAuthentication.Count -eq 1) "Legacy authentication interface remains uniquely inventoried"
Assert-True ((@($authenticationMap.methods.legacyMethod | Sort-Object -Unique) -join "`n") -ceq (@($legacyAuthentication[0].methods | Sort-Object -Unique) -join "`n")) "Authentication migration covers every remaining legacy authentication method"

$communicationMap = (Read-RequiredFile (Join-Path $repositoryRoot "contracts\cluster\v1\communication-migration-map.json")) | ConvertFrom-Json
Assert-True ($communicationMap.legacyInterface -eq "ICommunicationService") "Communication migration still maps the remaining legacy communication interface"
Assert-True ($communicationMap.targetService -eq "ClusterCommunication") "Communication migration targets the typed cluster service"
$legacyCommunication = @($manifest.services | Where-Object { $_.interface -eq "ICommunicationService" })
Assert-True ($legacyCommunication.Count -eq 1) "Legacy communication interface remains uniquely inventoried"
Assert-True ((@($communicationMap.methods.legacyMethod | Sort-Object -Unique) -join "`n") -ceq (@($legacyCommunication[0].methods | Sort-Object -Unique) -join "`n")) "Communication migration covers every remaining legacy communication method"

$contractProject = Read-RequiredFile (Join-Path $repositoryRoot "Data\NosGm.Cluster.Contracts\NosGm.Cluster.Contracts.csproj")
foreach ($schema in @(
    "cluster_control.proto",
    "gameforge_authentication.proto",
    "cluster_communication.proto",
    "cluster_configuration.proto"
)) {
    Assert-True ($contractProject.Contains($schema)) "Generated contracts include $schema"
}
Assert-True ($contractProject.Contains('GrpcServices="Both"')) "Typed client and server stubs are generated"

Write-Host "Remaining SCS transport inventory verified. Configuration and PenaltyRefresh are no longer part of that callback surface." -ForegroundColor Green
