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
        throw "Expected callback lifecycle file was not found: $RelativePath"
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
        throw "$Name does not match the required lifecycle ordering."
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

$activation = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackActivationOptions.cs"
$shadowHandler = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackShadowEnvelopeHandler.cs"
$subscriber = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\GrpcCommunicationCallbackSubscriber.cs"
$lifecycle = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\CommunicationCallbackSubscriberLifecycle.cs"
$masterProject = Read-RequiredFile `
    "Data\NosGm.Master.Library\NosGm.Master.Library.csproj"
$loginProgram = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Login\Program.cs"
$scsTransport = Read-RequiredFile `
    "Data\NosGm.Master.Library\Client\ScsClusterCommunicationTransport.cs"
$protocol = Read-RequiredFile `
    "contracts\cluster\v1\cluster_communication_callbacks.proto"
$shadowWorldValidator = Read-RequiredFile `
    "Data\NosGm.Cluster.Contracts\Communication\V1\CommunicationCallbackShadowWorldContractValidator.cs"
$callbackService = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Services\ClusterCommunicationCallbackService.cs"
$selfTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackActivationSelfTest.cs"
$shadowWorldTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackShadowWorldSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-production-lifecycle.md"

Require $activation "NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED" `
    "Production callback lifecycle has an explicit activation flag"
Require $activation "NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED" `
    "Callback effect application has a separate explicit flag"
Require $activation "CommunicationCallbackActivationMode.Disabled" `
    "Production callback lifecycle defaults to disabled"
Require $activation "CommunicationCallbackActivationMode.Shadow" `
    "Enabled callback lifecycle enters shadow mode only"
Require $activation "Production gRPC callback application remains blocked" `
    "Real callback effects fail closed before atomic cutover"
Require $activation "must be true or false without surrounding whitespace" `
    "Activation booleans reject ambiguous whitespace"
Require $activation "DefaultStopTimeoutMilliseconds = 5000" `
    "Lifecycle shutdown uses a bounded default"

Require $shadowHandler "CommunicationCallbackShadowEnvelopeHandler" `
    "Shadow mode has a dedicated no-effect handler"
Require $shadowHandler "ObservedCallbacks" `
    "Shadow mode exposes observed callback count"
Require $shadowHandler "LastObservedSequence" `
    "Shadow mode exposes the last observed sequence"
Forbid $shadowHandler "CommunicationServiceClient" `
    "Shadow handler cannot invoke legacy callback effects"
Forbid $shadowHandler "CommunicationCallbackEnvelopeDispatcher" `
    "Shadow handler cannot dispatch gameplay callbacks"

Require $lifecycle "CommunicationCallbackActivationOptions.Load()" `
    "Production lifecycle validates activation before subscriber options"
Require $lifecycle "CommunicationCallbackSubscriberOptions.Load" `
    "Production lifecycle loads the authenticated process identity"
Require $lifecycle "FileCommunicationCallbackCursorStore" `
    "Production lifecycle owns a generation-scoped cursor"
Require $lifecycle "CommunicationCallbackShadowEnvelopeHandler" `
    "Production lifecycle remains observation-only"
Require $lifecycle "GrpcCommunicationCallbackSubscriber" `
    "Production lifecycle creates the real gRPC stream client"
Require $lifecycle "CommunicationCallbackSubscriberHost" `
    "Production lifecycle uses bounded ownership"
Require $lifecycle "AppDomain.CurrentDomain.ProcessExit" `
    "Process exit performs final callback cleanup"
Require $lifecycle "CALLBACK_SHADOW_FAULTED" `
    "Terminal subscriber faults are observable"
