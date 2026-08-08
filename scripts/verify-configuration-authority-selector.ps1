[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Read-RepoFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Expected Configuration authority file is missing: $RelativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

function Require-Text {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Content.IndexOf($Expected, [StringComparison]::Ordinal) -lt 0) {
        throw "$Name is missing '$Expected'."
    }
}

function Forbid-Text {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Forbidden,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Content.IndexOf($Forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "$Name contains forbidden text '$Forbidden'."
    }
}

$gate = Read-RepoFile "Data\NosGm.Authentication.Client\Configuration\ConfigurationAuthorityGate.cs"
$coordinator = Read-RepoFile "Data\NosGm.Authentication.Client\Configuration\ConfigurationAuthorityCoordinator.cs"
$operatorOptions = Read-RepoFile "Data\NosGm.Authentication.Client\Configuration\ConfigurationAuthorityOperatorOptions.cs"
$qualificationRuntime = Read-RepoFile "Data\NosGm.Authentication.Client\Configuration\ConfigurationAuthorityQualificationRuntime.cs"
$overlap = Read-RepoFile "Data\NosGm.Authentication.Client\Configuration\ConfigurationUpdateOverlapDeduplicationLedger.cs"
$selfTest = Read-RepoFile "tests\NosGm.Authentication.Runtime.SelfTest\ConfigurationAuthorityCoordinatorSelfTest.cs"
$runtimeSelfTest = Read-RepoFile "tests\NosGm.Authentication.Runtime.SelfTest\ConfigurationAuthorityQualificationRuntimeSelfTest.cs"
$legacyClient = Read-RepoFile "Data\NosGm.Master.Library\Client\ConfigurationServiceClient.cs"
$typedLifecycle = Read-RepoFile "Data\NosGm.Master.Library\Client\ConfigurationGrpcShadowSubscriberLifecycle.cs"
$documentation = Read-RepoFile "docs\configuration-grpc-slice.md"

foreach ($required in @(
    "ScsAuthoritative = 1",
    "Armed = 2",
    "TypedGrpcAuthoritative = 3",
    "RolledBack = 4",
    "Get = 1",
    "Update = 2",
    "Callback = 3",
    "DefaultRequiredParityWindows = 3",
    "MaximumRequiredParityWindows = 16",
    "report.HasParity",
    "report.HasTerminalMismatch",
    "report.EvictedObservations == 0",
    "_qualifiedRuntimeGenerations.Contains(runtimeGenerationId)",
    "public bool Activate",
    "public bool Rollback",
    "ValidateSourceAndOperation"
)) {
    Require-Text $gate $required "Configuration joint authority gate"
}

foreach ($required in @(
    "public static ConfigurationAuthorityCoordinator Instance",
    "public bool Configure",
    "effectRoutingEnabled",
    "OperatorRollbackRequested",
    "ConfiguredProcessGenerationId",
    "ArmRequestId",
    "LastRecoveredRuntimeGenerationId",
    "StreamEndObservations",
    "ObserveQualification",
    "ObserveRuntimeGeneration",
    "CompleteRecovery",
    "ObserveStreamEnded",
    "RequestRollback",
    "TryApplyCallback",
    "ConfigurationAuthorityOperation.Callback",
    "_typedIngressReady",
    "ConfigurationAuthorityState.TypedGrpcAuthoritative",
    "ConfigurationAuthorityState.RolledBack",
    "Configuration overlap evidence reached its bounded capacity",
    "The Configuration runtime changed after typed authority activation",
    "FailClosed(exception)",
    "_overlapLedger.TryConsumeOpposite",
    "_overlapLedger.RecordApplied"
)) {
    Require-Text $coordinator $required "Configuration authority coordinator"
}

foreach ($required in @(
    "NOSGM_CONFIGURATION_GRPC_AUTHORITY_ARM_REQUEST_ID",
    "NOSGM_CONFIGURATION_GRPC_AUTHORITY_ROLLBACK_REQUESTED",
    "must be an exact lowercase canonical non-empty GUID",
    "must be true or false without surrounding whitespace",
    "cannot be requested together"
)) {
    Require-Text $operatorOptions $required "Configuration authority operator controls"
}

foreach ($required in @(
    "DefaultEvidenceCapacity = 16",
    "MaximumEvidenceCapacity = 64",
    "LinkedList<ConfigurationUpdateParityReport>",
    "TryConfigureFromEnvironment",
    "ObserveParity",
    "ObserveTypedUpdate",
    "ObserveStreamEnded",
    "FindRuntimeLocked",
    "_coordinator.ObserveQualification",
    "_coordinator.CompleteRecovery",
    "_coordinator.RequestRollback"
)) {
    Require-Text $qualificationRuntime $required "Configuration qualification runtime"
}

foreach ($required in @(
    "DefaultCapacity = 256",
    "MaximumCapacity = 4096",
    "TimeSpan.FromMinutes(10)",
    "TimeSpan.FromHours(1)",
    "LinkedList<AppliedUpdate>",
    "TryConsumeOpposite",
    "RecordApplied",
    "_pending.Count >= _capacity",
    "value.Length != 64",
    "character >= 'A' && character <= 'F'",
    "_duplicatesSuppressed++",
    "_expired++"
)) {
    Require-Text $overlap $required "Configuration overlap deduplication ledger"
}

