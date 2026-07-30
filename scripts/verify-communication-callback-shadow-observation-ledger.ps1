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
        throw "Expected shadow observation ledger file was not found: $RelativePath"
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
        throw "$Name does not match the required observation-ledger ordering."
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

$model = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackShadowObservation.cs"
$handler = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackShadowEnvelopeHandler.cs"
$subscriber = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\GrpcCommunicationCallbackSubscriber.cs"
$lifecycle = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackSubscriberLifecycle.cs"
$activationTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackActivationSelfTest.cs"
$ledgerTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackShadowObservationSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-shadow-observation-ledger.md"
$protocol = Read-RequiredFile `
    "contracts\cluster\v1\cluster_communication_callbacks.proto"

Require $model "ICommunicationCallbackStreamObservationContext" `
    "Observation context is an optional handler capability"
Require $model "CommunicationCallbackObservationPhase" `
    "Observation records distinguish replay from live delivery"
Require $model "CommunicationCallbackShadowObservation" `
    "Observation records are immutable typed evidence"
Require $model "CommunicationCallbackSemanticFingerprint" `
    "Semantic payload hashing has one reusable implementation"
Require $model "SHA256.Create()" `
    "Semantic fingerprints support both net481 and net10"
Require $model "semantic.PenaltyRefresh" `
    "Fingerprint builder copies typed callback payloads"
Require $model "semantic.StaticBonusRefresh" `
    "Fingerprint builder covers every typed callback kind"
Forbid $model "semantic.Target" `
    "Semantic fingerprint excludes callback routing scope"
Forbid $model "semantic.EventId" `
    "Semantic fingerprint excludes event identity"
Forbid $model "semantic.Sequence" `
    "Semantic fingerprint excludes accepted sequence"
Require $model "ReplayComplete" `
    "Control envelopes are explicitly rejected from semantic hashing"

Require $handler "DefaultObservationCapacity = 4096" `
    "Observation ledger has a bounded default"
Require $handler "MaximumObservationCapacity = 16384" `
    "Observation ledger has an absolute ceiling"
Require $handler "Queue<CommunicationCallbackShadowObservation>" `
    "Observation ledger uses a FIFO queue"
Require $handler "_observations.Dequeue()" `
    "Full observation ledger evicts the oldest entry"
Require $handler "Interlocked.Increment(ref _evictedObservations)" `
    "Observation loss remains measurable"
Require $handler "_observations.ToArray()" `
    "Observation snapshots never expose the mutable queue"
Require $handler "CommunicationCallbackObservationPhase.Replay" `
    "New streams begin in replay phase"
Require $handler "CommunicationCallbackObservationPhase.Live" `
    "Validated replay completion begins live phase"
Require $handler "The shadow handler has no active callback stream" `
    "Observation without a stream context fails closed"
Require-Match $handler `
    'lock\s*\(_syncRoot\).*?new\s+CommunicationCallbackShadowObservation.*?_observations\.Enqueue' `
    "Generation phase and queue insertion share one lock"

Require $subscriber "ICommunicationCallbackStreamObservationContext" `
    "Subscriber detects observation-aware handlers"
Require-Match $subscriber `
    '_replayTracker\.BeginStream\s*\(.*?_streamObservationContext\?\.BeginStream\s*\(' `
    "Subscriber binds observation context after replay tracking begins"
Require-Match $subscriber `
    '_replayTracker\.Complete\s*\(.*?_streamObservationContext\?\.CompleteReplay\s*\(' `
    "Subscriber changes observation phase only after barrier validation"
Require-Match $subscriber `
    'finally\s*\{\s*_streamObservationContext\?\.EndStream\(\);\s*_replayTracker\.Reset\(\)' `
    "Subscriber clears observation context on every stream exit"

Require $lifecycle "public int ObservationCapacity" `
    "Production lifecycle exposes ledger capacity"
Require $lifecycle "public long EvictedObservations" `
    "Production lifecycle exposes evidence loss"
Require $lifecycle "GetObservationSnapshot" `
    "Production lifecycle exposes defensive observation snapshots"
Require $lifecycle "RetainedObservations=" `
    "Shutdown log reports retained evidence"
Require $lifecycle "EvictedObservations=" `
    "Shutdown log reports lost evidence"

Require $activationTest "Shadow callback handler retains one bounded observation" `
    "Existing activation test now covers retained evidence"
Require $activationTest "active runtime generation" `
    "Existing activation test binds evidence to a generation"
Require $ledgerTest "Observation before the barrier is classified as replay" `
    "Ledger test covers replay phase"
Require $ledgerTest "Observation after the barrier is classified as live" `
    "Ledger test covers live phase"
Require $ledgerTest "Semantic fingerprint ignores EventId sequence and timestamps" `
    "Ledger test covers stable semantic hashing"
Require $ledgerTest "Different semantic payloads produce different fingerprints" `
    "Ledger test covers payload sensitivity"
Require $ledgerTest "Oldest observation is evicted" `
    "Ledger test covers FIFO capacity pressure"
Require $ledgerTest "Replay barrier cannot enter semantic observation fingerprints" `
    "Ledger test preserves control and data separation"

Require $documentation "SCS remains the only effect-applying callback transport" `
    "Documentation preserves SCS authority"
Require $documentation "Repeated identical payloads will share a fingerprint" `
    "Documentation records FIFO matching requirements"
Require $documentation "nonzero evictions" `
    "Documentation treats evidence loss as incomplete parity"
Require $documentation "No transport cutover is permitted" `
    "Documentation does not overstate observation evidence"

Forbid $protocol "ReportCommunicationCallbackObservation" `
    "Local observation ledger adds no acknowledgement RPC"

Write-Host `
    "NosGM bounded typed callback shadow observation ledger contracts passed." `
    -ForegroundColor Green