Forbid $lifecycle "CommunicationCallbackEnvelopeDispatcher" `
    "Production shadow lifecycle cannot apply typed callbacks"

Require $masterProject `
    '<Compile Include="Client\CommunicationCallbackSubscriberLifecycle.cs" />' `
    "Legacy Master library compiles the lifecycle owner"

Require-Match $loginProgram `
    'Authenticate\(ServerConfiguration\.MasterAuthKey\).*?DataAccessHelper\.Initialize\(\).*?NetworkManagers\.Add.*?CommunicationCallbackSubscriberLifecycle\.Instance\.StartLogin\(\)' `
    "Login starts callback shadow only after authentication and listeners"
Require-Match $loginProgram `
    'private static void StopLoginServers\(\).*?CommunicationCallbackSubscriberLifecycle\.Instance\.Stop\(\).*?networkManager\.StopServer\(\)' `
    "Login stops callback ownership before listeners"
Require-Match $loginProgram `
    'UnhandledExceptionHandler.*?StopLoginServers\(\).*?Process\.Start' `
    "Login stops callback ownership before crash restart"

Require-Match $scsTransport `
    'RegisterWorldServer\(.*?channelId\.HasValue.*?StartWorld\(\s*worldId,\s*channelId\.Value,\s*worldGroup\)' `
    "World starts callback shadow only after assigned registration"
Require-Match $scsTransport `
    'catch\s*\{\s*_serviceProxy\(\)\.UnregisterWorldServer\(worldId\);\s*throw;' `
    "World registration rolls back when callback shadow startup fails"
Require-Match $scsTransport `
    'UnregisterWorldServerAsync.*?CommunicationCallbackSubscriberLifecycle\.Instance\.Stop\(\);.*?_serviceProxy\(\)\.UnregisterWorldServer' `
    "World stops callback ownership before unregistering"

Require $protocol "RegisterCommunicationCallbackShadowWorld" `
    "Callback protocol exposes temporary World shadow registration"
Require $protocol "UnregisterCommunicationCallbackShadowWorld" `
    "Callback protocol exposes World shadow cleanup"
Require $protocol "cannot mutate accounts, sessions or channel assignment" `
    "Protocol records the callback-only authority boundary"
Require $shadowWorldValidator "ClusterNodeRole.World" `
    "Only World may validate a shadow route request"
Require $shadowWorldValidator "InvalidSubscriberIdentity" `
    "Malformed shadow World identity fails closed"
Require $callbackService "_hub.RegisterWorld" `
    "Shadow registration feeds only the callback routing hub"
Require $callbackService "_hub.UnregisterWorld" `
    "Shadow cleanup removes the callback route"
Forbid $callbackService "_state.RegisterWorldServer" `
    "Shadow registration cannot mutate authoritative communication state"
Require-Match $subscriber `
    'GetRuntimeInfoAsync.*?RegisterShadowWorldAsync.*?SubscribeCommunicationCallbacks' `
    "World registers its callback-only route before stream setup"
Require-Match $subscriber `
    'finally\s*\{.*?TryUnregisterShadowWorldAsync' `
    "World shadow route cleanup runs after every stream"
Require $subscriber "RegisterCommunicationCallbackShadowWorldAsync" `
    "Subscriber uses the typed shadow registration RPC"
Require $subscriber "UnregisterCommunicationCallbackShadowWorldAsync" `
    "Subscriber uses the typed shadow cleanup RPC"

Require $selfTest "Production callback subscriber is disabled by default" `
    "Activation default has a regression test"
Require $selfTest "Explicit callback activation starts only shadow observation" `
    "Shadow activation has a regression test"
Require $selfTest "Production callback effects remain blocked before atomic cutover" `
    "Application cutover remains guarded by a regression test"
Require $selfTest "Shadow callback handler records one validated envelope" `
    "Shadow observation has a runtime test"
Require $shadowWorldTest "World may register one callback-only shadow route" `
    "World shadow registration has a contract test"
Require $shadowWorldTest "Login cannot register a callback-only World route" `
    "Login shadow-route impersonation has a regression test"
Require $shadowWorldTest "Shadow World registration requires the assigned channel" `
    "World shadow registration requires exact assigned identity"

Require $documentation "SCS remains the only callback path allowed to execute" `
    "Documentation preserves one authoritative callback applier"
Require $documentation 'Setting it to `true` fails process initialization' `
    "Documentation records the application fail-closed boundary"
Require $documentation "RegisterCommunicationCallbackShadowWorld" `
    "Documentation records callback-only World routing"
Require $documentation "Production Master does not yet mirror" `
    "Documentation does not overstate live shadow coverage"
Require $documentation "server-issued replay-complete barrier" `
    "Documentation names the atomic cutover requirement"

Write-Host `
    "NosGM Login and World callback shadow lifecycle and routing contracts passed." `
    -ForegroundColor Green
