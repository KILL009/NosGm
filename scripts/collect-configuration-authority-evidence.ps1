[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Qualification", "LiveEffects")]
    [string]$Mode,
    [string[]]$WorldLogPath,
    [string]$OutputPath,
    [ValidateSet(3)]
    [int]$RequiredParityRuntimes = 3,
    [ValidateRange(1000, 200000)]
    [int]$MaxLogLines = 50000,
    [switch]$SelfTest
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$authorityPattern = [regex]::new(
    '\[CONFIG_GRPC_AUTHORITY_STATE\] Stage=(?<Stage>[A-Z_]+) ' +
    'Process=(?<Process>[0-9a-f-]{36}) Runtime=(?<Runtime>[^ ]*) ' +
    'State=(?<State>ScsAuthoritative|Armed|TypedGrpcAuthoritative|RolledBack) ' +
    'Effects=(?<Effects>True|False) Ready=(?<Ready>True|False) ' +
    'Blocked=(?<Blocked>True|False) Active=(?<Active>[^ ]*) ' +
    'Recovered=(?<Recovered>[^ ]*) Retained=(?<Retained>\d+) ' +
    'Accepted=(?<Accepted>\d+) Replaced=(?<Replaced>\d+) ' +
    'Evicted=(?<Evicted>\d+) PendingOverlap=(?<PendingOverlap>\d+) ' +
    'DuplicateSuppressed=(?<DuplicateSuppressed>\d+) ' +
    'StreamEnds=(?<StreamEnds>\d+)\.')
$parityPattern = [regex]::new(
    '\[CONFIG_GRPC_PARITY\] Verdict=(?<Verdict>[A-Za-z]+) ' +
    'Process=(?<Process>[0-9a-f-]{36}) Runtime=(?<Runtime>[0-9a-f-]{36}) ' +
    'Through=(?<Through>\d+) WindowStart=(?<WindowStart>\d+) ' +
    'ScsLive=(?<ScsLive>\d+) GrpcLive=(?<GrpcLive>\d+) ' +
    'Matched=(?<Matched>\d+) Recovery=(?<Recovery>\d+) ' +
    'Replay=(?<Replay>\d+) Evicted=(?<Evicted>\d+);')
$terminalParityVerdicts = @(
    "IncompleteEvidence",
    "OrderMismatch",
    "CountMismatch",
    "InvalidEvidence"
)

function Assert-Condition {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Test-CanonicalGeneration {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrEmpty($Value) -or
        $Value -cnotmatch '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') {
        return $false
    }

    $parsed = [Guid]::Empty
    return [Guid]::TryParseExact($Value, "D", [ref]$parsed) -and
        $parsed -ne [Guid]::Empty -and
        $parsed.ToString("D") -ceq $Value
}

function Convert-ToAuthorityRecord {
    param(
        [Parameter(Mandatory = $true)][Text.RegularExpressions.Match]$Match,
        [Parameter(Mandatory = $true)][long]$Ordinal
    )

    return [pscustomobject]@{
        Ordinal = $Ordinal
        Stage = $Match.Groups["Stage"].Value
        Process = $Match.Groups["Process"].Value
        Runtime = $Match.Groups["Runtime"].Value
        State = $Match.Groups["State"].Value
        Effects = [bool]::Parse($Match.Groups["Effects"].Value)
        Ready = [bool]::Parse($Match.Groups["Ready"].Value)
        Blocked = [bool]::Parse($Match.Groups["Blocked"].Value)
        Active = $Match.Groups["Active"].Value
        Recovered = $Match.Groups["Recovered"].Value
        Retained = [int]$Match.Groups["Retained"].Value
        Accepted = [long]$Match.Groups["Accepted"].Value
        Replaced = [long]$Match.Groups["Replaced"].Value
        Evicted = [long]$Match.Groups["Evicted"].Value
        PendingOverlap = [int]$Match.Groups["PendingOverlap"].Value
        DuplicateSuppressed = [long]$Match.Groups["DuplicateSuppressed"].Value
        StreamEnds = [long]$Match.Groups["StreamEnds"].Value
    }
}

function Convert-ToParityRecord {
    param(
        [Parameter(Mandatory = $true)][Text.RegularExpressions.Match]$Match,
        [Parameter(Mandatory = $true)][long]$Ordinal
    )

    return [pscustomobject]@{
        Ordinal = $Ordinal
        Verdict = $Match.Groups["Verdict"].Value
        Process = $Match.Groups["Process"].Value
        Runtime = $Match.Groups["Runtime"].Value
        Through = [uint64]$Match.Groups["Through"].Value
        WindowStart = [uint64]$Match.Groups["WindowStart"].Value
        ScsLive = [int]$Match.Groups["ScsLive"].Value
        GrpcLive = [int]$Match.Groups["GrpcLive"].Value
        Matched = [int]$Match.Groups["Matched"].Value
        Recovery = [int]$Match.Groups["Recovery"].Value
        Replay = [int]$Match.Groups["Replay"].Value
        Evicted = [long]$Match.Groups["Evicted"].Value
    }
}

function Read-ConfigurationEvidence {
    param([Parameter(Mandatory = $true)][string[]]$Lines)

    $authority = New-Object Collections.Generic.List[object]
    $parity = New-Object Collections.Generic.List[object]
    $ordinal = 0L
    foreach ($line in $Lines) {
        $ordinal++
        $authorityMatch = $authorityPattern.Match([string]$line)
        if ($authorityMatch.Success) {
            [void]$authority.Add((Convert-ToAuthorityRecord -Match $authorityMatch -Ordinal $ordinal))
        }

        $parityMatch = $parityPattern.Match([string]$line)
        if ($parityMatch.Success) {
            [void]$parity.Add((Convert-ToParityRecord -Match $parityMatch -Ordinal $ordinal))
        }
    }

    return [pscustomobject]@{
        Authority = $authority.ToArray()
        Parity = $parity.ToArray()
        MarkerLines = $authority.Count + $parity.Count
    }
}

function Get-QualifyingParityRuntimes {
    param(
        [Parameter(Mandatory = $true)][object[]]$Parity,
        [Parameter(Mandatory = $true)][string]$Process
    )

    $byRuntime = @{}
    $runtimeOrder = New-Object Collections.Generic.List[string]
    foreach ($record in @($Parity | Sort-Object Ordinal)) {
        if ($record.Process -cne $Process -or
            $record.Verdict -cne "Parity" -or
            $record.WindowStart -le 0 -or
            $record.Through -lt $record.WindowStart -or
            $record.ScsLive -le 0 -or
            $record.ScsLive -ne $record.GrpcLive -or
            $record.ScsLive -ne $record.Matched -or
            $record.Evicted -ne 0) {
            continue
        }
        if (-not $byRuntime.ContainsKey($record.Runtime)) {
            [void]$runtimeOrder.Add($record.Runtime)
        }
        $byRuntime[$record.Runtime] = $record
    }

    $result = New-Object Collections.Generic.List[object]
    foreach ($runtime in $runtimeOrder) {
        [void]$result.Add($byRuntime[$runtime])
    }
    return $result.ToArray()
}

function Assert-CommonEvidence {
    param(
        [Parameter(Mandatory = $true)]$Evidence,
        [Parameter(Mandatory = $true)][int]$RequiredRuntimes
    )

    Assert-Condition ($Evidence.Authority.Count -gt 0) `
        "No Configuration authority-state records were found."
    Assert-Condition ($Evidence.Parity.Count -gt 0) `
        "No Configuration parity records were found."

    $observedProcesses = @()
    $observedProcesses += @($Evidence.Authority | ForEach-Object { $_.Process })
    $observedProcesses += @($Evidence.Parity | ForEach-Object { $_.Process })
    $processes = @(
        $observedProcesses |
        Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
        Select-Object -Unique
    )
    Assert-Condition ($processes.Count -eq 1) `
        "Evidence must belong to exactly one World process generation."
    $process = [string]$processes[0]
    Assert-Condition (Test-CanonicalGeneration $process) `
        "The World process generation is malformed."

    $terminal = @($Evidence.Parity | Where-Object {
        $_.Process -ceq $process -and
        $terminalParityVerdicts -ccontains $_.Verdict
    })
    Assert-Condition ($terminal.Count -eq 0) `
        "Terminal Configuration parity evidence is present."

    foreach ($record in @($Evidence.Authority)) {
        Assert-Condition (Test-CanonicalGeneration $record.Process) `
            "An authority-state process generation is malformed."
        foreach ($generation in @($record.Runtime, $record.Active, $record.Recovered)) {
            if (-not [string]::IsNullOrEmpty([string]$generation)) {
                Assert-Condition (Test-CanonicalGeneration ([string]$generation)) `
                    "An authority-state runtime generation is malformed."
            }
        }
    }
    foreach ($record in @($Evidence.Parity)) {
        Assert-Condition (Test-CanonicalGeneration $record.Process) `
            "A parity process generation is malformed."
        Assert-Condition (Test-CanonicalGeneration $record.Runtime) `
            "A parity runtime generation is malformed."
    }

    $qualifying = @(Get-QualifyingParityRuntimes `
        -Parity @($Evidence.Parity) `
        -Process $process)
    Assert-Condition ($qualifying.Count -ge $RequiredRuntimes) `
        "Fewer than $RequiredRuntimes distinct qualifying parity runtimes were found."

    return [pscustomobject]@{
        Process = $process
        Qualifying = @($qualifying | Select-Object -First $RequiredRuntimes)
    }
}

function New-QualificationReceipt {
    param(
        [Parameter(Mandatory = $true)]$Evidence,
        [Parameter(Mandatory = $true)][int]$RequiredRuntimes,
        [Parameter(Mandatory = $true)][int]$SourceCount
    )

    $common = Assert-CommonEvidence -Evidence $Evidence -RequiredRuntimes $RequiredRuntimes
    $qualifiedIds = @($common.Qualifying | ForEach-Object { [string]$_.Runtime })
    $latest = @($Evidence.Authority | Sort-Object Ordinal | Select-Object -Last 1)[0]

    Assert-Condition ($latest.State -ceq "TypedGrpcAuthoritative") `
        "Dry-run evidence did not reach typed authority state."
    Assert-Condition (-not $latest.Effects) `
        "Qualification evidence must keep live effects disabled."
    Assert-Condition (-not $latest.Ready) `
        "Qualification evidence unexpectedly opened typed ingress."
    Assert-Condition (-not $latest.Blocked) `
        "Qualification evidence is blocked."
    Assert-Condition ($latest.Runtime -ceq $latest.Active) `
        "The latest runtime is not the active fourth runtime."
    Assert-Condition ($latest.Active -ceq $latest.Recovered) `
        "The dry-run active runtime did not complete recovery."
    Assert-Condition (Test-CanonicalGeneration $latest.Active) `
        "The dry-run activation runtime is missing or malformed."
    Assert-Condition ($qualifiedIds -cnotcontains $latest.Active) `
        "The activation runtime must be distinct from all qualification runtimes."
    Assert-Condition ($latest.Retained -ge $RequiredRuntimes) `
        "The authority state retained too few qualification runtimes."
    Assert-Condition ($latest.Accepted -ge $RequiredRuntimes) `
        "The authority state accepted too few qualification reports."
    Assert-Condition ($latest.Evicted -eq 0) `
        "Qualification evidence contains evictions."

    return [ordered]@{
        schemaVersion = 1
        evidenceType = "configuration-authority-qualification"
        verdict = "pass"
        processGenerationId = $common.Process
        requiredParityRuntimes = $RequiredRuntimes
        qualifyingRuntimeGenerationIds = $qualifiedIds
        activationRuntimeGenerationId = $latest.Active
        authority = [ordered]@{
            state = $latest.State
            effectsEnabled = $latest.Effects
            typedIngressReady = $latest.Ready
            blocked = $latest.Blocked
            retainedRuntimes = $latest.Retained
            acceptedReports = $latest.Accepted
            evictedReports = $latest.Evicted
        }
        collection = [ordered]@{
            sourceCount = $SourceCount
            markerLines = $Evidence.MarkerLines
        }
    }
}

function New-LiveEffectsReceipt {
    param(
        [Parameter(Mandatory = $true)]$Evidence,
        [Parameter(Mandatory = $true)][int]$RequiredRuntimes,
        [Parameter(Mandatory = $true)][int]$SourceCount
    )

    $common = Assert-CommonEvidence -Evidence $Evidence -RequiredRuntimes $RequiredRuntimes
    $qualifiedIds = @($common.Qualifying | ForEach-Object { [string]$_.Runtime })
    $states = @($Evidence.Authority | Sort-Object Ordinal)
    $ready = @($states | Where-Object {
        $_.State -ceq "TypedGrpcAuthoritative" -and
        $_.Effects -and $_.Ready -and -not $_.Blocked -and
        $_.Runtime -ceq $_.Active -and $_.Active -ceq $_.Recovered
    } | Select-Object -Last 1)
    Assert-Condition ($ready.Count -eq 1) `
        "No effect-authorized recovered typed-ingress state was found."
    $active = $ready[0]
    Assert-Condition (Test-CanonicalGeneration $active.Active) `
        "The live activation runtime is missing or malformed."
    Assert-Condition ($qualifiedIds -cnotcontains $active.Active) `
        "The live activation runtime must be distinct from qualification runtimes."
    Assert-Condition ($active.Retained -ge $RequiredRuntimes) `
        "The live authority state retained too few qualification runtimes."
    Assert-Condition ($active.Accepted -ge $RequiredRuntimes) `
        "The live authority state accepted too few qualification reports."
    Assert-Condition ($active.Evicted -eq 0) `
        "Live qualification evidence contains evictions."

    $rollback = @($states | Where-Object {
        $_.Ordinal -gt $active.Ordinal -and
        $_.State -ceq "RolledBack" -and
        $_.Effects -and -not $_.Ready -and $_.Blocked -and
        $_.StreamEnds -gt $active.StreamEnds -and
        $_.DuplicateSuppressed -ge 2
    } | Select-Object -Last 1)
    Assert-Condition ($rollback.Count -eq 1) `
        "No terminal rollback with bounded duplicate suppression was found after typed ingress."
    $final = @($states | Select-Object -Last 1)[0]
    Assert-Condition ($final.State -ceq "RolledBack") `
        "The final observed authority state is not rolled back to SCS."
    Assert-Condition ($final.Blocked -and -not $final.Ready -and $final.Effects) `
        "The final rollback state has inconsistent effect, ready or blocked flags."
    Assert-Condition ($final.DuplicateSuppressed -ge 2) `
        "The final rollback state did not preserve duplicate suppression evidence."
    Assert-Condition ($final.StreamEnds -ge 1) `
        "The final rollback state did not preserve terminal stream evidence."
    Assert-Condition ($final.Evicted -eq 0) `
        "The final authority evidence contains evictions."

    return [ordered]@{
        schemaVersion = 1
        evidenceType = "configuration-authority-live-effects"
        verdict = "pass"
        processGenerationId = $common.Process
        requiredParityRuntimes = $RequiredRuntimes
        qualifyingRuntimeGenerationIds = $qualifiedIds
        activationRuntimeGenerationId = $active.Active
        authority = [ordered]@{
            activeState = $active.State
            activeTypedIngressReady = $active.Ready
            finalState = $final.State
            finalTypedIngressReady = $final.Ready
            blocked = $final.Blocked
            duplicatesSuppressed = $final.DuplicateSuppressed
            streamEndObservations = $final.StreamEnds
            evictedReports = $final.Evicted
        }
        collection = [ordered]@{
            sourceCount = $SourceCount
            markerLines = $Evidence.MarkerLines
        }
    }
}

function New-EvidenceReceipt {
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceMode,
        [Parameter(Mandatory = $true)]$Evidence,
        [Parameter(Mandatory = $true)][int]$RequiredRuntimes,
        [Parameter(Mandatory = $true)][int]$SourceCount
    )

    if ($EvidenceMode -ceq "Qualification") {
        return New-QualificationReceipt `
            -Evidence $Evidence `
            -RequiredRuntimes $RequiredRuntimes `
            -SourceCount $SourceCount
    }
    return New-LiveEffectsReceipt `
        -Evidence $Evidence `
        -RequiredRuntimes $RequiredRuntimes `
        -SourceCount $SourceCount
}

function Write-EvidenceReceiptAtomically {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Json
    )

    $stream = $null
    $writer = $null
    try {
        $stream = [IO.File]::Open(
            $Path,
            [IO.FileMode]::CreateNew,
            [IO.FileAccess]::Write,
            [IO.FileShare]::None)
        $writer = New-Object IO.StreamWriter(
            $stream,
            (New-Object Text.UTF8Encoding($false)))
        $writer.Write($Json + [Environment]::NewLine)
        $writer.Flush()
    }
    catch [IO.IOException] {
        throw "The evidence receipt could not be created atomically; choose a new output path and retry."
    }
    finally {
        if ($null -ne $writer) {
            $writer.Dispose()
        }
        elseif ($null -ne $stream) {
            $stream.Dispose()
        }
    }
}

function Resolve-WorldLogPaths {
    if ($null -ne $WorldLogPath -and @($WorldLogPath).Count -gt 0) {
        $resolved = New-Object Collections.Generic.List[string]
        foreach ($path in @($WorldLogPath)) {
            $fullPath = [IO.Path]::GetFullPath($path)
            Assert-Condition (Test-Path -LiteralPath $fullPath -PathType Leaf) `
                "World log does not exist: $fullPath"
            [void]$resolved.Add($fullPath)
        }
        return $resolved.ToArray()
    }

    $statePath = Join-Path $root "artifacts\modern-login-local\processes.json"
    Assert-Condition (Test-Path -LiteralPath $statePath -PathType Leaf) `
        "No World log path was supplied and the modern Login runtime state is missing."
    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    Assert-Condition ($state.SchemaVersion -eq 1) `
        "The modern Login runtime state schema is unsupported."
    $world = @($state.Processes | Where-Object { $_.Name -ceq "World" })
    Assert-Condition ($world.Count -eq 1) `
        "The runtime state must contain exactly one World process."
    $worldDirectory = Split-Path -Parent ([string]$world[0].Executable)
    $defaultPath = Join-Path $worldDirectory "nosgm-world.log"
    Assert-Condition (Test-Path -LiteralPath $defaultPath -PathType Leaf) `
        "The active World production log was not found. Supply -WorldLogPath explicitly."
    return @($defaultPath)
}

