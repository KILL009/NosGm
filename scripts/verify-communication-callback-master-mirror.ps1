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
        throw "Expected Master callback mirror file was not found: $RelativePath"
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
        throw "$Name does not match the required callback mirror ordering."
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

$options = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackMirrorOptions.cs"
$publisher = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\GrpcCommunicationCallbackPublisher.cs"
$planner = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CharacterPresenceMirrorRoutePlanner.cs"
$mirror = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Master.Server\MasterCommunicationCallbackMirror.cs"
$service = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Master.Server\MirroredCommunicationService.cs"
$program = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Master.Server\Program.cs"
$project = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Master.Server\NosGm.Master.Server.csproj"
$optionsTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackMirrorOptionsSelfTest.cs"
$plannerTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CharacterPresenceMirrorRoutePlannerSelfTest.cs"
$liveTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\MasterCommunicationCallbackPublisherLiveSelfTest.cs"
$interfaceMapTest = Read-RequiredFile `
    "scripts\verify-master-callback-mirror-interface-map.ps1"
$windowsWorkflow = Read-RequiredFile `
    ".github\workflows\build-windows.yml"
$documentation = Read-RequiredFile `
    "docs\master-callback-publication-mirror.md"
$migrationMap = Read-RequiredFile `
    "contracts\cluster\v1\communication-callback-migration-map.json"

Require $options "NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_ENABLED" `
    "Master callback mirror has an explicit activation flag"
Require $options "DefaultQueueCapacity = 4096" `
    "Master callback mirror queue has a bounded default"
Require $options "MinimumQueueCapacity = 64" `
    "Master callback mirror queue has a safe floor"
Require $options "MaximumQueueCapacity = 16384" `
    "Master callback mirror queue has an absolute ceiling"
Require $options "DefaultStopTimeoutMilliseconds = 5000" `
    "Master callback mirror shutdown wait has a bounded default"
Require $options "must be true or false without surrounding whitespace" `
    "Master callback mirror booleans fail closed"

Require $publisher "options.CallerRole != ClusterNodeRole.Master" `
    "Callback publisher accepts only the Master role"
Require $publisher "publicationTemplate.Clone()" `
    "Every callback retry clones one immutable semantic template"
Require $publisher "request.Context = CreateRequestContext" `
    "Every callback attempt receives a fresh request context"
Require $publisher 'RequestId = Guid.NewGuid().ToString("D")' `
    "Every callback attempt receives a fresh request ID"
Forbid $publisher "NOSGM_AUTH_GRPC_CLIENT_CERT_PATH" `
    "Callback publisher never reads the AuthBridge certificate namespace"

Require $planner "ResolvePeerWorldIds" `
    "Presence routing has one pure route planner"
Require $planner "route.WorldId != sourceWorldId" `
    "Presence route planning excludes the source World"
Require $planner "StringComparison.Ordinal" `
    "Presence route planning compares World groups exactly"
Require $planner ".Distinct()" `
    "Presence route planning collapses duplicate World registrations"
Require $planner ".OrderBy(worldId => worldId)" `
    "Presence route planning is deterministic"

Require $mirror "MasterCommunicationGrpcIdentityOptions.Load()" `
    "Mirror lifecycle loads the communication-specific Master identity"
Require $mirror "new BlockingCollection<MirrorItem>" `
    "Master callback mirror owns one bounded FIFO queue"
Require $mirror "new ConcurrentQueue<MirrorItem>()" `
    "Master callback mirror preserves FIFO enqueue order"
Require $mirror "queue.TryAdd" `
    "SCS callback threads never block on mirror enqueue"
Forbid $mirror "queue.Add(" `
    "Blocking queue insertion cannot return"
Require $mirror "item.Template" `
    "Transient retries preserve the original EventId and payload"
Require $mirror "item.EnqueuedAt.AddSeconds" `
    "Queued callback copies expire locally"
Require $mirror "CALLBACK_MIRROR_FAULTED" `
    "Terminal mirror failure remains observable"
