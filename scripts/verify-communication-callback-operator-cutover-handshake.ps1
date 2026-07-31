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

function Require-Match {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not [regex]::IsMatch(
        $Content,
        $Pattern,
        [Text.RegularExpressions.RegexOptions]::Singleline)) {
        throw "$Name does not match the required routing boundary."
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

$options = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackOperatorCutoverOptions.cs"
$coordinator = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackOperatorCutoverCoordinator.cs"
$overlap = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackOverlapDeduplicationLedger.cs"
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
$scsLedger = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackScsObservationLedger.cs"
$legacyReceiver = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationClient.cs"
$typedDispatcher = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackEnvelopeDispatcher.cs"
$selfTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackOperatorCutoverCoordinatorSelfTest.cs"
$overlapTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackOverlapDeduplicationSelfTest.cs"
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

Require $overlap "DefaultCapacity = 1024" `
    "Overlap retention has a bounded default capacity"
Require $overlap "MaximumCapacity = 4096" `
    "Overlap retention has an absolute capacity ceiling"
Require $overlap "TimeSpan.FromMinutes(10)" `
    "Overlap evidence outlives the maximum callback TTL"
Require $overlap "TryConsumeOpposite" `
    "Opposite transport twins are consumed by fingerprint"
Require $overlap "StringComparison.Ordinal" `
    "Fingerprint matching is exact and culture independent"
Require $overlap "Callback overlap evidence reached its bounded capacity" `
    "Overlap capacity cannot evict ambiguity silently"

Require $coordinator "CommunicationCallbackCutoverState.Armed" `
    "The coordinator preserves the armed intermediate state"
Require $coordinator "_gate.Activate" `
    "Activation delegates to the generation-safe cutover gate"
Require $coordinator "EffectRoutingEnabled" `
    "Operator status exposes effect-routing authorization"
Require $coordinator "TypedIngressReady" `
    "Operator status exposes the replay-complete ingress barrier"
Require $coordinator "OverlapDuplicatesSuppressed" `
    "Operator status exposes cross-transport duplicate suppression"
Require $coordinator "CompleteReplay" `
    "Typed authority cannot apply before replay completion"
Require $coordinator "TryConsumeOpposite" `
    "Production routing checks the overlap twin before application"
Require $coordinator "RecordApplied" `
    "Successful effects retain one possible transport twin"
Require $coordinator "ObserveStreamEnded" `
    "Typed stream loss restores modeled SCS authority"
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

Require $scsLedger "public string ProcessIdentity" `
    "SCS overlap can bind configuration before the first matching callback"
Require $legacyReceiver "CommunicationCallbackTypedEffectHandlerRegistry.Configure" `
    "The legacy process registers the typed effect dispatcher lazily"
Require $legacyReceiver "CommunicationCallbackActivationOptions.Load()" `
    "The first SCS overlap event binds immutable apply authorization"
Require $legacyReceiver "CommunicationCallbackParitySource.LegacyScs" `
    "Legacy PenaltyRefresh enters the shared overlap router"
Require $legacyReceiver "semanticFingerprint" `
    "Legacy PenaltyRefresh uses its semantic fingerprint"
Require-Match $legacyReceiver `
    'UpdatePenaltyLog\(int penaltyLogId\).*?\.TryApply\(\s*CommunicationCallbackParitySource\.LegacyScs.*?semanticFingerprint' `
    "Only UpdatePenaltyLog routes legacy effects through the cutover coordinator"
Require-Match $legacyReceiver `
    'SendMessageToCharacter\(SCSCharacterMessage message\)\s*\{\s*Task\.Run' `
    "SendMessageToCharacter remains on its unchanged SCS path"

Require $typedDispatcher "CommunicationCallbackParitySource.TypedGrpc" `
    "Typed PenaltyRefresh enters the shared overlap router"
Require $typedDispatcher "CommunicationCallbackSemanticFingerprint.Compute(envelope)" `
    "Typed dispatch uses the same semantic fingerprint"
Require $typedDispatcher "semanticFingerprint" `
    "Typed dispatch cannot bypass overlap deduplication"

Require $selfTest "Qualification alone cannot arm without an operator request" `
    "Compiled self-test covers missing operator authorization"
Require $selfTest "The first new runtime generation completes activation handshake" `
    "Compiled self-test covers new-generation activation"
Require $selfTest "Typed effects remain closed until replay completion" `
    "Compiled self-test covers the replay barrier"
Require $selfTest "Atomic cutover suppresses the legacy PenaltyRefresh effect" `
    "Modeled authority self-test preserves strict source selection"
Require $selfTest "Typed effect failure rolls authority back before another callback" `
    "Compiled self-test covers effect failure rollback"
Require $selfTest "Every unselected callback kind remains on SCS" `
    "Compiled self-test preserves unselected callback authority"

Require $overlapTest "SCS may win the dual-delivery race" `
    "Overlap self-test covers SCS-first arrival"
Require $overlapTest "Typed gRPC may win the dual-delivery race" `
    "Overlap self-test covers typed-first arrival"
Require $overlapTest "Out-of-order typed twin matches by semantic fingerprint" `
    "Overlap self-test covers transport order inversion"
Require $overlapTest "Second repeated typed twin consumes the second occurrence" `
    "Overlap self-test covers repeated semantic fingerprints"
Require $overlapTest "Late SCS twin is suppressed even after authority rollback" `
    "Overlap self-test covers post-rollback delayed delivery"
Require $overlapTest "A new post-rollback callback applies through SCS" `
    "Overlap self-test preserves new SCS effects after rollback"

Require $documentation "bounded overlap ledger" `
    "Documentation records the cross-transport race boundary"
Require $documentation "whichever copy arrives first" `
    "Documentation states first-arrival effect selection"
Require $documentation "replay completion" `
    "Documentation records the replay-complete ingress barrier"
Require $documentation "fourth distinct runtime generation" `
    "Documentation records the new-generation activation rule"
Require $documentation "Master-side authority lease" `
    "Documentation defers final SCS publication removal"
Require $documentation "process restart" `
    "Documentation records the immutable operator request lifecycle"
Require $documentation "SendMessageToCharacter" `
    "Documentation preserves the excluded callback"

Write-Host `
    "NosGM overlap-safe PenaltyRefresh production routing passed." `
    -ForegroundColor Green
