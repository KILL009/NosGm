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
$registry = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackTypedEffectHandlerRegistry.cs"
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

Require $activation "PenaltyRefreshCutover" `
    "Explicit apply mode is restricted to PenaltyRefresh cutover"
Require $activation "IsApplyEnabled" `
    "Effect routing requires a separate explicit apply flag"
Require $activation "apply && !enabled" `
    "Effect routing cannot bypass callback subscriber activation"

Require $coordinator "CommunicationCallbackCutoverState.Armed" `
    "The coordinator preserves the armed intermediate state"
Require $coordinator "_gate.Activate" `
    "Activation delegates to the generation-safe cutover gate"
Require $coordinator "EffectRoutingEnabled" `
    "Operator status exposes effect-routing authorization"
Require $coordinator "TypedIngressReady" `
    "Operator status exposes the replay-complete ingress barrier"
Require $coordinator "CompleteReplay" `
    "Typed authority cannot apply before replay completion"
Require $coordinator "TryApply" `
    "Both transports use one atomic effect-selection method"
Require $coordinator "ObserveStreamEnded" `
    "Typed stream loss restores SCS authority"
Require $coordinator "FailClosed" `
    "Coordinator anomalies use one fail-closed path"
Require $coordinator "_gate.Rollback()" `
    "Generation drift and operator rollback restore SCS authority"
Require $coordinator "string.IsNullOrEmpty(_armRequestId)" `
    "Qualification cannot arm without an operator request"
Require $coordinator "Operator callback cutover configuration changed inside one process" `
    "Operator configuration is immutable inside one process"

Require $qualification "CommunicationCallbackActivationOptions.Load()" `
    "Terminal qualification reads explicit effect authorization"
Require $qualification "activation.IsApplyEnabled" `
    "Qualification binds the process to immutable routing authorization"
Require $qualification "coordinator.ObserveQualification" `
    "Terminal evidence can arm only through the coordinator"
Require $qualification ".RequestRollback(exception)" `
    "Qualification corruption rolls callback authority back"

Require $registry "Func<ICommunicationCallbackEnvelopeHandler>" `
    "The production typed dispatcher is resolved lazily"
Require $registry "The typed callback effect handler factory returned no handler" `
    "Missing typed effect dispatch fails closed"
Require $shadow "ObserveRuntimeGeneration(runtimeGenerationId)" `
    "A fresh typed stream performs the generation activation handshake"
Require $shadow "CompleteReplay(" `
    "The replay barrier opens typed ingress"
Require $shadow "ObserveStreamEnded" `
    "Stream closure closes typed ingress before terminal cleanup"
Require $shadow "CommunicationCallbackTypedEffectHandlerRegistry.Resolve()" `
    "Validated envelopes reach the registered effect router"
Require $extensions "GetPenaltyRefreshOperatorCutoverStatus" `
    "Lifecycle diagnostics expose immutable operator cutover state"
Require $extensions "RequestPenaltyRefreshOperatorRollback" `
    "Lifecycle diagnostics expose an explicit rollback control"

Require $legacyReceiver "CommunicationCallbackTypedEffectHandlerRegistry.Configure" `
    "The legacy process registers the typed effect dispatcher lazily"
Require $legacyReceiver "CommunicationCallbackParitySource.LegacyScs" `
    "Legacy PenaltyRefresh consumes the shared authority decision"
Require $legacyReceiver ".TryApply(" `
    "Legacy PenaltyRefresh is suppressed atomically after cutover"
Require $typedDispatcher "CommunicationCallbackParitySource.TypedGrpc" `
    "Typed PenaltyRefresh consumes the shared authority decision"
Require $typedDispatcher ".TryApply(" `
    "Typed dispatch cannot bypass the atomic authority gate"
Forbid $legacyReceiver "SendMessageToCharacter(message));\n            CommunicationCallback" `
    "SendMessageToCharacter remains outside typed cutover routing"

Require $selfTest "Qualification alone cannot arm without an operator request" `
    "Compiled self-test covers missing operator authorization"
Require $selfTest "The first new runtime generation completes activation handshake" `
    "Compiled self-test covers new-generation activation"
Require $selfTest "Typed effects remain closed until replay completion" `
    "Compiled self-test covers the replay barrier"
Require $selfTest "Atomic cutover suppresses the legacy PenaltyRefresh effect" `
    "Compiled self-test proves legacy suppression"
Require $selfTest "Activated PenaltyRefresh applies exactly once through typed gRPC" `
    "Compiled self-test proves one typed effect"
Require $selfTest "Stream loss restores PenaltyRefresh authority to SCS" `
    "Compiled self-test covers immediate stream-loss rollback"
Require $selfTest "Typed effect failure rolls authority back before another callback" `
    "Compiled self-test covers effect failure rollback"
Require $selfTest "Every unselected callback kind remains on SCS" `
    "Compiled self-test preserves unselected callback authority"

Require $documentation "routes production effects" `
    "Documentation records the production routing boundary"
Require $documentation "replay completion" `
    "Documentation records the replay-complete ingress barrier"
Require $documentation "fourth distinct runtime generation" `
    "Documentation records the new-generation activation rule"
Require $documentation "process restart" `
    "Documentation records the immutable operator request lifecycle"
Require $documentation "SendMessageToCharacter" `
    "Documentation preserves the excluded callback"

Write-Host `
    "NosGM operator PenaltyRefresh production cutover routing passed." `
    -ForegroundColor Green
