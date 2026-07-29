[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$manifestPath = Join-Path $repositoryRoot "contracts\cluster\v1\legacy-scs-surface.json"
$protoPath = Join-Path $repositoryRoot "contracts\cluster\v1\cluster_control.proto"
$contractProjectPath = Join-Path $repositoryRoot "Data\NosGm.Cluster.Contracts\NosGm.Cluster.Contracts.csproj"
$limitsPath = Join-Path $repositoryRoot "Data\NosGm.Cluster.Contracts\V1\ClusterProtocolLimits.cs"

function Assert-True {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Condition,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not $Condition) {
        throw "SCS transport contract verification failed: $Description"
    }

    Write-Host "[PASS] $Description" -ForegroundColor Green
}

function Read-RequiredFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required contract file was not found: $Path"
    }

    return [System.IO.File]::ReadAllText($Path)
}

function Get-InterfaceMethodNames {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Source,

        [Parameter(Mandatory = $true)]
        [string]$InterfaceName
    )

    $withoutComments = [regex]::Replace(
        $Source,
        '//.*?$|/\*.*?\*/',
        '',
        [Text.RegularExpressions.RegexOptions]::Multiline -bor
        [Text.RegularExpressions.RegexOptions]::Singleline)

    $interfaceMatch = [regex]::Match(
        $withoutComments,
        ('public\s+interface\s+' + [regex]::Escape($InterfaceName) + '\b[^{]*\{(?<body>.*?)\n\}'),
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
Assert-True ($manifest.schemaVersion -eq 1) "Manifest schema is version 1"
Assert-True ($manifest.sourceTransport -eq "SCS") "Manifest identifies the legacy SCS transport"
Assert-True ($manifest.targetTransport -eq "gRPC + Protobuf") "Manifest identifies the target transport"
Assert-True ($manifest.services.Count -eq 9) "All nine server and callback interfaces are inventoried"

$interfaceNames = @($manifest.services | ForEach-Object { $_.interface })
Assert-True `
    (($interfaceNames | Sort-Object -Unique).Count -eq $interfaceNames.Count) `
    "Interface entries are unique"

$manifestMethodCount = 0
foreach ($service in $manifest.services) {
    $sourcePath = Join-Path $repositoryRoot $service.source
    $source = Read-RequiredFile $sourcePath
    $actualMethods = @(Get-InterfaceMethodNames -Source $source -InterfaceName $service.interface)
    $expectedMethods = @($service.methods | Sort-Object -Unique)

    Assert-True `
        ($expectedMethods.Count -eq $service.methods.Count) `
        "$($service.interface) contains no duplicate method names"
    Assert-True `
        (($actualMethods -join "`n") -ceq ($expectedMethods -join "`n")) `
        "$($service.interface) matches the frozen migration inventory"

    if ($null -ne $service.legacyVersion) {
        $versionMarker = "[ScsService(Version = `"$($service.legacyVersion)`")]"
        Assert-True `
            ($source.Contains($versionMarker)) `
            "$($service.interface) preserves legacy version $($service.legacyVersion)"
    }

    $manifestMethodCount += $expectedMethods.Count
}

Assert-True ($manifestMethodCount -eq 99) "All 99 legacy RPC and callback methods are accounted for"

$proto = Read-RequiredFile $protoPath
Assert-True ($proto.Contains('syntax = "proto3";')) "The new wire contract uses proto3"
Assert-True ($proto.Contains("package nosgm.cluster.v1;")) "The wire contract is explicitly versioned"
Assert-True ($proto.Contains('option csharp_namespace = "NosGm.Cluster.Wire.V1";')) "Generated wire types use an isolated namespace"
Assert-True ($proto.Contains("service ClusterControl")) "The typed control service is declared"
Assert-True ($proto.Contains("rpc Negotiate(NegotiateRequest) returns (NegotiateResponse);")) "Version negotiation is typed"
Assert-True ($proto.Contains("rpc CheckHealth(HealthRequest) returns (HealthResponse);")) "Health checks are typed"
Assert-True (-not [regex]::IsMatch($proto, '\brpc\s+Invoke\b')) "No generic reflection-style Invoke endpoint is introduced"
Assert-True (-not [regex]::IsMatch($proto, '\bbytes\s+payload\b')) "No untyped object payload is introduced"
Assert-True (-not [regex]::IsMatch($proto, '(?i)\b(auth_key|password|pass_hash|token)\b')) "The control contract contains no credential-shaped fields"

$contractProject = Read-RequiredFile $contractProjectPath
Assert-True ($contractProject.Contains('<PackageReference Include="Google.Protobuf" Version="3.35.1" />')) "The Protobuf runtime version is pinned"
Assert-True ($contractProject.Contains('<PackageReference Include="Grpc.Core.Api" Version="2.80.0" />')) "The gRPC API version is pinned"
Assert-True ($contractProject.Contains('<PackageReference Include="Grpc.Tools" Version="2.80.0">')) "The gRPC build tools version is pinned"
Assert-True ($contractProject.Contains('<Protobuf Include="..\..\contracts\cluster\v1\cluster_control.proto"')) "The versioned schema participates in code generation"
Assert-True ($contractProject.Contains('GrpcServices="Both"')) "Typed client and server stubs are generated"

$limits = Read-RequiredFile $limitsPath
Assert-True ($limits.Contains("MaxInboundMessageBytes = 4 * 1024 * 1024")) "Inbound messages are capped at 4 MiB"
Assert-True ($limits.Contains("MaxOutboundMessageBytes = 4 * 1024 * 1024")) "Outbound messages are capped at 4 MiB"
Assert-True ($limits.Contains("MaxDeadlineMilliseconds = 60 * 1000")) "RPC deadlines are capped at 60 seconds"
Assert-True ($limits.Contains("BoundedDispatchQueueCapacity = 2048")) "The future dispatcher has a bounded queue contract"

$legacyProtocol = Read-RequiredFile (Join-Path $repositoryRoot "Data\NosGm.Core\Networking\Communication\Scs\Communication\Protocols\BinarySerialization\BinarySerializationProtocol.cs")
Assert-True ($legacyProtocol.Contains("MAX_MESSAGE_LENGTH = 128 * 1024 * 1024")) "The verifier still identifies the legacy 128 MiB SCS limit"

Write-Host "NosGM SCS replacement contract verification passed." -ForegroundColor Green
