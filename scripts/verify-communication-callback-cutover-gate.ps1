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
        throw "Expected callback cutover gate file was not found: $RelativePath"
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

$gate = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackCutoverGate.cs"
$activation = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackActivationOptions.cs"
$legacyReceiver = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationClient.cs"
$shadowHandler = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackShadowEnvelopeHandler.cs"
$selfTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackCutoverGateSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-cutover-gate.md"

Require $gate "DefaultRequiredParityWindows = 3" `
    "Cutover requires three successful parity windows"
Require $gate "MaximumRequiredParityWindows = 16" `
    "The parity qualification window count is bounded"
Require $gate "CommunicationCallbackKind.PenaltyRefresh" `
    "The first gate is restricted to PenaltyRefresh"
Require $gate "CommunicationCallbackKindParityComparator" `
    "Parity evidence is evaluated per callback kind"
Require $gate "CommunicationCallbackParityComparator.Compare" `
    "Kind-local evidence reuses the fail-closed parity comparator"
Require $gate "TypedGrpcAuthoritative" `
    "The gate models one explicit typed authority state"
Require $gate "Volatile.Write" `
    "Authority transitions are published atomically"
Require $gate "ShouldApply" `
    "Both transports can query one authority decision"
Require $gate "_qualifiedGenerations.Contains(runtimeGenerationId)" `
    "Activation cannot reuse a qualification generation"
Require $gate "RolledBack" `
    "Rollback is a terminal SCS-authoritative state"

Require $activation "Production gRPC callback application remains blocked" `
    "Production callback effects remain blocked"
Forbid $legacyReceiver "CommunicationCallbackCutoverGate" `
    "The legacy SCS receiver is not wired to the unqualified gate"
Forbid $shadowHandler "CommunicationCallbackCutoverGate" `
    "The typed shadow handler remains observation-only"

Require $selfTest "Fewer than three successful parity windows cannot arm cutover" `
    "Self-test covers the minimum qualification count"
Require $selfTest "Repeated evidence from one runtime generation cannot arm cutover" `
    "Self-test rejects generation reuse"
Require $selfTest "Activated PenaltyRefresh applies exactly once through typed gRPC" `
    "Self-test proves exactly one selected authority"
Require $selfTest "Rollback immediately restores PenaltyRefresh to SCS" `
    "Self-test proves coordinated rollback"
Require $selfTest "Unselected callback kinds always remain on SCS" `
    "Self-test preserves every unselected callback kind"

Require $documentation "foundation is deliberately not wired" `
    "Documentation preserves the production safety boundary"
Require $documentation "three distinct successful terminal windows" `
    "Documentation records the qualification requirement"
Require $documentation "PenaltyRefresh" `
    "Documentation identifies the first low-risk callback kind"

Write-Host `
    "NosGM PenaltyRefresh callback cutover gate foundation passed." `
    -ForegroundColor Green