foreach ($pureFile in @(
    @{ Content = $gate; Name = "Configuration joint authority gate" },
    @{ Content = $coordinator; Name = "Configuration authority coordinator" },
    @{ Content = $overlap; Name = "Configuration overlap deduplication ledger" }
)) {
    foreach ($forbidden in @(
        "NosGm.SCS",
        "ConfigurationServiceClient",
        "IConfigurationService",
        "Environment.GetEnvironmentVariable",
        "Task.Run",
        "System.Threading.Timer"
    )) {
        Forbid-Text $pureFile.Content $forbidden $pureFile.Name
    }
}

foreach ($forbidden in @(
    "NosGm.SCS",
    "ConfigurationServiceClient",
    "IConfigurationService",
    "Task.Run",
    "System.Threading.Timer"
)) {
    Forbid-Text $qualificationRuntime $forbidden "Configuration qualification runtime"
}

foreach ($required in @(
    "SCS owns Get, Update and callback by default",
    "Fewer than three parity runtimes cannot arm Configuration authority",
    "Three distinct parity runtimes arm Configuration authority",
    "A qualification runtime cannot activate Configuration authority",
    "A fourth runtime generation activates the joint authority gate",
    "Recovery atomically selects typed Get, Update and callback",
    "Typed semantic twin is suppressed after SCS-first overlap",
    "SCS semantic twin is suppressed after typed-first overlap",
    "Two identical occurrences remain two effects, never four",
    "Runtime drift makes Configuration rollback terminal",
    "A typed Configuration callback failure rolls back first",
    "Overlap capacity saturation fails closed to rollback",
    "Rollback suppresses a delayed SCS twin already applied by typed gRPC",
    "Unknown Configuration authority operations fail closed"
)) {
    Require-Text $selfTest $required "Configuration joint authority self-test"
}

foreach ($required in @(
    "Configuration dry-run lifecycle binds immutable controls",
    "Third Configuration parity runtime arms the dry-run gate",
    "Fourth Configuration runtime activates the dry-run handshake",
    "Disabled effect routing keeps typed ingress closed",
    "Configuration dry-run keeps callback effects on SCS",
    "Qualification runtime retains one bounded report per runtime"
)) {
    Require-Text $runtimeSelfTest $required "Configuration qualification runtime self-test"
}

foreach ($productionFile in @(
    @{ Content = $legacyClient; Name = "legacy Configuration client" },
    @{ Content = $typedLifecycle; Name = "typed Configuration subscriber lifecycle" }
)) {
    Forbid-Text $productionFile.Content "ConfigurationAuthorityCoordinator" $productionFile.Name
    Forbid-Text $productionFile.Content "ConfigurationAuthorityGate" $productionFile.Name
    Forbid-Text $productionFile.Content "ConfigurationUpdateOverlapDeduplicationLedger" $productionFile.Name
    Forbid-Text $productionFile.Content ".ShouldUse(" $productionFile.Name
    Forbid-Text $productionFile.Content ".TryApplyCallback(" $productionFile.Name
}

foreach ($required in @(
    "ConfigurationAuthorityQualificationRuntime.Instance",
    "TryConfigureFromEnvironment",
    "effectRoutingEnabled: false",
    "SCS remains the immutable production authority",
    "ObserveParity(report)"
)) {
    Require-Text $legacyClient $required "legacy Configuration lifecycle binding"
}

foreach ($required in @(
    "IConfigurationGrpcShadowStreamLifecycleObserver",
    "ObserveTypedUpdate",
    "ObserveStreamEnded",
    "ConfigurationAuthorityQualificationRuntime.Instance"
)) {
    Require-Text $typedLifecycle $required "typed Configuration lifecycle binding"
}

foreach ($required in @(
    "Authority lifecycle binding (dry-run)",
    "NOSGM_CONFIGURATION_GRPC_AUTHORITY_ARM_REQUEST_ID",
    "NOSGM_CONFIGURATION_GRPC_AUTHORITY_ROLLBACK_REQUESTED",
    "effect routing is compiled off",
    "three successful parity windows",
    "fourth, previously unqualified runtime generation",
    "Get, Update and callback",
    "first-arriving semantic copy",
    "terminal rollback",
    "SCS remains the production authority"
)) {
    Require-Text $documentation $required "Configuration gRPC slice documentation"
}

Write-Host "[PASS] Configuration Get, Update and callback share one atomic authority gate." -ForegroundColor Green
Write-Host "[PASS] Three parity runtimes and a fourth activation runtime prevent evidence reuse." -ForegroundColor Green
Write-Host "[PASS] Typed ingress remains closed until active-runtime recovery completes." -ForegroundColor Green
Write-Host "[PASS] Overlap applies one semantic effect and suppresses its opposite-source twin." -ForegroundColor Green
Write-Host "[PASS] Runtime drift, typed failure and capacity saturation roll back terminally to SCS." -ForegroundColor Green
Write-Host "[PASS] Production lifecycle observation is wired with Configuration effect routing compiled off." -ForegroundColor Green
