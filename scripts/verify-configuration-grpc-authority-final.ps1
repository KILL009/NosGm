[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Get-RepoPath {
    param([Parameter(Mandatory = $true)][string]$RelativePath)
    return Join-Path $repositoryRoot $RelativePath
}

function Assert-FileExists {
    param([Parameter(Mandatory = $true)][string]$RelativePath)
    $path = Get-RepoPath $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required final Configuration file is missing: $RelativePath"
    }
}

function Assert-FileMissing {
    param([Parameter(Mandatory = $true)][string]$RelativePath)
    $path = Get-RepoPath $RelativePath
    if (Test-Path -LiteralPath $path) {
        throw "Retired Configuration migration surface still exists: $RelativePath"
    }
}

function Read-RepoText {
    param([Parameter(Mandatory = $true)][string]$RelativePath)
    Assert-FileExists $RelativePath
    return [System.IO.File]::ReadAllText((Get-RepoPath $RelativePath))
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Needle,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "$Description is missing '$Needle'."
    }
}

function Assert-NotContains {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Needle,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if ($Text.IndexOf($Needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "$Description still contains retired text '$Needle'."
    }
}

function Assert-Before {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$First,
        [Parameter(Mandatory = $true)][string]$Second,
        [Parameter(Mandatory = $true)][string]$Description
    )
    $firstIndex = $Text.IndexOf($First, [StringComparison]::Ordinal)
    $secondIndex = $Text.IndexOf($Second, [StringComparison]::Ordinal)
    if ($firstIndex -lt 0 -or $secondIndex -lt 0 -or $firstIndex -ge $secondIndex) {
        throw "$Description requires '$First' before '$Second'."
    }
}

$requiredFiles = @(
    "contracts\cluster\v1\cluster_configuration.proto",
    "contracts\cluster\v1\configuration-migration-map.json",
    "Data\NosGm.Cluster.Contracts\Configuration\V1\ClusterConfigurationContractValidator.cs",
    "Data\NosGm.Program\NosGm.Authentication.Server\Services\ClusterConfigurationService.cs",
    "Data\NosGm.Authentication.Client\Configuration\GrpcClusterConfigurationTransport.cs",
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationGrpcSubscriber.cs",
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationMasterSeedClient.cs",
    "Data\NosGm.Master.Library\Client\ConfigurationServiceClient.cs",
    "Data\NosGm.Program\NosGm.Master.Server\Program.cs",
    "Data\NosGm.Handler\PacketHandler\Family\UseFamilySkillPacketHandler.cs",
    "scripts\start-modern-login-core-local.ps1",
    "tests\NosGm.Authentication.Runtime.SelfTest\ClusterConfigurationContractSelfTest.cs"
)
foreach ($file in $requiredFiles) {
    Assert-FileExists $file
}

$retiredFiles = @(
    "Data\NosGm.Master.Library\Client\ConfigurationRollbackTransport.cs",
    "Data\NosGm.Master.Library\Client\ConfigurationClient.cs",
    "Data\NosGm.Master.Library\Interface\IConfigurationService.cs",
    "Data\NosGm.Master.Library\Interface\IConfigurationClient.cs",
    "Data\NosGm.Program\NosGm.Master.Server\ConfigurationService.cs",
    "Data\NosGm.Master.Library\Client\ConfigurationGrpcShadowMirror.cs",
    "Data\NosGm.Master.Library\Client\ConfigurationGrpcShadowOptions.cs",
    "Data\NosGm.Master.Library\Client\ConfigurationGrpcShadowSubscriberLifecycle.cs",
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationGrpcShadowSubscriber.cs",
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationAuthorityCoordinator.cs",
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationAuthorityGate.cs",
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationAuthorityOperatorOptions.cs",
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationAuthorityQualificationRuntime.cs",
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationUpdateObservationLedger.cs",
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationUpdateOverlapDeduplicationLedger.cs",
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationUpdateParityComparator.cs"
)
foreach ($file in $retiredFiles) {
    Assert-FileMissing $file
}

$proto = Read-RepoText "contracts\cluster\v1\cluster_configuration.proto"
foreach ($required in @(
    "service ClusterConfiguration",
    "rpc GetConfiguration",
    "rpc UpdateConfiguration",
    "rpc SubscribeConfigurationUpdates",
    "rpc GetConfigurationRuntimeInfo",
    "rpc RestartConfigurationRuntime"
)) {
    Assert-Contains $proto $required "ClusterConfiguration proto"
}
foreach ($forbidden in @("rpc Authenticate", "MasterAuthKey", "auth_key")) {
    Assert-NotContains $proto $forbidden "ClusterConfiguration proto"
}

