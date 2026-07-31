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
        throw "Expected terminal qualification file was not found: $RelativePath"
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

$context = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackTerminalObservationContext.cs"
$shadow = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackShadowEnvelopeHandler.cs"
$runtime = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackQualificationRuntime.cs"
$ledger = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackKindParityEvidenceLedger.cs"
$masterProject = Read-RequiredFile `
    "Data\NosGm.Master.Library\NosGm.Master.Library.csproj"
$scs = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackScsObservationLedger.cs"
$extensions = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackSubscriberLifecycleQualificationExtensions.cs"
$activation = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackActivationOptions.cs"
$legacyReceiver = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationClient.cs"
$terminalTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackTerminalObservationContextSelfTest.cs"
$runtimeTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackQualificationRuntimeSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-terminal-qualification-capture.md"

Require $context "[ThreadStatic]" `
    "Terminal typed evidence is limited to the synchronous callback thread"
Require $context "finally" `
    "Terminal typed evidence is cleared after callback completion"
Require $context "GetObservationSnapshot" `
    "Terminal typed observations are exposed through defensive snapshots"
Require $context "ReplayEvidence" `
    "Terminal typed evidence retains the replay boundary"

Require $shadow "private CommunicationCallbackReplayEvidence _replayEvidence" `
    "The typed shadow retains replay evidence until stream closure"
Require $shadow "new CommunicationCallbackTerminalTypedObservationWindow" `
    "The typed shadow freezes a terminal window before callback cleanup"
Require $shadow "CommunicationCallbackTerminalObservationContext.Invoke" `
    "The existing stream-ended callback receives synchronous terminal evidence"
Require $shadow "if (!wasActive || _streamEnded == null)" `
    "Repeated stream-end calls cannot duplicate terminal evidence"

Require $scs "CurrentTypedWindow" `
    "SCS closure consumes the matching synchronous typed window"
Require $scs "CreateTypedWindow" `
    "Typed terminal evidence uses the transport-neutral adapter"
Require $scs "CreateScsWindow" `
    "SCS terminal evidence uses the transport-neutral adapter"
Require $scs "TryCapturePenaltyRefresh" `
    "SCS closure records kind-local PenaltyRefresh evidence"
Require $scs "if (!_windowActive)" `
    "Repeated SCS closure cannot duplicate qualification evidence"

Require $runtime "CommunicationCallbackKind.PenaltyRefresh" `
    "The qualification runtime remains restricted to PenaltyRefresh"
Require $runtime "CommunicationCallbackKindParityComparator.Compare" `
    "Terminal capture reuses the fail-closed kind comparator"
Require $runtime "_penaltyRefreshEvidence.TryAppend" `
    "Terminal evidence enters the bounded qualification ledger"
Require $runtime "_penaltyRefreshEvidence.Invalidate" `
    "Capture corruption permanently invalidates qualification"
Require $runtime "new CommunicationCallbackCutoverGate" `
    "Qualification status evaluates only a fresh inactive gate"
Require $ledger "public bool Invalidate()" `
    "The capture runtime can fail closed without fabricating evidence"

Require $extensions "GetPenaltyRefreshQualificationStatus" `
    "Subscriber lifecycle exposes terminal qualification status"
Require $extensions "GetPenaltyRefreshQualificationEvidenceSnapshot" `
    "Subscriber lifecycle exposes bounded terminal evidence"
Require $masterProject `
    "CommunicationCallbackSubscriberLifecycleQualificationExtensions.cs" `
    "Master.Library compiles lifecycle qualification visibility"

Require $activation "Production gRPC callback application remains blocked" `
    "Production typed callback effects remain blocked"
Forbid $legacyReceiver "CommunicationCallbackQualificationRuntime" `
    "Legacy SCS effect dispatch is not gated by qualification capture"
Forbid $legacyReceiver "TypedGrpcAuthoritative" `
    "Legacy SCS effect dispatch cannot select typed authority"

Require $terminalTest "Repeated end calls cannot duplicate terminal evidence" `
    "Compiled self-test covers terminal callback idempotence"
Require $terminalTest "Terminal context cleanup survives callback failure" `
    "Compiled self-test covers thread-local cleanup"
Require $runtimeTest "Three terminal parity streams qualify the inactive cutover gate" `
    "Compiled self-test covers three-generation qualification"
Require $runtimeTest "A newer terminal mismatch breaks the three-generation parity streak" `
    "Compiled self-test covers fail-closed mismatch capture"

Require $documentation "SCS still applies every callback" `
    "Documentation preserves SCS authority"
Require $documentation "thread-local" `
    "Documentation records the synchronous handoff boundary"
Require $documentation "disabled by default" `
    "Documentation defers production activation"

Write-Host `
    "NosGM terminal PenaltyRefresh qualification capture passed." `
    -ForegroundColor Green
