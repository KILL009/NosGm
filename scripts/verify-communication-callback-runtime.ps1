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
        throw "Expected callback runtime file was not found: $RelativePath"
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

$program = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Program.cs"
$options = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\CommunicationRuntimeOptions.cs"
$hub = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\State\CommunicationCallbackHub.cs"
$service = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Services\ClusterCommunicationCallbackService.cs"
$stateService = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Services\ClusterCommunicationService.cs"
$contract = Read-RequiredFile `
    "contracts\cluster\v1\cluster_communication_callbacks.proto"
$selfTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackHubSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-runtime.md"

Require $program "AddSingleton<CommunicationCallbackHub>" `
    "The callback hub has one process-wide state instance"
Require $program "MapGrpcService<ClusterCommunicationCallbackService>" `
    "The typed callback service is mapped"
Require $program ".EnableGrpcWeb()" `
    "Callback streaming remains available to Windows 10 callers"

Require $options "NOSGM_COMMUNICATION_MAX_CALLBACK_SUBSCRIBERS" `
    "Subscriber-state capacity is configurable"
Require $options "DefaultMaximumCallbackSubscribers = 2048" `
    "Subscriber-state capacity has a bounded default"
Require $options "MaximumCallbackSubscribers = 8192" `
    "Subscriber-state capacity has an absolute ceiling"

Require $hub "MaxPendingEventsPerSubscriber" `
    "Each active callback stream uses the 1,024-event pending bound"
Require $hub "MaxRetainedEventsPerSubscriber" `
    "Each process identity uses the 4,096-event replay bound"
Require $hub "Channel.CreateBounded" `
    "Pending callback delivery uses a bounded channel"
Require $hub "QueueOverflow" `
    "Falling behind terminates the stream"
Require $hub "HighestCapacityEvictedSequence" `
    "Replay gaps caused by capacity eviction are detected"
Require $hub "ResumeAfterSequence" `
    "Subscriber reconnects use the durable cursor"
Require $hub "Interlocked.Increment(ref _sequence)" `
    "Accepted callbacks receive one monotonic sequence"
Require $hub "CreatePublishFingerprint" `
    "Event IDs are bound to their semantic payload"
Require $hub "SHA256.HashData" `
    "Idempotency fingerprints use a stable cryptographic digest"
Require $hub "existing.Fingerprint" `
    "Duplicate event IDs distinguish idempotency from conflict"
Require $hub "ExpiresAtUnixTimeMs" `
    "Callback TTL is enforced during delivery and replay"
Require $hub "CharacterId" `
    "Character-targeted routing uses the derived exact route"
Require $hub "MakeSubscriberCapacity" `
    "Subscriber-state growth cannot become unbounded"
Forbid $hub "object[]" `
    "Callback routing contains no dynamic CLR invocation payload"
Forbid $hub "SCSCharacterMessage" `
    "Rendered legacy messaging is not tunneled through callbacks"
Forbid $hub "Task.WhenAll" `
    "Callback publication is not mirrored through hidden paths"

Require $service "ClusterNodeRole.Master" `
    "Only the Master role may publish callbacks"
Require $service "ClusterNodeRole.Login" `
    "Login may open its restricted callback stream"
Require $service "ClusterNodeRole.World" `
    "World may open its registered callback stream"
Require $service "AllowedFingerprints.TryGetValue" `
    "Callback publication fails closed without a Master allow-list"
Require $service "StatusCode.PermissionDenied" `
    "Unauthorized certificates fail at the RPC boundary"
Require $service "StatusCode.ResourceExhausted" `
    "Queue overflow is explicit to the subscriber"
Require $service "StatusCode.OutOfRange" `
    "Unavailable replay cursors are explicit"
Require $service "ReadAllAsync" `
    "The server stream drains one subscriber queue"
Require $service "subscription.ReplayEvents" `
    "Retained callbacks are sent before live delivery"
Forbid $service "GetClientProxy" `
    "The .NET 10 callback service does not call back through SCS"
Forbid $service "DynamicInvoke" `
    "The callback service contains no dynamic invocation"

Require $stateService "_callbackHub.RegisterWorld" `
    "World routing is derived only after authoritative registration"
Require $stateService "_state.UnregisterWorldServer(worldId);" `
    "A routing-index registration failure rolls authoritative state back"
Require $stateService "_callbackHub.BindCharacter" `
    "Successful character attachment feeds the derived route"
Require $stateService "_callbackHub.UnbindCharacter" `
    "Successful character teardown removes the derived route"
Require $stateService "_callbackHub.DisconnectAccount" `
    "Account teardown removes stale character routes"
Require $stateService "_callbackHub.PulseAccount" `
    "Session pulses keep the derived character route bounded"
Require $stateService "_callbackHub.UnregisterWorld" `
    "World teardown closes and removes its callback routing"

Require $contract "returns (stream CommunicationCallbackEnvelope)" `
    "Callback delivery remains server streaming"
Require $contract "oneof callback" `
    "Callback payloads remain typed Protobuf variants"
Require $contract "resume_after_sequence" `
    "The wire contract carries the replay cursor"

Require $selfTest "An identical canonical event is idempotent" `
    "Runtime self-test covers event idempotency"
Require $selfTest "Reconnect replays only newer retained events" `
    "Runtime self-test covers cursor replay"
Require $selfTest "Expired callbacks are never replayed" `
    "Runtime self-test covers TTL"
Require $selfTest "QueueOverflow" `
    "Runtime self-test covers bounded backpressure"
Require $documentation "Production `CommunicationServiceClient` still defaults to SCS" `
    "Documentation preserves the guarded production boundary"

Write-Host `
    "NosGM bounded communication callback runtime contracts passed." `
    -ForegroundColor Green
