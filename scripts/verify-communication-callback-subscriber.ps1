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
        throw "Expected callback subscriber file was not found: $RelativePath"
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

$options = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackSubscriberOptions.cs"
$cursor = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackCursorStore.cs"
$processor = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackProcessor.cs"
$subscriber = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\GrpcCommunicationCallbackSubscriber.cs"
$dispatcher = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackEnvelopeDispatcher.cs"
$masterProject = Read-RequiredFile `
    "Data\NosGm.Master.Library\NosGm.Master.Library.csproj"
$legacyClient = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationServiceClient.cs"
$selfTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackSubscriberSelfTest.cs"
$liveTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackLiveSubscriberSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-subscriber.md"

Require $options "NOSGM_COMMUNICATION_GRPC_CLIENT_CERT_PATH" `
    "Callback subscriber has its own certificate namespace"
Require $options "NOSGM_COMMUNICATION_GRPC_CALLBACK_CURSOR_PATH" `
    "Callback subscriber requires an explicit cursor path"
Require $options "The callback subscriber role must be Login or World." `
    "Only Login and World may subscribe"
Require $options "Login callback identity cannot contain World fields." `
    "Login cannot borrow a World identity"
Require $options "World callback identity is incomplete." `
    "World subscriptions require exact registered identity"
Require $options "address.IsLoopback" `
    "Callback endpoint remains loopback-only"
Require $options "Uri.UriSchemeHttps" `
    "Callback endpoint requires HTTPS"
Require $options "GRPCWEB" `
    "Callback subscriber exposes the Windows 10 wire mode"
Require $options "InitialReconnectDelayMilliseconds" `
    "Callback reconnection starts with a bounded delay"
Require $options "MaximumReconnectDelayMilliseconds" `
    "Callback reconnection has a hard delay ceiling"

Require $cursor "FileOptions.WriteThrough" `
    "Cursor writes request durable storage"
Require $cursor "stream.Flush(true)" `
    "Cursor payload is flushed before replacement"
Require $cursor "File.Replace" `
    "Existing callback cursor is atomically replaced"
Require $cursor "ulong.TryParse" `
    "Callback cursor accepts only an unsigned sequence"
Require $cursor "cursor file is corrupt" `
    "A corrupt cursor fails closed"
Forbid $cursor "BinaryFormatter" `
    "Callback cursor uses no unsafe object serialization"

Require $processor "envelope.Sequence <= _appliedSequence" `
    "Already applied callback sequences are ignored"
Require $processor "await _handler.ApplyAsync" `
    "Callback handler completes before acknowledgement"
Require $processor "_cursorStore.Save(envelope.Sequence);" `
    "Callback sequence is committed after application"
Require $processor "envelope.ExpiresAtUnixTimeMs" `
    "Expired callbacks are skipped without application"

Require $subscriber "SubscribeCommunicationCallbacks" `
    "Client uses the generated server-streaming callback stub"
Require $subscriber "GrpcWebMode.GrpcWeb" `
    "Windows 10 callback streaming uses binary gRPC-Web"
Require $subscriber "ClientCertificates.Add" `
    "Callback subscriber presents its mTLS identity"
Require $subscriber "RequestedService = WireV1.ClusterService.Communication" `
    "Callback setup context targets Communication"
Require $subscriber "ResumeAfterSequence = resumeAfterSequence" `
    "Callback stream resumes from the durable cursor"
Require $subscriber "Interlocked.CompareExchange(ref _running" `
    "One subscriber object owns at most one active stream"
Require $subscriber "ShouldReconnect" `
    "Transient callback failures use an explicit retry policy"
Require $subscriber "MaximumReconnectDelayMilliseconds" `
    "Callback reconnect backoff is capped"
Require $subscriber "default:" `
    "Unlisted fatal gRPC statuses are not retried"
Forbid $subscriber "Task.WhenAll" `
    "Callback delivery is not mirrored"
Forbid $subscriber "Scs" `
    "The gRPC subscriber contains no hidden SCS fallback"
Forbid $subscriber "DangerousAcceptAnyServerCertificateValidator" `
    "Callback subscriber never disables certificate validation"

Require $dispatcher "public sealed class CommunicationCallbackEnvelopeDispatcher" `
    "Login and World can construct the typed dispatcher"
foreach ($callback in @(
    "CharacterPresence",
    "KickSession",
    "Lifecycle",
    "GlobalEvent",
    "BazaarRefresh",
    "FamilyRefresh",
    "PenaltyRefresh",
    "RelationRefresh",
    "StaticBonusRefresh"
)) {
    Require $dispatcher $callback `
        "Typed dispatcher maps $callback"
}
Require $dispatcher "OnCharacterConnected" `
    "Typed dispatcher reuses the existing presence handler"
Require $dispatcher "OnUpdatePenaltyLog" `
    "Typed dispatcher reuses the existing Login penalty handler"
Forbid $dispatcher "SCSCharacterMessage" `
    "Deferred rendered messaging is absent from typed dispatch"
Require $masterProject `
    '<Compile Include="Client\CommunicationCallbackEnvelopeDispatcher.cs" />' `
    "Legacy Master library compiles the typed dispatcher"

Require $selfTest "The callback cursor advances after the handler returns" `
    "Self-test proves post-application cursor commit"
Require $selfTest "A failed callback never advances the durable cursor" `
    "Self-test protects callback replay after handler failure"
Require $selfTest "A corrupt callback cursor fails closed" `
    "Self-test protects cursor corruption"
Require $liveTest "Live Login stream applies the typed penalty callback" `
    "Live acceptance applies a typed callback"
Require $liveTest "Live callback cursor commits after handler completion" `
    "Live acceptance observes the post-handler cursor"
Require $liveTest "NOSGM_AUTH_GRPC_LIVE_LOGIN_CERT_PATH" `
    "Live subscriber presents the Login certificate"
Require $liveTest "NOSGM_AUTH_GRPC_LIVE_MASTER_CERT_PATH" `
    "Live publisher presents the separate Master certificate"
Require $liveTest "AllLoginNodes" `
    "Live acceptance exercises role-targeted routing"

Require $legacyClient "Communication gRPC cutover is blocked" `
    "Production communication cutover remains guarded"
Forbid $legacyClient "new GrpcCommunicationCallbackSubscriber" `
    "Production does not start the callback subscriber yet"
Require $documentation "Production remains on the SCS callback path." `
    "Documentation preserves the current production boundary"

Write-Host `
    "NosGM dual-target callback subscriber, durable cursor and typed dispatcher contracts passed." `
    -ForegroundColor Green
