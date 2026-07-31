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
        throw "Expected callback qualification file was not found: $RelativePath"
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

$ledger = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackKindParityEvidenceLedger.cs"
$activation = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackActivationOptions.cs"
$lifecycle = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackSubscriberLifecycle.cs"
$legacyReceiver = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationClient.cs"
$shadowHandler = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackShadowEnvelopeHandler.cs"
$selfTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackKindParityEvidenceLedgerSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-qualification-ledger.md"

Require $ledger "DefaultCapacity = 16" `
    "Qualification evidence has a bounded default capacity"
Require $ledger "MaximumCapacity = 64" `
    "Qualification evidence has an absolute capacity ceiling"
Require $ledger "CommunicationCallbackKind.PenaltyRefresh" `
    "The first qualification ledger is restricted to PenaltyRefresh"
Require $ledger "Queue<CommunicationCallbackKindParityEvidence>" `
    "Terminal qualification evidence uses FIFO retention"
Require $ledger "_evidence.Dequeue()" `
    "The oldest qualification entry is evicted at capacity"
Require $ledger "Interlocked.Increment(ref _evictedEvidence)" `
    "Evidence eviction remains observable"
Require $ledger "EvidenceEquals" `
    "Exact runtime-generation retries are detected"
Require $ledger "public bool Invalidate()" `
    "The terminal capture runtime can invalidate ambiguous evidence"
Require $ledger "Interlocked.Exchange(ref _invalidated, 1)" `
    "Ambiguous evidence permanently invalidates the process ledger"
Require $ledger "evidence.ObservedAt <= last.ObservedAt" `
    "Terminal evidence must preserve strict chronology"
Require $ledger "Interlocked.Read(ref _evictedEvidence) != 0" `
    "Evicted qualification history cannot arm callback authority"
Require $ledger "return gate.Arm(_evidence.ToArray())" `
    "Gate qualification uses one atomic retained snapshot"
Require $ledger "if (IsInvalidated ||" `
    "Invalidated evidence cannot arm callback authority"

Require $activation "Production gRPC callback application remains blocked" `
    "Production callback effects remain blocked"
Forbid $lifecycle "CommunicationCallbackKindParityEvidenceLedger" `
    "The lifecycle does not mutate the qualification ledger directly"
Forbid $legacyReceiver "CommunicationCallbackKindParityEvidenceLedger" `
    "Legacy SCS effect dispatch is untouched"
Forbid $shadowHandler "CommunicationCallbackKindParityEvidenceLedger" `
    "Typed gRPC effect handling remains observation-only"

Require $selfTest "Three retained parity generations arm the PenaltyRefresh gate" `
    "Compiled self-test covers successful qualification retention"
Require $selfTest "A terminal mismatch inside the latest evidence window blocks qualification" `
    "Compiled self-test covers a broken parity streak"
Require $selfTest "Capacity eviction removes the oldest terminal generation" `
    "Compiled self-test covers FIFO eviction"
Require $selfTest "An evicted evidence history cannot arm callback authority" `
    "Compiled self-test refuses incomplete qualification history"
Require $selfTest "Conflicting generation evidence permanently blocks this process ledger" `
    "Compiled self-test covers evidence conflict invalidation"
Require $selfTest "SCS authority" `
    "Compiled self-test preserves SCS authority on failed qualification"

Require $documentation "intentionally in-memory" `
    "Documentation limits evidence retention to one process"
Require $documentation "terminal capture integration" `
    "Documentation records production observation capture"
Require $documentation "after the first eviction" `
    "Documentation records eviction refusal"
Require $documentation "SCS remains the only callback" `
    "Documentation preserves SCS effect authority"
Require $documentation "explicit operator-controlled arming request" `
    "Documentation defers effect activation to a coordinated slice"

Write-Host `
    "NosGM bounded PenaltyRefresh parity qualification ledger passed." `
    -ForegroundColor Green
