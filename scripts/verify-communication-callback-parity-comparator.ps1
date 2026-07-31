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
        throw "Expected callback parity file was not found: $RelativePath"
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

$comparator = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackParityComparator.cs"
$handler = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackShadowEnvelopeHandler.cs"
$adapter = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackParityEvidenceAdapter.cs"
$lifecycle = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackSubscriberLifecycle.cs"
$project = Read-RequiredFile `
    "Data\NosGm.Master.Library\NosGm.Master.Library.csproj"
$selfTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackParityComparatorSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-parity-comparator.md"

Require $comparator "CommunicationCallbackParityVerdict" `
    "Parity has one explicit fail-closed verdict vocabulary"
Require $comparator "CommunicationCallbackParitySource.TypedGrpc" `
    "Comparator requires the typed source in its fixed role"
Require $comparator "CommunicationCallbackParitySource.LegacyScs" `
    "Comparator requires the legacy source in its fixed role"
Require $comparator "ReplayBoundaryMismatch" `
    "Comparator verifies the shared replay boundary"
Require $comparator "IncompleteEvidence" `
    "Comparator rejects evidence after FIFO eviction"
Require $comparator "NoLiveObservations" `
    "Comparator refuses empty positive parity"
Require $comparator "typed.Kind != scs.Kind" `
    "Comparator pairs callback kinds in FIFO order"
Require $comparator "typed.SemanticFingerprint" `
    "Comparator pairs semantic payload fingerprints"
Require $comparator "typed.SourceOrdinal" `
    "Mismatch report retains the typed sequence"
Require $comparator "scs.SourceOrdinal" `
    "Mismatch report retains the SCS local ordinal"
Forbid $comparator "CommunicationCallbackEnvelopeDispatcher" `
    "Comparator cannot apply typed callback effects"
Forbid $comparator "CommunicationServiceClient" `
    "Comparator cannot invoke legacy callback effects"

Require $handler "public bool IsStreamActive" `
    "Typed observation exposes moving-window state"
Require $handler "_observations.Clear()" `
    "A new typed stream starts generation-local evidence"
Require $handler "Interlocked.Exchange(ref _evictedObservations, 0)" `
    "A new typed stream resets generation-local evictions"
Require $handler "A shadow observation stream is already active" `
    "Overlapping typed windows fail closed"

Require $adapter "CommunicationCallbackObservationPhase.Live" `
    "Typed adapter admits only post-barrier observations"
Require $adapter "CommunicationCallbackScsObservationPhase.Live" `
    "SCS adapter admits only post-barrier observations"
Require $adapter "observation.ProcessIdentity" `
    "SCS adapter verifies process identity"
Require $adapter "observation.Sequence" `
    "Typed adapter preserves global sequence"
Require $adapter "observation.LocalOrdinal" `
    "SCS adapter preserves local FIFO ordinal"
Require $project "CommunicationCallbackParityEvidenceAdapter.cs" `
    "Classic Master build compiles the parity adapter"

Require $lifecycle "public CommunicationCallbackParityReport ParityReport" `
    "Production lifecycle exposes the current or last report"
Require $lifecycle "CreateParityReport" `
    "Production lifecycle builds reports from both local ledgers"
Require $lifecycle "LogParityReport(parityReport)" `
    "A terminal lifecycle emits the automatic parity report"
Require $lifecycle "[CALLBACK_PARITY_REPORT]" `
    "Parity logs have one stable bounded marker"
Require $lifecycle "SCS remains authoritative" `
    "Runtime report preserves the effect-authority boundary"

Require $selfTest "FIFO-equivalent live SCS and gRPC callbacks reach parity" `
    "Runtime self-test covers positive FIFO parity"
Require $selfTest "Reordered semantic payloads fail parity" `
    "Runtime self-test covers ordering mismatch"
Require $selfTest "Any ledger eviction makes parity evidence incomplete" `
    "Runtime self-test covers bounded evidence loss"
Require $selfTest "An empty live window does not claim positive parity" `
    "Runtime self-test rejects vacuous parity"
Require $selfTest "Evidence from different runtime generations never pairs" `
    "Runtime self-test covers generation isolation"

Require $documentation "SCS remains the sole callback transport" `
    "Documentation preserves SCS effect authority"
Require $documentation '`Parity` is evidence, not authorization' `
    "Documentation does not authorize cutover"
Require $documentation "disabled-by-default atomic inbound activation" `
    "Documentation keeps the next gate explicit and disabled"

Write-Host `
    "NosGM bounded SCS-versus-gRPC callback parity comparator contracts passed." `
    -ForegroundColor Green
