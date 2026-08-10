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
        throw "Expected callback authority file was not found: $RelativePath"
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
$overlap = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackOverlapDeduplicationLedger.cs"
$qualification = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackQualificationRuntime.cs"
$shadow = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackShadowEnvelopeHandler.cs"
$registry = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackTypedEffectHandlerRegistry.cs"
$legacyReceiver = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationClient.cs"
$clientInterface = Read-RequiredFile `
    "Data\NosGm.Master.Library\Interface\ICommunicationClient.cs"
$typedDispatcher = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackEnvelopeDispatcher.cs"
$masterService = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Master.Server\MirroredCommunicationService.cs"
$migrationMap = Read-RequiredFile `
    "contracts\cluster\v1\communication-callback-migration-map.json"
$selfTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackOperatorCutoverCoordinatorSelfTest.cs"
$overlapTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackOverlapDeduplicationSelfTest.cs"

# Keep the reusable transition machinery guarded for the callback kinds that
# have not completed their authority cutover yet.
Require $options `
    "NOSGM_COMMUNICATION_GRPC_CALLBACKS_PENALTY_REFRESH_ARM_REQUEST_ID" `
    "Historical operator options remain available to compiled transition tests"
Require $options "IsCanonicalNonEmptyGuid" `
    "Operator request IDs remain strongly validated"
Require $overlap "DefaultCapacity = 1024" `
    "Overlap retention remains bounded"
Require $overlap "MaximumCapacity = 4096" `
    "Overlap retention keeps an absolute ceiling"
Require $overlap "TryConsumeOpposite" `
    "Reusable overlap deduplication remains implemented"
Require $coordinator "CommunicationCallbackCutoverState.Armed" `
    "Reusable transition coordinator preserves its armed state"
Require $coordinator "CompleteReplay" `
    "Reusable transition coordinator preserves its replay barrier"
Require $coordinator "FailClosed" `
    "Reusable transition coordinator keeps one fail-closed path"
Require $qualification "coordinator.ObserveQualification" `
    "Historical qualification evidence still exercises the coordinator"
Require $registry "Func<ICommunicationCallbackEnvelopeHandler>" `
    "Typed effect dispatcher remains lazily resolved"
Require $shadow "CommunicationCallbackTypedEffectHandlerRegistry.Resolve()" `
    "Validated envelopes still reach the registered typed dispatcher"

# PenaltyRefresh itself has graduated from that overlap model. The SCS side of
# the dual-delivery race must now be absent rather than coordinated.
Forbid $clientInterface "void UpdatePenaltyLog(int penaltyLogId);" `
    "PenaltyRefresh is absent from the SCS callback interface"
Forbid $legacyReceiver "UpdatePenaltyLog(int penaltyLogId)" `
    "PenaltyRefresh legacy SCS receiver is physically removed"
Forbid $legacyReceiver "CommunicationCallbackParitySource.LegacyScs" `
    "No PenaltyRefresh SCS effect enters the overlap coordinator"
Require $clientInterface `
    "PenaltyRefresh is gRPC-authoritative and has no SCS fallback" `
    "Dead legacy base calls fail closed instead of selecting SCS"

Require $typedDispatcher `
    "kind == WireV1.CommunicationCallbackKind.PenaltyRefresh" `
    "Typed dispatcher isolates the completed PenaltyRefresh slice"
Require $typedDispatcher "ApplyCore(envelope);" `
    "PenaltyRefresh applies directly from the typed stream"
Require $typedDispatcher "CommunicationCallbackParitySource.TypedGrpc" `
    "Unmigrated callback kinds still pass through transition routing"

Require $masterService `
    "MasterPenaltyRefreshGrpcAuthority.Instance.Publish(penaltyId)" `
    "Master RefreshPenalty routes through the final gRPC authority"
Forbid $masterService "base.RefreshPenalty(penaltyId)" `
    "Master never fans PenaltyRefresh out over SCS"
Forbid $masterService "TryPenaltyRefresh(penaltyId)" `
    "Master no longer sends a shadow PenaltyRefresh twin"
Require $masterService "EventId = eventId" `
    "Bounded gRPC retries preserve one idempotent EventId"
Require $masterService "response.AcceptedSequence > 0" `
    "Final authority requires a durable accepted sequence"
Require $masterService "no SCS callback was attempted" `
    "Publication failure is explicitly fail-closed"

Require $migrationMap '"disposition": "grpc_authoritative"' `
    "Migration map records final PenaltyRefresh authority"
Require $migrationMap '"legacySurfaceRemoved": true' `
    "Migration map records legacy callback removal"
Require $migrationMap '"fallback": null' `
    "Migration map records no PenaltyRefresh fallback"

# The compiled historical tests remain useful regression coverage for the
# transition primitives that later callback slices may reuse.
Require $selfTest "Qualification alone cannot arm without an operator request" `
    "Compiled transition self-test still covers missing authorization"
Require $selfTest "Typed effects remain closed until replay completion" `
    "Compiled transition self-test still covers replay gating"
Require $overlapTest "SCS may win the dual-delivery race" `
    "Compiled overlap test keeps SCS-first race coverage"
Require $overlapTest "Typed gRPC may win the dual-delivery race" `
    "Compiled overlap test keeps typed-first race coverage"

Write-Host `
    "NosGM PenaltyRefresh final authority passed; historical operator overlap machinery remains only as reusable transition coverage." `
    -ForegroundColor Green
