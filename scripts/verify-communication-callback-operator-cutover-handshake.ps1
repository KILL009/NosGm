[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$repositoryRoot =
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Read-RequiredFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $fullPath = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Expected operator cutover file was not found: $RelativePath"
    }
    return [System.IO.File]::ReadAllText($fullPath)
}

function Require {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not $Content.Contains($Expected)) {
        throw "$Name is missing '$Expected'."
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

function Forbid {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Forbidden,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Content.Contains($Forbidden)) {
        throw "$Name contains forbidden text '$Forbidden'."
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

$options = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackOperatorCutoverOptions.cs"
$coordinator = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackOperatorCutoverCoordinator.cs"
$qualification = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackQualificationRuntime.cs"
$shadow = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackShadowEnvelopeHandler.cs"
$extensions = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackSubscriberLifecycleQualificationExtensions.cs"
$activation = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackActivationOptions.cs"
$legacyReceiver = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationClient.cs"
$typedDispatcher = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackEnvelopeDispatcher.cs"
$selfTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackOperatorCutoverCoordinatorSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-operator-cutover-handshake.md"

Require $options `
    "NOSGM_COMMUNICATION_GRPC_CALLBACKS_PENALTY_REFRESH_ARM_REQUEST_ID" `
    "PenaltyRefresh arming requires an explicit operator request ID"
Require $options `
    "NOSGM_COMMUNICATION_GRPC_CALLBACKS_PENALTY_REFRESH_ROLLBACK_REQUESTED" `
    "PenaltyRefresh exposes an explicit rollback request"
Require $options "IsCanonicalNonEmptyGuid" `
    "Operator request IDs must be canonical GUIDs"
Require $options "cannot be requested together" `
    "Arm and rollback requests are mutually exclusive"

Require $coordinator "CommunicationCallbackCutoverState.Armed" `
    "The coordinator preserves the armed intermediate state"
Require $coordinator "_gate.Activate" `
    "Activation delegates to the generation-safe cutover gate"
Require $coordinator "_gate.ActiveGeneration" `
    "Active authority is scoped to one runtime generation"
Require $coordinator "FailClosed" `
    "Coordinator anomalies use one fail-closed path"
Require $coordinator "_gate.Rollback()" `
    "Generation drift and operator rollback restore SCS authority"
Require $coordinator "string.IsNullOrEmpty(_armRequestId)" `
    "Qualification cannot arm without an operator request"
Require $coordinator "Operator callback cutover configuration changed inside one process" `
    "Operator configuration is immutable inside one process"

Require $qualification "CommunicationCallbackOperatorCutoverOptions.Load()" `
    "Terminal qualification loads the operator request"
Require $qualification "coordinator.ObserveQualification" `
    "Terminal evidence can arm only through the coordinator"
Require $qualification ".RequestRollback(exception)" `
    "Qualification corruption rolls callback authority back"
Require $shadow "ObserveRuntimeGeneration(runtimeGenerationId)" `
    "A fresh typed stream performs the generation activation handshake"
Require $extensions "GetPenaltyRefreshOperatorCutoverStatus" `
    "Lifecycle diagnostics expose immutable operator cutover state"
Require $extensions "RequestPenaltyRefreshOperatorRollback" `
    "Lifecycle diagnostics expose an explicit rollback control"

Require $activation "Production gRPC callback application remains blocked" `
    "Production typed callback effects remain blocked"
Forbid $legacyReceiver "CommunicationCallbackOperatorCutoverCoordinator" `
    "Legacy SCS effect dispatch is not gated in this slice"
Forbid $typedDispatcher "CommunicationCallbackOperatorCutoverCoordinator" `
    "Typed effect dispatch is not enabled in this slice"
Forbid $legacyReceiver "ShouldApply(" `
    "Legacy effects do not consume modeled authority yet"
Forbid $typedDispatcher "ShouldApply(" `
    "Typed effects do not consume modeled authority yet"

Require $selfTest "Qualification alone cannot arm without an operator request" `
    "Compiled self-test covers missing operator authorization"
Require $selfTest "The first new runtime generation completes activation handshake" `
    "Compiled self-test covers new-generation activation"
Require $selfTest "Generation drift makes rollback terminal for the process" `
    "Compiled self-test covers generation-scoped rollback"
Require $selfTest "Operator configuration cannot change inside one process" `
    "Compiled self-test covers immutable operator configuration"
Require $selfTest "Every unselected callback kind remains on SCS" `
    "Compiled self-test preserves unselected callback authority"

Require $documentation "does not route production effects" `
    "Documentation preserves the effect-routing boundary"
Require $documentation "fourth distinct runtime generation" `
    "Documentation records the new-generation activation rule"
Require $documentation "process restart" `
    "Documentation records the immutable operator request lifecycle"
Require $documentation "SCS remains the only production effect path" `
    "Documentation preserves SCS authority"

Write-Host `
    "NosGM operator PenaltyRefresh cutover handshake passed." `
    -ForegroundColor Green
