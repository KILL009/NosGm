[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$manifestPath = Join-Path $repositoryRoot "contracts\cluster\v1\legacy-scs-surface.json"
$protoPath = Join-Path $repositoryRoot "contracts\cluster\v1\cluster_control.proto"
$authenticationMapPath = Join-Path $repositoryRoot "contracts\cluster\v1\authentication-migration-map.json"
$authenticationProtoPath = Join-Path $repositoryRoot "contracts\cluster\v1\gameforge_authentication.proto"
$contractProjectPath = Join-Path $repositoryRoot "Data\NosGm.Cluster.Contracts\NosGm.Cluster.Contracts.csproj"
$limitsPath = Join-Path $repositoryRoot "Data\NosGm.Cluster.Contracts\V1\ClusterProtocolLimits.cs"
$authenticationLimitsPath = Join-Path $repositoryRoot "Data\NosGm.Cluster.Contracts\Authentication\V1\AuthenticationContractLimits.cs"
$authenticationValidatorPath = Join-Path $repositoryRoot "Data\NosGm.Cluster.Contracts\Authentication\V1\GameforgeAuthenticationContractValidator.cs"

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

$authenticationMap = (Read-RequiredFile $authenticationMapPath) | ConvertFrom-Json
Assert-True ($authenticationMap.schemaVersion -eq 1) "Authentication migration map schema is version 1"
Assert-True ($authenticationMap.legacyInterface -eq "IAuthentificationService") "Authentication map names the frozen legacy interface"
Assert-True ($authenticationMap.targetService -eq "GameforgeAuthentication") "Authentication map names the typed target service"

$legacyAuthentication = @($manifest.services | Where-Object { $_.interface -eq "IAuthentificationService" })
Assert-True ($legacyAuthentication.Count -eq 1) "The legacy authentication interface is unique"
$legacyAuthenticationMethods = @($legacyAuthentication[0].methods | Sort-Object -Unique)
$mappedAuthenticationMethods = @($authenticationMap.methods.legacyMethod | Sort-Object -Unique)
Assert-True `
    (($mappedAuthenticationMethods -join "`n") -ceq ($legacyAuthenticationMethods -join "`n")) `
    "All eight legacy authentication methods have an explicit disposition"

$typedAuthenticationMethods = @($authenticationMap.methods | Where-Object { $_.disposition -eq "typed_rpc" })
Assert-True ($typedAuthenticationMethods.Count -eq 5) "Exactly five stateful authentication methods receive typed RPCs"
Assert-True `
    (@($typedAuthenticationMethods | Where-Object { $_.sideEffect -ne $true }).Count -eq 0) `
    "Every authentication RPC is marked as side-effecting"
$expectedAuthenticationRoles = @{
    RegisterGameforgeAuthTicket = "AuthBridge"
    ConsumeGameforgeAuthTicket = "Login"
    RegisterGameforgeWorldPermit = "Login"
    ConsumeGameforgeWorldPermit = "World"
    RevokeGameforgeWorldPermit = "Login"
}
foreach ($method in $typedAuthenticationMethods) {
    Assert-True `
        ([string]$method.allowedCallerRole -ceq $expectedAuthenticationRoles[[string]$method.legacyMethod]) `
        "$($method.legacyMethod) preserves its single authorized caller role"
}
Assert-True `
    (@($authenticationMap.methods | Where-Object { $_.disposition -eq "transport_identity" }).Count -eq 1) `
    "Legacy shared-key authentication is replaced by transport identity"
Assert-True `
    (@($authenticationMap.methods | Where-Object { $_.disposition -eq "deferred" }).Count -eq 2) `
    "Unused password-hash DTO operations stay off the new transport"

$authenticationProto = Read-RequiredFile $authenticationProtoPath
Assert-True ($authenticationProto.Contains('syntax = "proto3";')) "The authentication wire contract uses proto3"
Assert-True ($authenticationProto.Contains("service GameforgeAuthentication")) "The typed authentication service is declared"
Assert-True ($authenticationProto.Contains('import "cluster_control.proto";')) "Authentication requests reuse the versioned request context"
foreach ($method in $typedAuthenticationMethods) {
    $escapedTarget = [regex]::Escape([string]$method.target)
    Assert-True `
        ([regex]::IsMatch($authenticationProto, ('\brpc\s+' + $escapedTarget + '\s*\('))) `
        "Legacy $($method.legacyMethod) maps to typed RPC $($method.target)"
}
Assert-True (-not [regex]::IsMatch($authenticationProto, '\brpc\s+Invoke\b')) "Authentication introduces no generic Invoke endpoint"
Assert-True (-not [regex]::IsMatch($authenticationProto, '\bbytes\s+payload\b')) "Authentication introduces no untyped object payload"
Assert-True (-not [regex]::IsMatch($authenticationProto, '(?i)\b(auth_key|password|pass_hash)\b')) "Password hashes and shared authentication keys stay off the new wire"
Assert-True ($authenticationProto.Contains("Sensitive authorization material. Implementations must never log it.")) "Sensitive authorization material has an explicit no-log contract"

$contractProject = Read-RequiredFile $contractProjectPath
Assert-True ($contractProject.Contains('<PackageReference Include="Google.Protobuf" Version="3.35.1" />')) "The Protobuf runtime version is pinned"
Assert-True ($contractProject.Contains('<PackageReference Include="Grpc.Core.Api" Version="2.80.0" />')) "The gRPC API version is pinned"
Assert-True ($contractProject.Contains('<PackageReference Include="Grpc.Tools" Version="2.80.0">')) "The gRPC build tools version is pinned"
Assert-True ($contractProject.Contains('<Protobuf Include="..\..\contracts\cluster\v1\cluster_control.proto"')) "The versioned schema participates in code generation"
Assert-True ($contractProject.Contains('<Protobuf Include="..\..\contracts\cluster\v1\gameforge_authentication.proto"')) "The authentication schema participates in code generation"
Assert-True ($contractProject.Contains('GrpcServices="Both"')) "Typed client and server stubs are generated"

$limits = Read-RequiredFile $limitsPath
Assert-True ($limits.Contains("MaxInboundMessageBytes = 4 * 1024 * 1024")) "Inbound messages are capped at 4 MiB"
Assert-True ($limits.Contains("MaxOutboundMessageBytes = 4 * 1024 * 1024")) "Outbound messages are capped at 4 MiB"
Assert-True ($limits.Contains("MaxDeadlineMilliseconds = 60 * 1000")) "RPC deadlines are capped at 60 seconds"
Assert-True ($limits.Contains("BoundedDispatchQueueCapacity = 2048")) "The future dispatcher has a bounded queue contract"

$authenticationLimits = Read-RequiredFile $authenticationLimitsPath
Assert-True ($authenticationLimits.Contains("MaxAuthorizationCodeLength = 4096")) "Authorization material preserves the verified 4096-character bound"
Assert-True ($authenticationLimits.Contains("MaxCountryId = 9")) "Authentication accepts exactly the ten regional country IDs"
Assert-True ($authenticationLimits.Contains("InstallationIdLength = 36")) "Installation IDs use canonical GUID length"
Assert-True ($authenticationLimits.Contains("MaxIpAddressLength = 45")) "World permit IP bindings are length-bounded"

$authenticationValidator = Read-RequiredFile $authenticationValidatorPath
Assert-True ($authenticationValidator.Contains("ClusterNodeRole.AuthBridge")) "Only AuthBridge may issue Gameforge tickets"
Assert-True ($authenticationValidator.Contains("ClusterNodeRole.Login")) "Login owns ticket consumption and permit issue/revoke"
Assert-True ($authenticationValidator.Contains("ClusterNodeRole.World")) "Only World may consume World permits"
Assert-True ($authenticationValidator.Contains("ClusterService.Authentication")) "Authentication RPCs reject cross-service request contexts"
Assert-True ($authenticationValidator.Contains("Guid.TryParseExact")) "Installation IDs must be canonical non-empty GUIDs"
Assert-True ($authenticationValidator.Contains("IPAddress.TryParse")) "World permit IP bindings are parsed, not trusted as opaque text"

$legacyProtocol = Read-RequiredFile (Join-Path $repositoryRoot "Data\NosGm.Core\Networking\Communication\Scs\Communication\Protocols\BinarySerialization\BinarySerializationProtocol.cs")
Assert-True ($legacyProtocol.Contains("MAX_MESSAGE_LENGTH = 128 * 1024 * 1024")) "The verifier still identifies the legacy 128 MiB SCS limit"

Write-Host "NosGM SCS replacement contract verification passed." -ForegroundColor Green
