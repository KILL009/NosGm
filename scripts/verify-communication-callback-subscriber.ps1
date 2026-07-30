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
        throw "$Name does not match the required mapping pattern."
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
$eventMapper = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationGlobalEventMapper.cs"
$masterProject = Read-RequiredFile `
    "Data\NosGm.Master.Library\NosGm.Master.Library.csproj"
$legacyClient = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationServiceClient.cs"
$selfTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackSubscriberSelfTest.cs"
$envelopeValidationTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackEnvelopeValidationSelfTest.cs"
$optionsSafetyTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackOptionsSafetySelfTest.cs"
$liveTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackLiveSubscriberSelfTest.cs"
$masterRoleTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\MasterCertificateRoleSelfTest.cs"
$testProgram = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\Program.cs"
$protocol = Read-RequiredFile `
    "contracts\cluster\v1\cluster_communication_callbacks.proto"
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
Require $options "return AuthenticationGrpcWireMode.Http2;" `
    "Callback subscriber defaults to native HTTP/2"
Require $options "GRPCWEB" `
    "Callback subscriber retains optional gRPC-Web compatibility"
Require $options "InitialReconnectDelayMilliseconds" `
    "Callback reconnection starts with a bounded delay"
Require $options "MaximumReconnectDelayMilliseconds" `
    "Callback reconnection has a hard delay ceiling"
Require $options "PathsEqual(certificatePath, cursorPath)" `
    "Cursor path cannot collide with the client certificate"
Require $options "PathsEqual(cursorPath, trustedRootPath)" `
    "Cursor path cannot collide with the trusted root"
Require $options "PathsEqual(certificatePath, trustedRootPath)" `
    "Client certificate cannot be reused as the trusted root"
Require $options "client certificate, trusted root, and cursor paths must be distinct" `
    "All callback security files remain isolated"

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
Require $processor "ValidateEnvelope(envelope);" `
    "New callback sequences are validated before application"
Require $processor "IsCanonicalNonEmptyGuid" `
    "Callback envelopes require canonical event IDs"
Require $processor "ValidateCallbackAndTarget" `
    "Callback payloads must match their target"
Require $processor "MaxEventTtlSeconds" `
    "Callback envelope lifetime is bounded"
Require $processor "await _handler.ApplyAsync" `
    "Callback handler completes before acknowledgement"
Require $processor "_cursorStore.Save(envelope.Sequence);" `
    "Callback sequence is committed after application"
Require $processor "envelope.ExpiresAtUnixTimeMs" `
    "Expired callbacks are skipped without application"

Require $subscriber "SubscribeCommunicationCallbacks" `
    "Client uses the generated server-streaming callback stub"
Require $subscriber "GrpcWebMode.GrpcWeb" `
    "Optional compatibility mode uses binary gRPC-Web"
Require $subscriber "WinHttpHandler" `
    "Legacy Windows 11 callers use native HTTP/2"
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
Require $dispatcher "CommunicationGlobalEventMapper.ToDomain" `
    "Typed dispatcher uses the explicit global-event mapping"
Forbid $dispatcher "(EventType)envelope.GlobalEvent.EventType" `
    "Typed dispatcher never casts offset enums directly"
Forbid $dispatcher "SCSCharacterMessage" `
    "Deferred rendered messaging is absent from typed dispatch"

$eventMappings = @(
    @("InstantBattle", "INSTANTBATTLE"),
    @("LandOfDeath", "LOD"),
    @("MinilandRefresh", "MINILANDREFRESHEVENT"),
    @("RankingRefresh", "RANKINGREFRESH"),
    @("GlacernonShip", "GLACERNONSHIP"),
    @("GlacernonRaid", "GLACERNONRAID"),
    @("MeteoriteGame", "METEORITEGAME"),
    @("TalentArena", "TALENTARENA"),
    @("Caligor", "CALIGOR"),
    @("IceBreaker", "ICEBREAKER"),
    @("AutoReboot", "AUTOREBOOT"),
    @("Act7Ship", "Act7Ship"),
    @("CelestialSpire", "CELESTIALSPIRE"),
    @("RainbowBattle", "RAINBOWBATTLE"),
    @("DropRate", "DROPRATE"),
    @("FairyRate", "FAIRYRATE"),
    @("HeroRate", "HERORATE"),
    @("XpRate", "XPRATE"),
    @("ResetRate", "RESETRATE"),
    @("DailyMissionExtensionRefresh", "DAILYMISSIONEXTENSIONREFRESH"),
    @("Asgobas", "ASGOBAS"),
    @("WorldBoss", "WORLDBOSS"),
    @("BattleRoyale", "BattleRoyal"),
    @("DuelEvent", "DUELEVENT"),
    @("PrivateDuelEvent", "DUELEVENTPRIVATE"),
    @("OpenWorldBoss", "OpenWorldBoss")
)
foreach ($mapping in $eventMappings) {
    $wireName = [regex]::Escape([string]$mapping[0])
    $domainName = [regex]::Escape([string]$mapping[1])
    Require-Match $eventMapper `
        ("case\s+WireV1\.CommunicationGlobalEventType\." +
         $wireName + ":\s*return\s+EventType\." +
         $domainName + ";") `
        ("Wire-to-domain global event pair is exact: " + $mapping[0])
    Require-Match $eventMapper `
        ("case\s+EventType\." + $domainName +
         ":\s*return\s+WireV1\.CommunicationGlobalEventType\." +
         $wireName + ";") `
        ("Domain-to-wire global event pair is exact: " + $mapping[1])
}
Require $eventMapper "public static EventType ToDomain" `
    "Global-event mapper supports callback consumption"
