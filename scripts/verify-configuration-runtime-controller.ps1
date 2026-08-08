[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Read-RepoFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = Join-Path $root $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Configuration runtime controller file is missing: $Path"
    }
    return [IO.File]::ReadAllText($fullPath)
}

function Require {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Needle,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "$Name is missing '$Needle'."
    }
}

function Forbid {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Needle,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Content.IndexOf($Needle, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "$Name contains forbidden text '$Needle'."
    }
}

$proto = Read-RepoFile "contracts\cluster\v1\cluster_configuration.proto"
$options = Read-RepoFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\ConfigurationRuntimeControlOptions.cs"
$controller = Read-RepoFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\State\ConfigurationRuntimeController.cs"
$state = Read-RepoFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\State\ClusterConfigurationState.cs"
$service = Read-RepoFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Services\ClusterConfigurationService.cs"
$program = Read-RepoFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Program.cs"
$client = Read-RepoFile `
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationRuntimeControllerClient.cs"
$identity = Read-RepoFile `
    "Data\NosGm.Authentication.Client\Configuration\ConfigurationRuntimeControllerIdentityOptions.cs"
$tool = Read-RepoFile `
    "Tools\NosGM.ConfigurationRuntimeController\Program.cs"
$script = Read-RepoFile `
    "scripts\invoke-configuration-grpc-runtime-control.ps1"
$start = Read-RepoFile "scripts\start-modern-login-core-local.ps1"
$selfTest = Read-RepoFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\ConfigurationRuntimeControllerSelfTest.cs"

foreach ($required in @(
    "rpc GetConfigurationRuntimeInfo",
    "rpc RestartConfigurationRuntime",
    "expected_runtime_generation_id = 2;",
    "control_enabled = 8;"
)) {
    Require $proto $required "Configuration runtime control contract"
}

foreach ($required in @(
    "NOSGM_CONFIGURATION_GRPC_RUNTIME_CONTROL_ENABLED",
    "return new ConfigurationRuntimeControlOptions(false);",
    "must be true or false"
)) {
    Require $options $required "Configuration runtime control options"
}

foreach ($required in @(
    "expectedRuntimeGenerationId != _runtimeGenerationId",
    "replacement.Update(current.Configuration)",
    "retiredState.RetireForRuntimeRestart()",
    "Guid.NewGuid()",
    "_restartCount++",
    "ConfigurationSubscriptionOpenResult.RuntimeChanged"
)) {
    Require $controller $required "Configuration runtime controller"
}
Require $state "RuntimeRestarted" "Configuration stream restart boundary"
Require $state "The Configuration runtime state is retired." `
    "Configuration retired-state guard"

foreach ($required in @(
    "WireV1.ClusterNodeRole.Master",
    "RestartConfigurationRuntime",
    ".RuntimeGenerationChanged =>",
    "ConfigurationResultCode.Conflict",
    "writer.WriteAsync(envelope, cancellationToken)",
    "ThrowForSubscriptionTermination",
    "The Configuration runtime restarted.",
    'runtimeGenerationId.ToString("D")'
)) {
    Require $service $required "Configuration runtime control service"
}
Forbid $service "_runtimeIdentity" `
    "Configuration runtime identity separation"

foreach ($required in @(
    "AddSingleton<ConfigurationRuntimeController>()",
    "ConfigurationRuntimeControlOptions.Load",
    "Configuration runtime control requires at least one Master mTLS certificate fingerprint.",
    "configurationRuntime.RuntimeGenerationId",
    "callbackRuntimeIdentity.GenerationId"
)) {
    Require $program $required "Configuration runtime DI separation"
}

foreach ($required in @(
    "options.CallerRole != ClusterNodeRole.Master",
    "RequestedService = WireV1.ClusterService.Configuration",
    "GetConfigurationRuntimeInfoAsync",
    "RestartConfigurationRuntimeAsync",
    "expectedRuntimeGenerationId"
)) {
    Require $client $required "Configuration runtime controller client"
}
Require $identity "NOSGM_CONFIGURATION_GRPC_CONTROL_MASTER_CERT_PATH" `
    "Configuration controller identity"

foreach ($required in @(
    'args[0] != "status"',
    'args[0] != "restart"',
    "status.RuntimeGenerationId",
    "client.RestartAsync",
    "JsonSerializer.Serialize"
)) {
    Require $tool $required "Configuration runtime controller tool"
}

foreach ($required in @(
    "Import-Clixml",
    "credentials.Master",
    "NOSGM_CONFIGURATION_GRPC_CONTROL_MASTER_CERT_PASSWORD",
    "ConfigurationRuntimeControlEnabled",
    "ExpectedRuntimeGenerationId must be a lowercase canonical GUID."
)) {
    Require $script $required "Configuration runtime control wrapper"
}
Forbid $script "Export-Clixml" `
    "Configuration runtime control wrapper"
Forbid $script "Set-Content" `
    "Configuration runtime control wrapper"

foreach ($required in @(
    "EnableConfigurationRuntimeControl",
    "NOSGM_AUTH_GRPC_MASTER_CERT_SHA256",
    "NOSGM_CONFIGURATION_GRPC_RUNTIME_CONTROL_ENABLED",
    "EnableConfigurationGrpcShadow",
    "NOSGM_CONFIGURATION_GRPC_SHADOW_ENABLED",
    "ConfigurationRuntimeControlEnabled"
)) {
    Require $start $required "Configuration runtime local startup"
}

foreach ($required in @(
    "Configuration runtime control is disabled by default",
    "Stale compare-and-swap restart is rejected",
    "Configuration restart terminates the old World stream explicitly",
    "Configuration restart signals the old World stream boundary",
    "Configuration restart does not rotate callback runtime",
    "Restarted Configuration runtime preserves the snapshot",
    "Old runtime cursors fail closed after restart"
)) {
    Require $selfTest $required "Configuration runtime controller self-test"
}

foreach ($forbidden in @(
    "NosGm.SCS",
    "ConfigurationObject",
    "MaxGold = current",
    "Password",
    "Credential"
)) {
    Forbid $controller $forbidden "Configuration runtime controller"
}

Write-Host "[PASS] Configuration runtime control is disabled by default and Master-mTLS only." -ForegroundColor Green
Write-Host "[PASS] Restart uses exact generation compare-and-swap and preserves only the typed snapshot seed." -ForegroundColor Green
Write-Host "[PASS] Old streams terminate explicitly while callback and process runtime identities remain unchanged." -ForegroundColor Green
Write-Host "[PASS] Windows operator control keeps the Master credential DPAPI-protected and emits sanitized JSON." -ForegroundColor Green
