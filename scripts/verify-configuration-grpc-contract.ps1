[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$protoPath = Join-Path $repositoryRoot "contracts\cluster\v1\cluster_configuration.proto"
$mapPath = Join-Path $repositoryRoot "contracts\cluster\v1\configuration-migration-map.json"
$servicePath = Join-Path $repositoryRoot "Data\NosGm.Master.Library\Interface\IConfigurationService.cs"
$clientPath = Join-Path $repositoryRoot "Data\NosGm.Master.Library\Interface\IConfigurationClient.cs"
$projectPath = Join-Path $repositoryRoot "Data\NosGm.Cluster.Contracts\NosGm.Cluster.Contracts.csproj"
$validatorPath = Join-Path $repositoryRoot "Data\NosGm.Cluster.Contracts\Configuration\V1\ClusterConfigurationContractValidator.cs"
$selfTestPath = Join-Path $repositoryRoot "tests\NosGm.Authentication.Runtime.SelfTest\ClusterConfigurationContractSelfTest.cs"

foreach ($path in @($protoPath, $mapPath, $servicePath, $clientPath, $projectPath, $validatorPath, $selfTestPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Configuration gRPC contract file is missing: $path"
    }
}

$proto = [System.IO.File]::ReadAllText($protoPath)
foreach ($required in @(
    "service ClusterConfiguration",
    "rpc GetConfiguration",
    "rpc UpdateConfiguration",
    "message ConfigurationSnapshot",
    "import \"cluster_control.proto\""
)) {
    if (-not $proto.Contains($required)) {
        throw "cluster_configuration.proto is missing '$required'."
    }
}

foreach ($forbidden in @(
    "MasterAuthKey",
    "master_auth_key",
    "auth_key",
    "rpc Authenticate",
    "AuthenticateRequest"
)) {
    if ($proto.IndexOf($forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "cluster_configuration.proto must not carry legacy authentication material: '$forbidden'."
    }
}

$map = Get-Content -LiteralPath $mapPath -Raw | ConvertFrom-Json
if ($map.legacyInterface -ne "IConfigurationService" -or
    $map.targetService -ne "ClusterConfiguration") {
    throw "Configuration migration map identifies the wrong legacy or target service."
}

$expectedMethods = @(
    "Authenticate",
    "GetConfigurationObject",
    "UpdateConfigurationObject"
)
$mappedMethods = @($map.methods | ForEach-Object { $_.legacyMethod })
if (@(Compare-Object -ReferenceObject $expectedMethods -DifferenceObject $mappedMethods).Count -ne 0 -or
    $mappedMethods.Count -ne $expectedMethods.Count) {
    throw "Configuration migration map must own exactly Authenticate, GetConfigurationObject, and UpdateConfigurationObject."
}

$authenticate = @($map.methods | Where-Object legacyMethod -eq "Authenticate")
$get = @($map.methods | Where-Object legacyMethod -eq "GetConfigurationObject")
$update = @($map.methods | Where-Object legacyMethod -eq "UpdateConfigurationObject")
if ($authenticate.Count -ne 1 -or $authenticate[0].disposition -ne "transport_identity" -or $null -ne $authenticate[0].target) {
    throw "Authenticate must be replaced by transport identity, not a typed RPC."
}
if ($get.Count -ne 1 -or $get[0].disposition -ne "typed_rpc" -or $get[0].target -ne "GetConfiguration") {
    throw "GetConfigurationObject must map exactly to GetConfiguration."
}
if ($update.Count -ne 1 -or $update[0].disposition -ne "typed_rpc" -or $update[0].target -ne "UpdateConfiguration") {
    throw "UpdateConfigurationObject must map exactly to UpdateConfiguration."
}

$callback = $map.callbackBoundary
if ($null -eq $callback -or
    $callback.legacyInterface -ne "IConfigurationClient" -or
    $callback.legacyMethod -ne "ConfigurationUpdated" -or
    $callback.disposition -ne "deferred") {
    throw "ConfigurationUpdated must remain an explicit deferred callback boundary."
}

$legacyService = [System.IO.File]::ReadAllText($servicePath)
foreach ($method in $expectedMethods) {
    if ($legacyService.IndexOf($method + "(", [StringComparison]::Ordinal) -lt 0) {
        throw "IConfigurationService no longer contains expected legacy method '$method'; review the migration map."
    }
}

$legacyClient = [System.IO.File]::ReadAllText($clientPath)
if ($legacyClient.IndexOf("ConfigurationUpdated(", [StringComparison]::Ordinal) -lt 0) {
    throw "IConfigurationClient callback surface changed; review the deferred callback boundary."
}

$project = [System.IO.File]::ReadAllText($projectPath)
if ($project.IndexOf("cluster_configuration.proto", [StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw "NosGm.Cluster.Contracts must compile cluster_configuration.proto."
}

$validator = [System.IO.File]::ReadAllText($validatorPath)
foreach ($required in @(
    "ClusterService.Configuration",
    "ClusterNodeRole.World",
    "InvalidMaxGold",
    "InvalidExpBuffTimestamp",
    "InvalidGoldBuffTimestamp"
)) {
    if ($validator.IndexOf($required, [StringComparison]::Ordinal) -lt 0) {
        throw "Configuration contract validator is missing '$required'."
    }
}

$selfTest = [System.IO.File]::ReadAllText($selfTestPath)
foreach ($required in @(
    "[ModuleInitializer]",
    "InvalidCallerRole",
    "InvalidContext",
    "MissingConfiguration",
    "InvalidMaxGold",
    "InvalidExpBuffTimestamp",
    "InvalidGoldBuffTimestamp"
)) {
    if ($selfTest.IndexOf($required, [StringComparison]::Ordinal) -lt 0) {
        throw "Configuration contract self-test is missing '$required'."
    }
}

Write-Host "[PASS] Configuration legacy methods are completely mapped." -ForegroundColor Green
Write-Host "[PASS] MasterAuthKey is excluded from the typed Configuration protocol." -ForegroundColor Green
Write-Host "[PASS] ConfigurationUpdated remains an explicit deferred SCS callback boundary." -ForegroundColor Green
Write-Host "[PASS] Configuration contract validator and runtime self-test are wired." -ForegroundColor Green
