[CmdletBinding()]
param(
    [string]$FallbackPath = "Data/NosGm.Authentication.Client/Communication/CommunicationCallbackExistingIdentityFallback.cs",
    [string]$ActivationPath = "Data/NosGm.Authentication.Client/Communication/CommunicationCallbackActivationOptions.cs",
    [string]$MasterIdentityPath = "Data/NosGm.Authentication.Client/Communication/MasterCommunicationGrpcIdentityOptions.cs",
    [string]$WrapperPath = "scripts/start-communication-callback-shadow-local.ps1",
    [string]$ProbeScriptPath = "scripts/test-communication-callback-shadow-local.ps1",
    [string]$ProbeSelfTestPath = "tests/NosGm.Authentication.Runtime.SelfTest/CommunicationPenaltyRefreshLiveProbeSelfTest.cs",
    [string]$CutoverDocumentPath = "docs/penaltyrefresh-grpc-authority-cutover.md"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Required([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing communication callback shadow bridge file: $Path"
    }
    return Get-Content -LiteralPath $Path -Raw
}

function Require([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Communication callback shadow bridge contract failed: $Description"
    }
}

function Forbid([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "Communication callback shadow bridge contract failed: $Description"
    }
}

function Assert-PowerShellParses([string]$Path) {
    $tokens = $null
    $parseErrors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile(
        (Resolve-Path -LiteralPath $Path),
        [ref]$tokens,
        [ref]$parseErrors)
    if (@($parseErrors).Count -gt 0) {
        $messages = $parseErrors | ForEach-Object {
            "$($_.Extent.StartLineNumber): $($_.Message)"
        }
        throw "PowerShell parse errors in ${Path}:`n$($messages -join "`n")"
    }
}

$fallback = Read-Required $FallbackPath
$activation = Read-Required $ActivationPath
$masterIdentity = Read-Required $MasterIdentityPath
$wrapper = Read-Required $WrapperPath
$probeScript = Read-Required $ProbeScriptPath
$probeSelfTest = Read-Required $ProbeSelfTestPath
$cutoverDocument = Read-Required $CutoverDocumentPath
$masterIdentityCompact = $masterIdentity -replace '\s+', ''
$wrapperCompact = $wrapper -replace '\s+', ''
$probeScriptCompact = $probeScript -replace '\s+', ''
$probeSelfTestCompact = $probeSelfTest -replace '\s+', ''

Assert-PowerShellParses $WrapperPath
Assert-PowerShellParses $ProbeScriptPath

Require $fallback 'NOSGM_COMMUNICATION_GRPC_USE_EXISTING_IDENTITY_FALLBACK' 'The identity bridge must remain explicitly opt-in.'
Require $fallback 'return false;' 'The identity bridge must default to disabled.'
Require $fallback 'AuthenticationGrpcClientOptions.AddressVariable' 'Subscriber address must come from the process-scoped existing gRPC identity.'
Require $fallback 'AuthenticationGrpcClientOptions.CertificatePathVariable' 'Subscriber certificate path must come from the process-scoped existing gRPC identity.'
Require $fallback 'AuthenticationGrpcClientOptions.CertificatePasswordVariable' 'Subscriber certificate password must stay inside the same child process.'
Require $fallback 'AuthenticationGrpcClientOptions.CallerInstanceIdVariable' 'Subscriber caller identity must remain process-scoped.'
Require $fallback 'AuthenticationGrpcClientOptions.DeadlineVariable' 'Subscriber setup must preserve the bounded gRPC deadline.'
Require $fallback 'AuthenticationGrpcClientOptions.WireModeVariable' 'Subscriber must preserve the preselected HTTP2 or gRPC-Web wire mode.'
Require $fallback 'Environment.SpecialFolder.LocalApplicationData' 'Fallback callback cursors must live outside the repository in local application data.'
Require $fallback 'SHA256.Create()' 'Fallback cursor file names must not embed arbitrary caller identity text.'
Require $fallback 'CommunicationCallbackSubscriberOptions.CursorPathVariable' 'The bridge must provide a dedicated durable callback cursor path.'
Forbid $fallback 'AuthenticationGrpcClientOptions.TrustedRootCertificatePathVariable' 'The net481 callback subscriber must not inherit file-scoped root pinning from the generic identity.'

Require $activation 'CommunicationCallbackExistingIdentityFallback.IsEnabled()' 'Callback activation must consult the explicit identity bridge switch.'
Require $activation '.PrepareSubscriberEnvironment();' 'Callback activation must prepare the role-specific subscriber namespace before subscriber options load.'
Require $activation 'usesProcessEnvironment' 'Dictionary-backed self-tests must not mutate the real process environment.'

Require $masterIdentityCompact 'ConfigurationRuntimeControllerIdentityOptions.CertificatePathVariable' 'Master callback publication must fall back only to the existing Master certificate identity.'
Require $masterIdentityCompact 'ConfigurationRuntimeControllerIdentityOptions.CertificatePasswordVariable' 'Master callback publication must keep the Master certificate password role-scoped.'
Require $masterIdentityCompact 'ConfigurationRuntimeControllerIdentityOptions.CallerInstanceIdVariable' 'Master callback publication must preserve the existing Master process identity.'
Require $masterIdentityCompact 'ConfigurationRuntimeControllerIdentityOptions.AddressVariable' 'Master callback publication must use the existing Master Configuration endpoint only as an explicit fallback.'
Require $masterIdentity 'CommunicationCallbackExistingIdentityFallback.IsEnabled' 'Master identity reuse must remain explicitly opt-in.'
Require $masterIdentity '!string.IsNullOrEmpty(dedicated)' 'Dedicated callback Master credentials must retain priority over fallback values.'

Require $wrapperCompact '[Environment]::SetEnvironmentVariable("NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED","true",[EnvironmentVariableTarget]::Process)' 'The shadow wrapper must enable the callback subscriber explicitly.'
Require $wrapperCompact '[Environment]::SetEnvironmentVariable("NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED","false",[EnvironmentVariableTarget]::Process)' 'The shadow wrapper must keep typed callback effect application disabled.'
Forbid $wrapperCompact '[Environment]::SetEnvironmentVariable("NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED","true",[EnvironmentVariableTarget]::Process)' 'Shadow acceptance must never enable typed callback effects.'
Require $wrapperCompact '[Environment]::SetEnvironmentVariable("NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_ENABLED","true",[EnvironmentVariableTarget]::Process)' 'The shadow wrapper must enable Master typed publication.'
Require $wrapperCompact 'AuthenticationTransport="GRPC"' 'The shadow wrapper must give Login its role-separated generic gRPC identity before fallback bridging.'
Require $wrapper 'CommunicationCallbackEffectAuthority' 'Runtime state must expose that SCS is still effect-authoritative in this slice.'
Require $wrapper '"SCS"' 'Runtime state must record SCS callback effect authority during shadow acceptance.'
Require $wrapper '$previousEnvironment[$name]' 'The wrapper must restore every temporary parent-shell callback variable.'

Require $probeSelfTest '"--live-penalty-refresh-probe"' 'The live probe must use a dedicated command-line mode and never piggyback on the broad live suite.'
Require $probeSelfTest 'ObservationOnlyPenaltyLogId = int.MaxValue' 'The probe must use the reserved observation-only positive PenaltyLogId.'
Require $probeSelfTestCompact 'Kind=WireV1.CommunicationCallbackTargetKind.AllNodes' 'PenaltyRefresh must exercise the Login plus World ALL_NODES route.'
Require $probeSelfTestCompact 'PenaltyRefresh=newWireV1.PenaltyRefreshCallback' 'The live probe must publish the typed PenaltyRefresh payload.'
Require $probeSelfTest 'response.MatchedSubscribers < 2' 'The publisher must fail unless Login and World routes are both attached.'
Require $probeSelfTest '[CALLBACK_PENALTY_PROBE]' 'The probe must emit a machine-readable accepted-sequence marker.'
Forbid $probeSelfTest 'PenaltyLogDAO' 'The typed shadow probe must never touch the penalty database.'
Forbid $probeSelfTest 'OnUpdatePenaltyLog' 'The typed shadow probe must never invoke the legacy penalty effect directly.'
Forbid $probeSelfTest 'CommunicationServiceClient.Instance' 'The typed shadow probe must bypass every gameplay effect surface.'

Require $probeScript '[string]$state.CommunicationCallbackEffectAuthority -ne "SCS"' 'The acceptance script must refuse to run unless SCS remains effect authority.'
Require $probeScript 'Get-CallbackCursorPath "login-local-1"' 'The acceptance script must observe the Login role cursor.'
Require $probeScript 'Get-CallbackCursorPath "world-local-1"' 'The acceptance script must observe the World role cursor.'
Require $probeScript '--live-penalty-refresh-probe' 'The acceptance script must invoke only the dedicated PenaltyRefresh probe.'
Require $probeScript 'Test-CursorAdvanced $loginBaseline $loginCurrent $acceptedSequence' 'Login delivery must be proven by a durable cursor advance to the accepted sequence.'
Require $probeScript 'Test-CursorAdvanced $worldBaseline $worldCurrent $acceptedSequence' 'World delivery must be proven by a durable cursor advance to the accepted sequence.'
Require $probeScript '$loginCurrent.Generation -ne $worldCurrent.Generation' 'Login and World must commit against the same callback runtime generation.'
Require $probeScript 'NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PATH' 'The probe publisher must use the Master callback certificate namespace.'
Require $probeScript 'NOSGM_COMMUNICATION_GRPC_TRUSTED_ROOT_CERT_PATH' 'The .NET 10 probe must pin the local root without changing the net481 subscriber trust behavior.'
Require $probeScript '-p:UseSharedCompilation=false' 'The local probe build must avoid compiler-server file locks.'
Require $probeScript '$previousEnvironment[$name]' 'The probe must restore temporary Master publisher credentials from its shell.'
Forbid $probeScript 'NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED' 'The acceptance probe must never mutate the running child-process APPLY policy.'
Forbid $probeScript 'PenaltyLogDAO' 'The PowerShell acceptance path must not inspect or mutate the penalty database.'

Require $cutoverDocument 'Slice 1: reproducible local shadow wiring' 'The cutover document must preserve the staged shadow boundary.'
Require $cutoverDocument 'Slice 2: PenaltyRefresh authority cutover' 'The final SCS suppression must remain a separate validated slice.'
Require $cutoverDocument 'Configuration remains gRPC-only' 'The new callback work must not regress the completed Configuration authority.'

Write-Host 'Communication callback role-separated shadow identity and PenaltyRefresh probe contracts passed.' -ForegroundColor Green