function Invoke-CollectorSelfTest {
    $process = "71000000-0000-0000-0000-000000000001"
    $runtimes = @(
        "72000000-0000-0000-0000-000000000001",
        "72000000-0000-0000-0000-000000000002",
        "72000000-0000-0000-0000-000000000003",
        "72000000-0000-0000-0000-000000000004"
    )
    $lines = New-Object Collections.Generic.List[string]
    for ($index = 0; $index -lt 3; $index++) {
        $ordinal = 10 + ($index * 10)
        [void]$lines.Add(
            "[CONFIG_GRPC_PARITY] Verdict=Parity Process=$process Runtime=$($runtimes[$index]) Through=$($ordinal + 1) WindowStart=$ordinal ScsLive=1 GrpcLive=1 Matched=1 Recovery=1 Replay=0 Evicted=0; authority selection is evaluated separately.")
    }
    [void]$lines.Add(
        "[CONFIG_GRPC_AUTHORITY_STATE] Stage=TYPED_RECOVERY Process=$process Runtime=$($runtimes[3]) State=TypedGrpcAuthoritative Effects=False Ready=False Blocked=False Active=$($runtimes[3]) Recovered=$($runtimes[3]) Retained=3 Accepted=3 Replaced=0 Evicted=0 PendingOverlap=0 DuplicateSuppressed=0 StreamEnds=0.")
    $qualificationEvidence = Read-ConfigurationEvidence -Lines $lines.ToArray()
    $qualification = New-EvidenceReceipt `
        -EvidenceMode "Qualification" `
        -Evidence $qualificationEvidence `
        -RequiredRuntimes 3 `
        -SourceCount 1
    Assert-Condition ($qualification.verdict -ceq "pass") `
        "Qualification self-test did not pass."

    $liveLines = New-Object Collections.Generic.List[string]
    foreach ($line in $lines | Select-Object -First 3) {
        [void]$liveLines.Add($line)
    }
    [void]$liveLines.Add(
        "[CONFIG_GRPC_AUTHORITY_STATE] Stage=TYPED_RECOVERY Process=$process Runtime=$($runtimes[3]) State=TypedGrpcAuthoritative Effects=True Ready=True Blocked=False Active=$($runtimes[3]) Recovered=$($runtimes[3]) Retained=3 Accepted=3 Replaced=0 Evicted=0 PendingOverlap=1 DuplicateSuppressed=0 StreamEnds=0.")
    [void]$liveLines.Add(
        "[CONFIG_GRPC_AUTHORITY_STATE] Stage=STREAM_ENDED Process=$process Runtime=$($runtimes[3]) State=RolledBack Effects=True Ready=False Blocked=True Active= Recovered=$($runtimes[3]) Retained=3 Accepted=3 Replaced=0 Evicted=0 PendingOverlap=0 DuplicateSuppressed=2 StreamEnds=1.")
    $liveEvidence = Read-ConfigurationEvidence -Lines $liveLines.ToArray()
    $live = New-EvidenceReceipt `
        -EvidenceMode "LiveEffects" `
        -Evidence $liveEvidence `
        -RequiredRuntimes 3 `
        -SourceCount 1
    Assert-Condition ($live.authority.duplicatesSuppressed -eq 2) `
        "Live-effects self-test lost duplicate suppression evidence."

    $crossProcess = @($liveLines.ToArray())
    $crossProcess += $crossProcess[0].Replace(
        $process,
        "71000000-0000-0000-0000-000000000002")
    $rejected = $false
    try {
        $invalidEvidence = Read-ConfigurationEvidence -Lines $crossProcess
        [void](New-EvidenceReceipt `
            -EvidenceMode "LiveEffects" `
            -Evidence $invalidEvidence `
            -RequiredRuntimes 3 `
            -SourceCount 1)
    }
    catch {
        $rejected = $true
    }
    Assert-Condition $rejected `
        "Cross-process evidence was not rejected."

    $terminalLines = @($liveLines.ToArray())
    $terminalLines +=
        "[CONFIG_GRPC_PARITY] Verdict=OrderMismatch Process=$process Runtime=$($runtimes[3]) Through=50 WindowStart=40 ScsLive=1 GrpcLive=1 Matched=0 Recovery=1 Replay=0 Evicted=0; authority selection is evaluated separately."
    $rejected = $false
    try {
        $terminalEvidence = Read-ConfigurationEvidence -Lines $terminalLines
        [void](New-EvidenceReceipt `
            -EvidenceMode "LiveEffects" `
            -Evidence $terminalEvidence `
            -RequiredRuntimes 3 `
            -SourceCount 1)
    }
    catch {
        $rejected = $true
    }
    Assert-Condition $rejected `
        "Terminal parity evidence was not rejected."

    $unsuppressedLines = @($liveLines.ToArray() | ForEach-Object {
        $_.Replace("DuplicateSuppressed=2", "DuplicateSuppressed=0")
    })
    $rejected = $false
    try {
        $unsuppressedEvidence = Read-ConfigurationEvidence -Lines $unsuppressedLines
        [void](New-EvidenceReceipt `
            -EvidenceMode "LiveEffects" `
            -Evidence $unsuppressedEvidence `
            -RequiredRuntimes 3 `
            -SourceCount 1)
    }
    catch {
        $rejected = $true
    }
    Assert-Condition $rejected `
        "Live evidence without duplicate suppression was not rejected."

    $atomicDirectory = Join-Path (
        [IO.Path]::GetTempPath()) (
        "nosgm-configuration-authority-" + [Guid]::NewGuid().ToString("N"))
    $atomicPath = Join-Path $atomicDirectory "receipt.json"
    New-Item -ItemType Directory -Path $atomicDirectory | Out-Null
    try {
        $atomicJson = '{"schemaVersion":1,"verdict":"pass"}'
        Write-EvidenceReceiptAtomically -Path $atomicPath -Json $atomicJson
        Assert-Condition ((Get-Content -LiteralPath $atomicPath -Raw).Trim() -ceq $atomicJson) `
            "Atomic receipt creation changed the receipt payload."

        $rejected = $false
        try {
            Write-EvidenceReceiptAtomically -Path $atomicPath -Json $atomicJson
        }
        catch {
            $rejected = $true
        }
        Assert-Condition $rejected `
            "Atomic receipt creation did not reject an existing destination."
        Assert-Condition ((Get-Content -LiteralPath $atomicPath -Raw).Trim() -ceq $atomicJson) `
            "Atomic receipt collision changed the original receipt."
    }
    finally {
        Remove-Item -LiteralPath $atomicDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Configuration authority evidence collector self-test passed." -ForegroundColor Green
}

