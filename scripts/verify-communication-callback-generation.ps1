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
        throw "Expected callback generation file was not found: $RelativePath"
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

$protocol = Read-RequiredFile `
    "contracts\cluster\v1\cluster_communication_callbacks.proto"
$runtimeIdentity = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\State\CommunicationCallbackRuntimeIdentity.cs"
$runtimeProgram = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Program.cs"
$runtimeService = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Services\ClusterCommunicationCallbackService.cs"
$runtimeValidator = Read-RequiredFile `
    "Data\NosGm.Cluster.Contracts\Communication\V1\CommunicationCallbackRuntimeInfoContractValidator.cs"
$cursor = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackCursorStore.cs"
$processor = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackProcessor.cs"
$subscriber = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\GrpcCommunicationCallbackSubscriber.cs"
$lifecycleHost = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackSubscriberHost.cs"
$generationTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackRuntimeGenerationSelfTest.cs"
$cursorTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackSubscriberSelfTest.cs"
$hostTest = Read-RequiredFile `
    "tests\NosGm.Authentication.Runtime.SelfTest\CommunicationCallbackSubscriberHostSelfTest.cs"
$documentation = Read-RequiredFile `
    "docs\communication-callback-subscriber.md"

Require $protocol "GetCommunicationCallbackRuntimeInfo" `
    "Callback protocol exposes a runtime-generation query"
Require $protocol "runtime_generation_id = 7" `
    "Callback subscription carries the queried runtime generation"
Require $protocol "started_at_unix_time_ms" `
    "Runtime generation metadata includes process start time"
Require $protocol "current_sequence" `
    "Runtime generation metadata exposes the current sequence"

Require $runtimeIdentity "GenerationId = Guid.NewGuid();" `
    "Every runtime process creates a fresh generation"
Require $runtimeIdentity "StartedAt = timeProvider.GetUtcNow();" `
    "Runtime generation records its startup time"
Require $runtimeProgram "AddSingleton<CommunicationCallbackRuntimeIdentity>" `
    "Runtime generation identity is process-wide"
Require $runtimeProgram "callbackRuntimeIdentity.GenerationId" `
    "Runtime startup log exposes the callback generation"

Require $runtimeValidator "ClusterNodeRole.Login" `
    "Login may query runtime generation metadata"
Require $runtimeValidator "ClusterNodeRole.World" `
    "World may query runtime generation metadata"
Require $runtimeValidator "InvalidCallerRole" `
    "Other roles fail the generation query closed"
Require $runtimeService "GetCommunicationCallbackRuntimeInfo" `
    "Runtime serves authenticated generation metadata"
Require $runtimeService '_runtimeIdentity.GenerationId.ToString("D")' `
    "Runtime returns a canonical generation GUID"
Require $runtimeService "request.RuntimeGenerationId" `
    "Stream setup validates the caller generation"
Require $runtimeService "The callback runtime generation changed before the stream opened." `
    "A restart race fails stream setup closed"

Require $cursor "NOSGM_CALLBACK_CURSOR_V1" `
    "Durable cursor has an explicit versioned format"
Require $cursor "ICommunicationCallbackGenerationCursorStore" `
    "Cursor store exposes generation binding"
Require $cursor "BindRuntimeGeneration" `
    "Cursor sequence is loaded through generation binding"
Require $cursor "lines.Length == 1" `
    "Legacy sequence-only cursors have an explicit migration path"
Require $cursor "return 0;" `
    "Unknown generations begin at zero"
Require $cursor "FileOptions.WriteThrough" `
    "Generation cursor preserves durable writes"
Require $cursor "File.Replace" `
    "Generation cursor preserves atomic replacement"
Forbid $cursor "JsonConvert" `
    "Generation cursor avoids permissive object deserialization"

Require $processor "BindRuntimeGeneration" `
    "Callback processor binds before consuming a generation"
Require $processor "generationStore.BindRuntimeGeneration" `
    "Processor loads only the selected generation cursor"
Require $processor "has no bound runtime generation" `
    "Generation-aware processing fails closed before binding"
Require $subscriber "GetRuntimeInfoAsync" `
    "Subscriber queries generation before every stream"
Require $subscriber "GetCommunicationCallbackRuntimeInfoAsync" `
    "Subscriber uses the generated unary generation RPC"
Require $subscriber "RuntimeGenerationId = runtimeGenerationId" `
    "Subscriber sends the same generation into stream setup"
Require $subscriber "StatusCode.DataLoss" `
    "Malformed generation metadata is terminal"
Require $subscriber "StatusCode.FailedPrecondition" `
    "Runtime restart races enter controlled reconnection"

Require $lifecycleHost "CommunicationCallbackSubscriberHostState" `
    "Lifecycle owner exposes explicit states"
Require $lifecycleHost "The communication callback subscriber host can be started only once." `
    "Lifecycle owner forbids duplicate starts"
Require $lifecycleHost "CancellationTokenSource" `
    "Lifecycle owner controls subscriber cancellation"
Require $lifecycleHost "LastException" `
    "Lifecycle owner exposes terminal failure"
Require $lifecycleHost "Stop(TimeSpan timeout)" `
    "Lifecycle shutdown has a bounded wait"
Forbid $lifecycleHost "Thread.Abort" `
    "Lifecycle shutdown never aborts threads"

Require $generationTest "Every callback runtime process receives a distinct generation" `
    "Runtime generation uniqueness has a regression test"
Require $cursorTest "A new runtime generation never inherits the previous sequence" `
    "Generation changes reset subscriber replay state"
Require $cursorTest "A legacy unscoped cursor migrates safely from zero" `
    "Legacy cursor migration has a regression test"
Require $hostTest "Callback lifecycle host stops within its bounded deadline" `
    "Lifecycle cancellation has a regression test"
Require $hostTest "Callback lifecycle host exposes terminal subscriber faults" `
    "Lifecycle fault visibility has a regression test"
Require $documentation "runtime-generation scoped" `
    "Documentation records the generation ownership boundary"
Require $documentation "Production remains on the SCS callback path." `
    "Generation work does not activate production callbacks"

Write-Host `
    "NosGM callback runtime generation, cursor ownership and lifecycle contracts passed." `
    -ForegroundColor Green