Require $mirror "SCS remains authoritative" `
    "Terminal mirror failure cannot claim transport authority"
Require $mirror "TryCharacterPresence" `
    "Character presence is published through the shared mirror queue"
Require $mirror "WorldIdTarget(targetWorldId)" `
    "Each presence copy targets one exact peer World"
Require $mirror "DropIfTargetNotFound" `
    "Presence copies may tolerate a missing shadow target"
Require $mirror "TARGET_NOT_REGISTERED" `
    "Missing presence shadow targets remain observable"
Require $mirror "CommunicationResultCode.NotFound" `
    "Missing presence shadow targets do not fault the worker"
Require $mirror "TryStaticBonusRefresh" `
    "Future static-bonus publication already has an exact typed builder"
Require $mirror "CommunicationCallbackTargetKind.CharacterId" `
    "Static-bonus publication uses the CharacterId route"
Require $mirror "CommunicationCallbackTargetKind.AllNodes" `
    "Penalty refresh targets Login and World subscribers"
Require $mirror "CommunicationCallbackTargetKind.AllWorlds" `
    "World-wide callbacks preserve their legacy scope"
Require $mirror "CommunicationCallbackTargetKind.WorldGroup" `
    "Group-scoped callbacks preserve their legacy scope"
Require $mirror "CommunicationCallbackTargetKind.WorldId" `
    "Character presence uses exact World routing"

Require $service ": CommunicationService," `
    "Mirrored service inherits the complete legacy implementation"
Require $service "ICommunicationService" `
    "Mirrored service reimplements the SCS interface"
Require $service "CALLBACK_MIRROR_ISOLATED_FAILURE" `
    "Mirror exceptions are isolated from SCS results"
Require $service "ResolvePeerWorldIds" `
    "Presence callbacks snapshot exact peer Worlds"
Require $service "MirrorPresence" `
    "Presence callbacks fan out through one isolated helper"
Forbid $service "SendMessageToCharacter" `
    "Rendered legacy character messaging is not mirrored"

$orderedMethods = @(
    @{ Name = "ConnectCharacter"; Mirror = "MirrorPresence" },
    @{ Name = "DisconnectCharacter"; Mirror = "MirrorPresence" },
    @{ Name = "KickSession"; Mirror = "TryKickSession" },
    @{ Name = "RefreshPenalty"; Mirror = "TryPenaltyRefresh" },
    @{ Name = "Restart"; Mirror = "TryRestart" },
    @{ Name = "RunGlobalEvent"; Mirror = "TryGlobalEvent" },
    @{ Name = "Shutdown"; Mirror = "TryShutdown" },
    @{ Name = "UpdateBazaar"; Mirror = "TryBazaarRefresh" },
    @{ Name = "UpdateFamily"; Mirror = "TryFamilyRefresh" },
    @{ Name = "UpdateRelation"; Mirror = "TryRelationRefresh" }
)
foreach ($entry in $orderedMethods) {
    $methodName = [regex]::Escape($entry.Name)
    $mirrorName = [regex]::Escape($entry.Mirror)
    Require-Match $service `
        ("public\s+new\s+[^\{]+\b" + $methodName +
         "\s*\(.*?base\." + $methodName +
         "\s*\(.*?" + $mirrorName + "\s*\(") `
        ("SCS executes before the typed mirror for " + $entry.Name)
}

Require-Match $service `
    'DisconnectCharacter\(.*?wasConnected.*?base\.DisconnectCharacter\(.*?if\s*\(wasConnected\).*?MirrorPresence' `
    "Redundant character disconnects do not create mirror events"
Require-Match $service `
    'MirrorPresence\(.*?foreach\s*\(Guid targetWorldId in peerWorldIds\).*?TryCharacterPresence\(\s*targetWorldId' `
    "Presence fan-out publishes one copy per exact peer World"

Require-Match $program `
    'StartCommunicationCallbackMirror\(\).*?AddService<ICommunicationService, MirroredCommunicationService>.*?server\.Start\(\)' `
    "Master validates mirror configuration before exposing SCS"