$map = Get-Content -LiteralPath (Get-RepoPath "contracts\cluster\v1\configuration-migration-map.json") -Raw | ConvertFrom-Json
if ($map.schemaVersion -ne 2 -or
    $map.status -ne "complete" -or
    $map.authority -ne "gRPC" -or
    $null -ne $map.fallback) {
    throw "Configuration migration map must declare schema 2, complete gRPC authority, and fallback null."
}
$getRoles = @($map.operations | Where-Object rpc -eq "GetConfiguration")[0].callerRoles
$updateRoles = @($map.operations | Where-Object rpc -eq "UpdateConfiguration")[0].callerRoles
$subscribeRoles = @($map.operations | Where-Object rpc -eq "SubscribeConfigurationUpdates")[0].callerRoles
if (@($getRoles).Count -ne 2 -or @($getRoles) -notcontains "World" -or @($getRoles) -notcontains "Master") {
    throw "GetConfiguration final roles must be exactly World + Master."
}
if (@($updateRoles).Count -ne 2 -or @($updateRoles) -notcontains "World" -or @($updateRoles) -notcontains "Master") {
    throw "UpdateConfiguration final roles must be exactly World + Master."
}
if (@($subscribeRoles).Count -ne 1 -or @($subscribeRoles)[0] -ne "World") {
    throw "SubscribeConfigurationUpdates must remain World-only."
}

$facade = Read-RepoText "Data\NosGm.Master.Library\Client\ConfigurationServiceClient.cs"
foreach ($required in @(
    "GrpcClusterConfigurationTransport",
    "ConfigurationGrpcSubscriber",
    "AuthenticationGrpcClientOptions.Load(ClusterNodeRole.World)",
    "UpdateAsync(",
    "GetAsync(",
    "ConfigurationUpdate?.Invoke"
)) {
    Assert-Contains $facade $required "World Configuration facade"
}
foreach ($forbidden in @(
    "NosGm.SCS",
    "ScsConfiguration",
    "RollbackTransport",
    "ShadowMirror",
    "AuthorityCoordinator",
    "AcceptancePulse"
)) {
    Assert-NotContains $facade $forbidden "World Configuration facade"
}

$master = Read-RepoText "Data\NosGm.Program\NosGm.Master.Server\Program.cs"
foreach ($required in @(
    "EnsureConfigurationGrpcAuthority();",
    "ConfigurationMasterSeedClient",
    "ConfigurationRuntimeControllerIdentityOptions.Load()",
    "EnsureSeededAsync("
)) {
    Assert-Contains $master $required "Master Configuration startup"
}
Assert-NotContains $master "AddService<IConfigurationService" "Master Configuration startup"
Assert-Before $master "EnsureConfigurationGrpcAuthority();" "ScsServiceBuilder.CreateService" "Master cold-boot ordering"

$seedClient = Read-RepoText "Data\NosGm.Authentication.Client\Configuration\ConfigurationMasterSeedClient.cs"
foreach ($required in @(
    "ClusterNodeRole.Master",
    "EnsureSeededAsync(",
    "await GetAsync(",
    "current.Result == ConfigurationTransportResultCode.Success",
    "current.Result != ConfigurationTransportResultCode.Unavailable",
    "return await SeedAsync("
)) {
    Assert-Contains $seedClient $required "Master Configuration seed client"
}
Assert-NotContains $seedClient "SubscribeConfigurationUpdates" "Master Configuration seed client"
Assert-Before $seedClient "await GetAsync(" "return await SeedAsync(" "Master restart-safe seed sequence"

$service = Read-RepoText "Data\NosGm.Program\NosGm.Authentication.Server\Services\ClusterConfigurationService.cs"
foreach ($required in @(
    '"GetConfiguration"',
    'WireV1.ClusterNodeRole.World,',
    'WireV1.ClusterNodeRole.Master);',
    '"UpdateConfiguration"',
    '"SubscribeConfigurationUpdates"'
)) {
    Assert-Contains $service $required "Configuration gRPC service"
}

$validator = Read-RepoText "Data\NosGm.Cluster.Contracts\Configuration\V1\ClusterConfigurationContractValidator.cs"
foreach ($required in @(
    "ClusterNodeRole.World,",
    "ClusterNodeRole.Master);",
    "SubscribeConfigurationUpdatesRequest",
    "params ClusterNodeRole[] allowedRoles"
)) {
    Assert-Contains $validator $required "Configuration contract validator"
}

