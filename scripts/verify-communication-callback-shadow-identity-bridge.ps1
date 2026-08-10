[CmdletBinding()]
param(
    [string]$FallbackPath = "Data/NosGm.Authentication.Client/Communication/CommunicationCallbackExistingIdentityFallback.cs",
    [string]$MasterIdentityPath = "Data/NosGm.Authentication.Client/Communication/MasterCommunicationGrpcIdentityOptions.cs",
    [string]$AuthorityStartupPath = "scripts/start-penaltyrefresh-grpc-authority-local.ps1",
    [string]$CompatibilityStartupPath = "scripts/start-communication-callback-shadow-local.ps1",
    [string]$ProbeScriptPath = "scripts/test-communication-callback-shadow-local.ps1",
    [string]$ProbeSelfTestPath = "tests/NosGm.Authentication.Runtime.SelfTest/CommunicationPenaltyRefreshLiveProbeSelfTest.cs",
    [string]$DispatcherPath = "Data/NosGm.Master.Library/Client/CommunicationCallbackEnvelopeDispatcher.cs",
    [string]$ClientInterfacePath = "Data/NosGm.Master.Library/Interface/ICommunicationClient.cs",
    [string]$MigrationMapPath = "contracts/cluster/v1/communication-callback-migration-map.json"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Read-Required([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing PenaltyRefresh authority file: $Path"
    }
    return Get-Content -LiteralPath $Path -Raw
}

function Require([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "PenaltyRefresh authority contract failed: $Description"
    }
    Write-Host "[PASS] $Description" -ForegroundColor Green
}

function Forbid([string]$Content, [string]$Needle, [string]$Description) {
    if ($Content.IndexOf($Needle, [StringComparison]::Ordinal) -ge 0) {
        throw "PenaltyRefresh authority contract failed: $Description"
    }
    Write-Host "[PASS] $Description" -ForegroundColor Green
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
$masterIdentity = Read-Required $MasterIdentityPath
$authorityStartup = Read-Required $AuthorityStartupPath
$compatibilityStartup = Read-Required $CompatibilityStartupPath
$probeScript = Read-Required $ProbeScriptPath
$probeSelfTest = Read-Required $ProbeSelfTestPath
$dispatcher = Read-Required $DispatcherPath
$clientInterface = Read-Required $ClientInterfacePath
$migrationMap = Read-Required $MigrationMapPath

$masterIdentityCompact = $masterIdentity -replace '\s+', ''
$authorityStartupCompact = $authorityStartup -replace '\s+', ''
$probeSelfTestCompact = $probeSelfTest -replace '\s+', ''

Assert-PowerShellParses $AuthorityStartupPath
Assert-PowerShellParses $CompatibilityStartupPath
Assert-PowerShellParses $ProbeScriptPath

Require $fallback 'NOSGM_COMMUNICATION_GRPC_USE_EXISTING_IDENTITY_FALLBACK' 'Role-separated callback identity reuse remains explicit.'
Require $fallback 'AuthenticationGrpcClientOptions.CertificatePathVariable' 'Subscriber certificate remains process-scoped.'
Require $fallback 'CommunicationCallbackSubscriberOptions.CursorPathVariable' 'Login and World keep dedicated durable callback cursors.'
Require $fallback 'SHA256.Create()' 'Cursor file names remain bounded hashes of caller identities.'
Forbid $fallback 'AuthenticationGrpcClientOptions.TrustedRootCertificatePathVariable' 'Legacy net481 subscribers do not inherit file-scoped root pinning.'

Require $masterIdentityCompact 'ConfigurationRuntimeControllerIdentityOptions.CertificatePathVariable' 'Master reuses only its role-separated mTLS identity when explicitly bridged.'
Require $masterIdentityCompact 'ConfigurationRuntimeControllerIdentityOptions.CallerInstanceIdVariable' 'Master callback publication preserves its process identity.'
Require $masterIdentity '!string.IsNullOrEmpty(dedicated)' 'Dedicated callback Master credentials retain priority.'

Require $authorityStartupCompact '[Environment]::SetEnvironmentVariable("NOSGM_COMMUNICATION_GRPC_USE_EXISTING_IDENTITY_FALLBACK","true",[EnvironmentVariableTarget]::Process)' 'Final startup enables the role-separated identity bridge.'
Require $authorityStartupCompact '[Environment]::SetEnvironmentVariable("NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED","true",[EnvironmentVariableTarget]::Process)' 'Final startup requires the callback subscribers.'
Require $authorityStartupCompact '[Environment]::SetEnvironmentVariable("NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED","false",[EnvironmentVariableTarget]::Process)' 'Other typed callbacks remain observation-only.'
Forbid $authorityStartupCompact '[Environment]::SetEnvironmentVariable("NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED","true",[EnvironmentVariableTarget]::Process)' 'Final startup never opens broad typed callback application.'
Require $authorityStartupCompact 'AuthenticationTransport="GRPC"' 'Login and World receive their role-separated gRPC identities.'
Require $authorityStartup 'CommunicationCallbackMode -NotePropertyValue "PenaltyRefreshAuthority"' 'Runtime state records the final PenaltyRefresh mode.'
Require $authorityStartup 'PenaltyRefreshCallbackAuthority -NotePropertyValue "gRPC"' 'Runtime state records gRPC as PenaltyRefresh authority.'
Require $authorityStartup 'PenaltyRefreshCallbackFallback -NotePropertyValue $null' 'Runtime state records no PenaltyRefresh fallback.'
Require $authorityStartup 'RemainingCommunicationCallbackAuthority -NotePropertyValue "SCS"' 'Other callbacks retain SCS authority.'
Require $authorityStartup '$previousEnvironment[$name]' 'Final startup restores temporary parent-shell variables.'

Require $compatibilityStartup 'start-penaltyrefresh-grpc-authority-local.ps1' 'Historical shadow startup redirects to the final authority startup.'
Forbid $compatibilityStartup 'CommunicationCallbackEffectAuthority' 'Compatibility startup no longer claims global SCS callback authority.'

Require $dispatcher 'kind == WireV1.CommunicationCallbackKind.PenaltyRefresh' 'Dispatcher isolates the completed PenaltyRefresh authority slice.'
Require $dispatcher 'ApplyCore(envelope);' 'PenaltyRefresh applies directly from the typed stream.'
Require $dispatcher 'CommunicationCallbackParitySource.TypedGrpc' 'Other callback kinds retain the transitional coordinator.'
Forbid $clientInterface 'void UpdatePenaltyLog(int penaltyLogId);' 'UpdatePenaltyLog is absent from the SCS callback interface.'
Require $clientInterface 'PenaltyRefresh is gRPC-authoritative and has no SCS fallback' 'Dead legacy calls fail closed.'

Require $probeSelfTest '"--live-penalty-refresh-probe"' 'Acceptance uses a dedicated PenaltyRefresh probe mode.'
Require $probeSelfTest 'ObservationOnlyPenaltyLogId = int.MaxValue' 'Acceptance uses the reserved nonexistent positive PenaltyLogId.'
Require $probeSelfTestCompact 'Kind=WireV1.CommunicationCallbackTargetKind.AllNodes' 'Acceptance exercises the ALL_NODES route.'
Require $probeSelfTestCompact 'PenaltyRefresh=newWireV1.PenaltyRefreshCallback' 'Acceptance publishes the typed PenaltyRefresh payload.'
Require $probeSelfTest 'response.MatchedSubscribers < 2' 'Acceptance requires both Login and World routes to be live.'
Forbid $probeSelfTest 'PenaltyLogDAO' 'Probe publisher never mutates or inspects the penalty database.'
Forbid $probeSelfTest 'CommunicationServiceClient.Instance' 'Probe publisher bypasses gameplay effect surfaces.'

Require $probeScript '[string]$state.CommunicationCallbackMode -ne "PenaltyRefreshAuthority"' 'Acceptance refuses non-final callback runtime state.'
Require $probeScript '[string]$state.PenaltyRefreshCallbackAuthority -ne "gRPC"' 'Acceptance requires gRPC PenaltyRefresh authority.'
Require $probeScript '$null -ne $state.PenaltyRefreshCallbackFallback' 'Acceptance requires a null PenaltyRefresh fallback.'
Require $probeScript 'Get-CallbackCursorPath "login-local-1"' 'Acceptance observes the Login durable cursor.'
Require $probeScript 'Get-CallbackCursorPath "world-local-1"' 'Acceptance observes the World durable cursor.'
Require $probeScript 'Test-CursorAdvanced $loginBaseline $loginCurrent $acceptedSequence' 'Login must durably commit the accepted sequence.'
Require $probeScript 'Test-CursorAdvanced $worldBaseline $worldCurrent $acceptedSequence' 'World must durably commit the accepted sequence.'
Require $probeScript 'PenaltyRefresh authority is typed gRPC with no SCS callback fallback.' 'Acceptance reports the final authority invariant.'
Require $probeScript 'Communication PenaltyRefresh real-process gRPC authority acceptance passed.' 'Acceptance has an unambiguous final success marker.'
Forbid $probeScript 'observation-only' 'Final acceptance no longer describes PenaltyRefresh as shadow-only.'

Require $migrationMap '"schemaVersion": 2' 'Migration map uses the completed callback schema.'
Require $migrationMap '"disposition": "grpc_authoritative"' 'Migration map records PenaltyRefresh gRPC authority.'
Require $migrationMap '"legacySurfaceRemoved": true' 'Migration map records SCS callback removal.'
Require $migrationMap '"fallback": null' 'Migration map records no PenaltyRefresh fallback.'

Write-Host 'PenaltyRefresh final gRPC authority, role-separated identity, and real-process acceptance contracts passed.' -ForegroundColor Green
