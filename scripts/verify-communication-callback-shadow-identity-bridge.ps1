[CmdletBinding()]
param(
    [string]$FallbackPath = "Data/NosGm.Authentication.Client/Communication/CommunicationCallbackExistingIdentityFallback.cs",
    [string]$ActivationPath = "Data/NosGm.Authentication.Client/Communication/CommunicationCallbackActivationOptions.cs",
    [string]$MasterIdentityPath = "Data/NosGm.Authentication.Client/Communication/MasterCommunicationGrpcIdentityOptions.cs",
    [string]$WrapperPath = "scripts/start-communication-callback-shadow-local.ps1",
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
$cutoverDocument = Read-Required $CutoverDocumentPath
$masterIdentityCompact = $masterIdentity -replace '\s+', ''

Assert-PowerShellParses $WrapperPath

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

Require $wrapper 'NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED' 'The shadow wrapper must enable the callback subscriber explicitly.'
Require $wrapper 'NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED' 'The shadow wrapper must set the separate callback effect switch.'
Require $wrapper '"false"' 'The shadow wrapper must keep typed callback effect application disabled.'
Require $wrapper 'NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_ENABLED' 'The shadow wrapper must enable Master typed publication.'
Require $wrapper 'AuthenticationTransport = "GRPC"' 'The shadow wrapper must give Login its role-separated generic gRPC identity before fallback bridging.'
Require $wrapper 'CommunicationCallbackEffectAuthority' 'Runtime state must expose that SCS is still effect-authoritative in this slice.'
Require $wrapper '"SCS"' 'Runtime state must record SCS callback effect authority during shadow acceptance.'
Require $wrapper '$previousEnvironment[$name]' 'The wrapper must restore every temporary parent-shell callback variable.'
Forbid $wrapper 'NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED",`n        "true"' 'Shadow acceptance must never enable typed callback effects.'

Require $cutoverDocument 'Slice 1: reproducible local shadow wiring' 'The cutover document must preserve the staged shadow boundary.'
Require $cutoverDocument 'Slice 2: PenaltyRefresh authority cutover' 'The final SCS suppression must remain a separate validated slice.'
Require $cutoverDocument 'Configuration remains gRPC-only' 'The new callback work must not regress the completed Configuration authority.'

Write-Host 'Communication callback role-separated shadow identity bridge contracts passed.' -ForegroundColor Green