Require $eventMapper "public static WireV1.CommunicationGlobalEventType ToWire" `
    "Global-event mapper supports future Master publication"
Require $masterProject `
    '<Compile Include="Client\CommunicationCallbackEnvelopeDispatcher.cs" />' `
    "Legacy Master library compiles the typed dispatcher"
Require $masterProject `
    '<Compile Include="Client\CommunicationGlobalEventMapper.cs" />' `
    "Legacy Master library compiles the explicit event mapper"

Require $selfTest "The callback cursor advances after the handler returns" `
    "Self-test proves post-application cursor commit"
Require $selfTest "A failed callback never advances the durable cursor" `
    "Self-test protects callback replay after handler failure"
Require $selfTest "A corrupt callback cursor fails closed" `
    "Self-test protects cursor corruption"
Require $selfTest "defaults to native HTTP/2" `
    "Self-test protects the Windows 11 transport default"
Require $selfTest "CommunicationCallbackTargetKind.AllNodes" `
    "Processor self-test constructs an authoritative penalty target"
Require $envelopeValidationTest "Malformed callback event IDs fail before application" `
    "Malformed event IDs are covered by runtime tests"
Require $envelopeValidationTest "without advancing the cursor" `
    "Malformed envelopes cannot be acknowledged"
Require $envelopeValidationTest "MaxEventTtlSeconds" `
    "Envelope lifetime bounds are covered by runtime tests"
Require $optionsSafetyTest "Callback cursor cannot overwrite the trusted root" `
    "Trusted root collision has a regression test"
Require $optionsSafetyTest "client certificate and trusted root must be distinct" `
    "Certificate role separation has a regression test"

Require $liveTest "public static async Task RunLiveAsync()" `
    "Live callback acceptance exposes an explicit async entry point"
Forbid $liveTest "[ModuleInitializer]" `
    "Live callback networking never runs under the CLR module initializer lock"
Require $liveTest "subscriberInstanceId" `
    "Live callback acceptance owns an explicit process identity"
Require $liveTest 'Guid.NewGuid().ToString("N")' `
    "Each live wire-mode acceptance uses an isolated process identity"
Forbid $liveTest "acceptance-login-callback-subscriber-1" `
    "Live acceptance never reuses the stale fixed subscriber identity"
Require $liveTest "Live Login stream applies the typed penalty callback" `
    "Live acceptance applies a typed callback"
Require $liveTest "Live callback cursor commits after handler completion" `
    "Live acceptance observes the post-handler cursor"
Require $liveTest "NOSGM_AUTH_GRPC_LIVE_LOGIN_CERT_PATH" `
    "Live subscriber presents the Login certificate"
Require $liveTest "NOSGM_AUTH_GRPC_LIVE_MASTER_CERT_PATH" `
    "Live publisher presents the separate Master certificate"
Require $liveTest "CommunicationCallbackTargetKind.AllNodes" `
    "Live penalty acceptance targets Login and World subscribers"
Forbid $liveTest "CommunicationCallbackTargetKind.AllLoginNodes" `
    "Live penalty acceptance never narrows the all-node contract"
Require $masterRoleTest "public static async Task RunLiveAsync()" `
    "Live Master certificate probe exposes an explicit async entry point"
Forbid $masterRoleTest 'Contains("--live"' `
    "Master network probing never runs from its static module initializer"
Forbid $masterRoleTest ".GetAwaiter().GetResult()" `
    "Master network probing never blocks the module initializer"
Require $testProgram "await MasterCertificateRoleSelfTest.RunLiveAsync();" `
    "Main self-test flow runs the live Master role probe"
Require $testProgram "await CommunicationCallbackLiveSubscriberSelfTest.RunLiveAsync();" `
    "Main self-test flow runs the live callback acceptance"
Require-Match $testProgram `
    'if\s*\(args\.Contains\("--live".*?MasterCertificateRoleSelfTest\.RunLiveAsync\(\).*?CommunicationCallbackLiveSubscriberSelfTest\.RunLiveAsync\(\).*?RunLiveGrpcAcceptanceAsync\(\)' `
    "All live network tests run after module initialization completes"

Require $protocol "HTTP/2 is the primary Windows 11 transport" `
    "Protocol comments record the current primary transport"
Require $protocol "Subscriber replay acknowledgement is sequence-based" `
    "Protocol distinguishes publication idempotency from replay acknowledgement"
Require $legacyClient "Communication gRPC cutover is blocked" `
    "Production communication cutover remains guarded"
Forbid $legacyClient "new GrpcCommunicationCallbackSubscriber" `
    "Production does not start the callback subscriber yet"
Require $documentation "Production remains on the SCS callback path." `
    "Documentation preserves the current production boundary"
Require $documentation "Native HTTP/2 is the primary Windows 11 path." `
    "Documentation records the Windows 11 transport decision"
Require $documentation "A malformed envelope fails closed." `
    "Documentation records the envelope validation boundary"
Require $documentation "successful typed dispatch" `
    "Documentation distinguishes dispatch from downstream completion"
Require $documentation "runtime-generation scoped" `
    "Documentation records the durable-cursor generation boundary"

Write-Host `
    "NosGM dual-target callback subscriber, validation, durable cursor and typed dispatcher contracts passed." `
    -ForegroundColor Green