$selfTest = Read-RepoText "tests\NosGm.Authentication.Runtime.SelfTest\ClusterConfigurationContractSelfTest.cs"
foreach ($required in @(
    "Configuration Get accepts Master context",
    "Configuration Update accepts Master seed snapshot",
    "Configuration Subscribe rejects Master caller role"
)) {
    Assert-Contains $selfTest $required "Configuration contract self-test"
}

$family = Read-RepoText "Data\NosGm.Handler\PacketHandler\Family\UseFamilySkillPacketHandler.cs"
Assert-Contains $family "UpdateConfigurationObject(updated);" "Family Configuration mutation"
Assert-Contains $family "no gameplay effect was applied" "Family Configuration failure path"
Assert-Before $family "UpdateConfigurationObject(updated);" "AddStaticBuff(new StaticBuffDTO" "Family Configuration publication ordering"

$startup = Read-RepoText "scripts\start-modern-login-core-local.ps1"
foreach ($required in @(
    '"NOSGM_CONFIGURATION_GRPC_CONTROL_URL"',
    '"NOSGM_CONFIGURATION_GRPC_CONTROL_MASTER_CERT_PATH"',
    '"NOSGM_CONFIGURATION_GRPC_CONTROL_MASTER_CERT_PASSWORD"',
    '"NOSGM_CONFIGURATION_GRPC_CONTROL_TRUSTED_ROOT_CERT_PATH"',
    'NOSGM_AUTH_GRPC_MASTER_CERT_SHA256',
    'NOSGM_AUTH_GRPC_CLIENT_CERT_PATH = [string]$manifest.Clients.World.CertificatePath',
    '-Name "AuthenticationGrpc"',
    'ConfigurationAuthority = "gRPC"',
    'ConfigurationFallback = $null'
)) {
    Assert-Contains $startup $required "Local startup script"
}
foreach ($forbidden in @(
    "EnableConfigurationGrpcShadow",
    "ConfigurationAuthorityArmRequestId",
    "EnableConfigurationAuthorityEffects",
    "RequestConfigurationAuthorityRollback",
    "NOSGM_CONFIGURATION_GRPC_SHADOW_ENABLED",
    "NOSGM_CONFIGURATION_GRPC_ACCEPTANCE_PULSE_ENABLED",
    "NOSGM_CONFIGURATION_GRPC_AUTHORITY_ARM_REQUEST_ID"
)) {
    Assert-NotContains $startup $forbidden "Local startup script"
}
Assert-Before $startup '-Name "AuthenticationGrpc"' '-Name "Master"' "Local process startup order"
Assert-Before $startup '-Name "Master"' '-Name "World"' "Local process startup order"

$masterLibraryProject = Read-RepoText "Data\NosGm.Master.Library\NosGm.Master.Library.csproj"
$masterProject = Read-RepoText "Data\NosGm.Program\NosGm.Master.Server\NosGm.Master.Server.csproj"
foreach ($forbidden in @(
    "ConfigurationRollbackTransport.cs",
    "ConfigurationClient.cs",
    "IConfigurationService.cs",
    "IConfigurationClient.cs",
    "ConfigurationGrpcShadowMirror.cs",
    "ConfigurationGrpcShadowOptions.cs",
    "ConfigurationGrpcShadowSubscriberLifecycle.cs"
)) {
    Assert-NotContains $masterLibraryProject $forbidden "NosGm.Master.Library project"
}
Assert-NotContains $masterProject "ConfigurationService.cs" "NosGm.Master.Server project"

Write-Host "[PASS] Configuration SCS service, callback, rollback, shadow and selector surfaces are absent." -ForegroundColor Green
Write-Host "[PASS] Configuration authority is gRPC-only and fail-closed." -ForegroundColor Green
Write-Host "[PASS] Get/Update allow World + Master while subscription remains World-only." -ForegroundColor Green
Write-Host "[PASS] Master performs restart-safe gRPC seeding before its legacy listener starts." -ForegroundColor Green
Write-Host "[PASS] World uses the gRPC facade/subscriber and family effects publish authority first." -ForegroundColor Green
Write-Host "[PASS] Windows local startup always provisions Authentication/Configuration gRPC plus dedicated Master and World mTLS identities." -ForegroundColor Green
