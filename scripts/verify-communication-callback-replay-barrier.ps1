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
        throw "Expected callback replay-barrier file was not found: $RelativePath"
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
        throw "$Name does not match the required replay-barrier ordering."
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

$protocol = Read-RequiredFile `
    "contracts\cluster\v1\cluster_communication_callbacks.proto"
$service = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Services\ClusterCommunicationCallbackService.cs"
$subscriber = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\GrpcCommunicationCallbackSubscriber.cs"
$tracker = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackReplayTracker.cs"
$processor = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackProcessor.cs"
$lifecycle = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackSubscriberLifecycle.cs"
$trackerTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackReplayTrackerSelfTest.cs"
$liveTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackLiveSubscriberSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-replay-barrier.md"

Require $protocol "supports_replay_complete_barrier = 8" `
    "Replay barrier is explicitly negotiated"
Require $protocol "CommunicationCallbackReplayComplete replay_complete = 19" `
    "Replay barrier is a typed server-stream control variant"
Require $protocol "string runtime_generation_id = 1" `
    "Replay barrier is bound to one runtime generation"
Require $protocol "uint64 replay_through_sequence = 2" `
    "Replay barrier declares its global sequence boundary"
Require $protocol "uint64 resume_after_sequence = 3" `
    "Replay barrier echoes the durable resume cursor"
Require $protocol "uint32 replayed_events = 4" `
    "Replay barrier reports actual replay delivery count"
Forbid $protocol "CommunicationCallbackReplayComplete replay_complete = 19;`n  }`n}`n`nmessage Publish" `
    "Master publication union cannot contain the server-only barrier"

Require $service "private static readonly object StreamBoundarySync" `
    "Subscription opening and publication share one process-wide gate"
Require-Match $service `
    'lock\s*\(StreamBoundarySync\).*?_hub\.TryOpenSubscription\s*\(.*?replayThroughSequence\s*=\s*_hub\.CurrentSequence' `
    "Subscription opens and snapshots sequence atomically"
Require-Match $service `
    'lock\s*\(StreamBoundarySync\).*?result\s*=\s*_hub\.Publish\s*\(' `
    "Callback publication participates in the same boundary gate"
Require-Match $service `
    'subscription\.ReplayEvents.*?request\.SupportsReplayCompleteBarrier.*?ReplayComplete\s*=.*?subscription\.PendingEvents' `
    "Server writes replay, then the negotiated barrier, then pending callbacks"
Require $service "ReplayedEvents = replayedEvents" `
    "Server reports only replay envelopes actually written"
Require $service '"CommunicationCallbackReplayComplete"' `
    "Replay boundary is independently auditable"
Forbid $service "await responseStream.WriteAsync(envelope);`n                    lock (StreamBoundarySync)" `
    "Network writes never hold the publication boundary gate"

Require $subscriber "SupportsReplayCompleteBarrier = true" `
    "New subscribers explicitly request the replay barrier"
Require $subscriber "CommunicationCallbackReplayTracker" `
    "Subscriber owns a dedicated control-plane tracker"
Require-Match $subscriber `
    'CallbackOneofCase\s*\.ReplayComplete' `
    "Subscriber intercepts the control variant"
Require-Match $subscriber `
    'CallbackOneofCase\s*\.ReplayComplete.*?_replayTracker\.Complete\s*\(.*?return;.*?_processor\.ProcessAsync' `
    "Replay barrier bypasses the callback processor"
Require $subscriber "ValidateLiveSequence" `
    "Callbacks after readiness must follow the declared boundary"
Require-Match $subscriber `
    'RunSingleStreamAsync.*?_replayTracker\.Reset\(\).*?BeginStream.*?finally.*?_replayTracker\.Reset\(\)' `
    "Every stream attempt clears stale readiness"
Forbid $processor "ReplayComplete" `
    "Replay barrier can never advance the callback cursor"

Require $tracker "CommunicationCallbackReplayEvidence" `
    "Replay readiness uses immutable evidence"
Require $tracker "envelope.Target != null" `
    "Control envelopes reject callback target metadata"
Require $tracker "barrier.ReplayedEvents != _observedReplayEvents" `
    "Barrier count must match observed replay callbacks"
Require $tracker "The callback stream returned more than one replay barrier" `
    "Duplicate barriers fail closed"
Require $tracker "A live callback did not follow the replay boundary" `
    "Live sequence regression fails closed"

Require $lifecycle "public bool IsReplayComplete" `
    "Production lifecycle exposes active readiness"
Require $lifecycle "public CommunicationCallbackReplayEvidence ReplayEvidence" `
    "Production lifecycle exposes immutable replay evidence"
Require $lifecycle "public ulong AppliedSequence" `
    "Production lifecycle exposes the durable callback position"
Require $lifecycle "public string RuntimeGenerationId" `
    "Production lifecycle exposes the active generation"

Require $trackerTest "An empty replay reaches readiness" `
    "Tracker test covers an empty runtime"
Require $trackerTest "Duplicate replay barriers fail closed" `
    "Tracker test covers duplicate control elements"
Require $trackerTest "Barrier replay counts must match" `
    "Tracker test covers replay-count mismatch"
Require $trackerTest "Replay barriers cannot contain event metadata" `
    "Tracker test covers control/data separation"
Require $trackerTest "A live callback cannot cross backwards" `
    "Tracker test covers post-barrier ordering"

Require $liveTest "Live Login stream receives the replay-complete barrier" `
    "Live mTLS acceptance waits for readiness"
Require $liveTest "Replay barrier never invokes the callback handler" `
    "Live acceptance proves the control bypasses handlers"
Require $liveTest "Replay barrier never advances the callback cursor" `
    "Live acceptance proves the control bypasses durable acknowledgement"
Require $liveTest "Live callback sequence follows the replay boundary" `
    "Live acceptance proves pending delivery follows the boundary"

Require $documentation "SCS remains the only callback transport allowed to apply effects" `
    "Documentation preserves SCS authority"
Require $documentation 'a client that sends `false` receives the legacy stream' `
    "Documentation records old-client compatibility"
Require $documentation "never reaches the processor" `
    "Documentation records control/data separation"
Require $documentation "does not itself prove payload parity" `
    "Documentation does not overstate replay readiness"

Write-Host `
    "NosGM negotiated callback replay-complete barrier contracts passed." `
    -ForegroundColor Green