Require $program "StopInfrastructure" `
    "Master owns callback mirror shutdown"
Require-Match $program `
    'private static void StopInfrastructure\(\).*?MasterCommunicationCallbackMirror\.Instance\.Stop\(\).*?StopLauncherAuthBridge\(\)' `
    "Master stops the callback mirror before its auxiliary bridge"

Require $project '<Compile Include="MasterCommunicationCallbackMirror.cs" />' `
    "Classic Master project compiles the mirror lifecycle"
Require $project '<Compile Include="MirroredCommunicationService.cs" />' `
    "Classic Master project compiles the SCS-first wrapper"

Require $interfaceMapTest "GetInterfaceMap" `
    "Compiled mirror verification inspects the CLR interface map"
Require $interfaceMapTest '"ConnectCharacter"' `
    "Compiled mirror verification includes character connection"
Require $interfaceMapTest '"DisconnectCharacter"' `
    "Compiled mirror verification includes character disconnection"
Require $interfaceMapTest "NosGm.Master.Server.MirroredCommunicationService" `
    "Compiled mirror verification expects all ten derived targets"
Require $interfaceMapTest '"SendMessageToCharacter"' `
    "Compiled mirror verification preserves raw messaging on the legacy service"
Require $windowsWorkflow "Verify Master callback mirror interface dispatch" `
    "Windows CI names the compiled interface-dispatch check"
Require $windowsWorkflow "./scripts/verify-master-callback-mirror-interface-map.ps1" `
    "Windows CI executes the compiled interface-dispatch check"

Require $optionsTest "Master callback mirror is disabled by default" `
    "Mirror activation default has a regression test"
Require $optionsTest "rejects surrounding whitespace" `
    "Mirror boolean strictness has a regression test"
Require $plannerTest "unique deterministic peers" `
    "Presence peer selection has a deterministic regression test"
Require $plannerTest "excludes the source World" `
    "Presence source exclusion has a regression test"
Require $plannerTest "excludes another World group" `
    "Presence group isolation has a regression test"
Require $plannerTest "unknown source World" `
    "Unknown presence sources have a regression test"

Require $liveTest "new GrpcCommunicationCallbackPublisher" `
    "Live acceptance exercises the reusable publisher"
Require-Match $liveTest `
    'publisher\.PublishAsync\(template.*?publisher\.PublishAsync\(template' `
    "Live acceptance retries the same semantic publication"
Require $liveTest "CommunicationGlobalEventType.InstantBattle" `
    "Live publisher probe uses a valid World-only callback"
Require $liveTest "retry.AcceptedSequence" `
    "Live acceptance proves EventId idempotency"
Require $liveTest "IsBackground = false" `
    "Live publisher acceptance cannot be skipped by process exit"

Require $migrationMap '"legacyMethod": "CharacterConnected"' `
    "Migration inventory contains character connection"
Require $migrationMap '"legacyMethod": "CharacterDisconnected"' `
    "Migration inventory contains character disconnection"
Require $migrationMap '"legacyMethod": "SendMessageToCharacter"' `
    "Migration inventory retains the deferred raw-message boundary"
Require $migrationMap '"targetKind": "ALL_NODES"' `
    "Migration inventory preserves penalty all-node routing"
Require $documentation "SCS remains the only transport allowed to apply" `
    "Documentation preserves one callback effect authority"
Require $documentation 'non-blocking `TryAdd`' `
    "Documentation records the non-blocking queue boundary"
Require $documentation "one `WORLD_ID` publication per peer World" `
    "Documentation records exact presence fan-out"
Require $documentation "source World never receives" `
    "Documentation records source-World exclusion"
Require $documentation 'current `ICommunicationService` exposes no SCS emitter' `
    "Documentation does not invent a static-bonus source"
Require $documentation "server-issued replay-complete barrier" `
    "Documentation names the next atomic cutover boundary"

Write-Host `
    "NosGM bounded SCS-first Master callback publication mirror contracts passed." `
    -ForegroundColor Green
