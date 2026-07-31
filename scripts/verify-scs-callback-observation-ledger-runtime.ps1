[CmdletBinding()]
param(
    [string]$AssemblyDirectory = "bin\Release\Master"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$repositoryRoot =
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$assemblyPath = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot $AssemblyDirectory))
if (-not (Test-Path -LiteralPath $assemblyPath -PathType Container)) {
    throw "Compiled Master assembly directory was not found: $assemblyPath"
}

$resolveHandler = [ResolveEventHandler] {
    param($sender, $eventArgs)

    $simpleName = ([Reflection.AssemblyName]::new($eventArgs.Name)).Name
    $candidate = Join-Path $assemblyPath ($simpleName + ".dll")
    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
        return [Reflection.Assembly]::LoadFrom($candidate)
    }
    return $null
}

function Assert-Equal {
    param(
        $Expected,
        $Actual,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not [object]::Equals($Expected, $Actual)) {
        throw "$Name expected '$Expected' but received '$Actual'."
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$Name
    )

    try {
        & $Action
    }
    catch {
        Write-Host "[PASS] $Name" -ForegroundColor Green
        return
    }
    throw "$Name did not throw."
}

[AppDomain]::CurrentDomain.add_AssemblyResolve($resolveHandler)
try {
    $authenticationAssembly = [Reflection.Assembly]::LoadFrom(
        (Join-Path $assemblyPath "NosGm.Authentication.Client.dll"))
    $masterLibraryAssembly = [Reflection.Assembly]::LoadFrom(
        (Join-Path $assemblyPath "NosGm.Master.Library.dll"))

    $fingerprintType = $authenticationAssembly.GetType(
        "NosGm.Communication.Client.CommunicationCallbackSemanticFingerprint",
        $true,
        $false)
    $replayEvidenceType = $authenticationAssembly.GetType(
        "NosGm.Communication.Client.CommunicationCallbackReplayEvidence",
        $true,
        $false)
    $ledgerType = $masterLibraryAssembly.GetType(
        "NosGm.Master.Library.Client.CommunicationCallbackScsObservationLedger",
        $true,
        $false)

    $ledger = [Activator]::CreateInstance($ledgerType, @([int]2))
    Assert-Equal $false $ledger.IsWindowActive `
        "Compiled SCS ledger is inactive before stream warmup"

    $generation = "11111111-2222-3333-4444-555555555555"
    $identity = "World:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:1:Sumeria"
    $ledger.BeginWindow($identity, $generation, [uint64]0)
    Assert-Equal $true $ledger.IsWindowActive `
        "Compiled SCS ledger opens during stream warmup"
    Assert-Equal $false $ledger.IsReplayComplete `
        "Warmup does not claim replay completion"
    Assert-Throws {
        $ledger.BeginWindow($identity, $generation, [uint64]0)
    } "An active SCS window cannot be replaced silently"

    $computePenalty = $fingerprintType.GetMethod(
        "ComputePenaltyRefresh",
        [Reflection.BindingFlags]::Public -bor
        [Reflection.BindingFlags]::Static)
    $fingerprint7 = $computePenalty.Invoke($null, @([int]7))
    $fingerprint8 = $computePenalty.Invoke($null, @([int]8))
    $tryRecord = $ledgerType.GetMethod("TryRecord")
    $kindType = $tryRecord.GetParameters()[0].ParameterType
    $penaltyKind = [Enum]::Parse($kindType, "PenaltyRefresh")

    Assert-Equal $true ($ledger.TryRecord($penaltyKind, $fingerprint7)) `
        "Warmup SCS callback is retained"
    $warmup = $ledger.GetObservationSnapshot()
    Assert-Equal 1 $warmup.Count `
        "Warmup ledger contains one observation"
    Assert-Equal "Warmup" $warmup[0].Phase.ToString() `
        "Pre-barrier SCS callback is classified as warmup"
    Assert-Equal $identity $warmup[0].ProcessIdentity `
        "SCS observation preserves process identity"
    Assert-Equal $generation $warmup[0].RuntimeGenerationId `
        "SCS observation preserves runtime generation"
    Assert-Equal ([uint64]1) $warmup[0].LocalOrdinal `
        "SCS observation ordinal starts at one"

    $evidenceConstructor = $replayEvidenceType.GetConstructors(
        [Reflection.BindingFlags]::Instance -bor
        [Reflection.BindingFlags]::NonPublic) |
        Where-Object { $_.GetParameters().Count -eq 5 } |
        Select-Object -First 1
    if ($null -eq $evidenceConstructor) {
        throw "Replay evidence internal constructor was not found."
    }
    $evidence = $evidenceConstructor.Invoke(@(
        $generation,
        [uint64]3,
        [uint64]0,
        [uint32]0,
        [DateTimeOffset]::UtcNow))
    $ledger.CompleteReplay($evidence)
    Assert-Equal $true $ledger.IsReplayComplete `
        "Compiled SCS ledger crosses into live phase"
    Assert-Equal ([uint64]3) $ledger.ReplayEvidence.ReplayThroughSequence `
        "SCS ledger retains the typed replay boundary"
    Assert-Throws {
        $ledger.CompleteReplay($evidence)
    } "Duplicate replay completion fails closed"

    Assert-Equal $true ($ledger.TryRecord($penaltyKind, $fingerprint7)) `
        "First live SCS callback is retained"
    Assert-Equal $true ($ledger.TryRecord($penaltyKind, $fingerprint8)) `
        "Second live SCS callback is retained"
    $bounded = $ledger.GetObservationSnapshot()
    Assert-Equal 2 $bounded.Count `
        "Compiled SCS ledger remains at exact capacity"
    Assert-Equal ([uint64]2) $bounded[0].LocalOrdinal `
        "FIFO pressure evicts the warmup observation"
    Assert-Equal "Live" $bounded[0].Phase.ToString() `
        "Post-barrier SCS callback is classified as live"
    Assert-Equal ([uint64]3) $bounded[1].LocalOrdinal `
        "Newest live SCS observation is retained"
    Assert-Equal ([long]1) $ledger.EvictedObservations `
        "Compiled SCS ledger reports evidence eviction"
    Assert-Equal ([long]3) $ledger.ObservedCallbacks `
        "Compiled SCS ledger preserves cumulative callback count"

    $ledger.EndWindow()
    Assert-Equal $false $ledger.IsWindowActive `
        "Ending the typed stream closes SCS observation"
    Assert-Equal $false ($ledger.TryRecord($penaltyKind, $fingerprint8)) `
        "Closed SCS window ignores later callbacks"
    Assert-Equal 2 $ledger.GetObservationSnapshot().Count `
        "Closed SCS window retains its diagnostic snapshot"

    $nextGeneration = "22222222-3333-4444-5555-666666666666"
    $ledger.BeginWindow("Login", $nextGeneration, [uint64]3)
    Assert-Equal 0 $ledger.GetObservationSnapshot().Count `
        "A new SCS window clears evidence from the prior generation"
    Assert-Equal ([long]0) $ledger.ObservedCallbacks `
        "A new SCS window resets cumulative window counters"
    Assert-Equal $false $ledger.IsReplayComplete `
        "A new SCS window returns to warmup"

    Assert-Throws {
        $ledger.TryRecord($penaltyKind, "not-a-sha256") | Out-Null
    } "Malformed SCS semantic fingerprint fails closed"
    $unknownKind = [Enum]::ToObject($kindType, 2147483647)
    Assert-Throws {
        $ledger.TryRecord($unknownKind, $fingerprint8) | Out-Null
    } "Unknown SCS callback kind fails closed"
    Assert-Throws {
        [Activator]::CreateInstance($ledgerType, @([int]0)) | Out-Null
    } "Compiled SCS ledger rejects zero capacity"
    Assert-Throws {
        $tooLongIdentity = "W" * 129
        $freshLedger = [Activator]::CreateInstance($ledgerType, @([int]2))
        $freshLedger.BeginWindow(
            $tooLongIdentity,
            $nextGeneration,
            [uint64]0)
    } "Compiled SCS ledger bounds process identity"

    Write-Host `
        "NosGM compiled SCS callback observation ledger runtime passed." `
        -ForegroundColor Green
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolveHandler)
}