if ($SelfTest) {
    Invoke-CollectorSelfTest
    return
}

if ($env:OS -ne "Windows_NT") {
    throw "The Configuration authority evidence collector requires Windows."
}

$resolvedPaths = @(Resolve-WorldLogPaths)
$allLines = New-Object Collections.Generic.List[string]
foreach ($path in $resolvedPaths) {
    foreach ($line in @(Get-Content -LiteralPath $path -Tail $MaxLogLines)) {
        [void]$allLines.Add([string]$line)
    }
}

$evidence = Read-ConfigurationEvidence -Lines $allLines.ToArray()
$receipt = New-EvidenceReceipt `
    -EvidenceMode $Mode `
    -Evidence $evidence `
    -RequiredRuntimes $RequiredParityRuntimes `
    -SourceCount $resolvedPaths.Count

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $timestamp = [DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss")
    $fileMode = $Mode.ToLowerInvariant()
    $OutputPath = Join-Path $root `
        "artifacts\configuration-authority-evidence\configuration-authority-$fileMode-$timestamp.json"
}
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$json = $receipt | ConvertTo-Json -Depth 6
foreach ($forbidden in @(
    "Snapshot",
    "Payload",
    "Password",
    "CertificatePath",
    "Credential",
    "Account",
    "MaxGold"
)) {
    Assert-Condition ($json.IndexOf(
        $forbidden,
        [StringComparison]::OrdinalIgnoreCase) -lt 0) `
        "The evidence receipt contains forbidden text '$forbidden'."
}
Write-EvidenceReceiptAtomically -Path $OutputPath -Json $json

$roundTrip = Get-Content -LiteralPath $OutputPath -Raw | ConvertFrom-Json
Assert-Condition ($roundTrip.schemaVersion -eq 1 -and $roundTrip.verdict -ceq "pass") `
    "The written evidence receipt failed round-trip validation."
Write-Host "Configuration authority evidence accepted: $OutputPath" -ForegroundColor Green
