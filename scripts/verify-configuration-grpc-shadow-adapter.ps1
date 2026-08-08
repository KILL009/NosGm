[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Read-RepoFile {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Expected Configuration shadow-adapter file is missing: $RelativePath"
    }
    return [System.IO.File]::ReadAllText($path)
}

function Require-Text {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($Content.IndexOf($Expected, [StringComparison]::Ordinal) -lt 0) {
        throw "$Name is missing '$Expected'."
    }
}

function Require-Before {
    param(
        [Parameter(Mandatory = $true)][string]$Content,
        [Parameter(Mandatory = $true)][string]$First,
        [Parameter(Mandatory = $true)][string]$Second,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $firstIndex = $Content.IndexOf($First, [StringComparison]::Ordinal)
    $secondIndex = $Content.IndexOf($Second, [StringComparison]::Ordinal)
    if ($firstIndex -lt 0 -or $secondIndex -lt 0 -or $firstIndex -ge $secondIndex) {
        throw "$Name must keep '$First' before '$Second'."
    }
}

$options = Read-RepoFile "Data\NosGm.Master.Library\Client\ConfigurationGrpcShadowOptions.cs"
$mirror = Read-RepoFile "Data\NosGm.Master.Library\Client\ConfigurationGrpcShadowMirror.cs"
$client = Read-RepoFile "Data\NosGm.Master.Library\Client\ConfigurationServiceClient.cs"
$subscriberLifecycle = Read-RepoFile "Data\NosGm.Master.Library\Client\ConfigurationGrpcShadowSubscriberLifecycle.cs"
$project = Read-RepoFile "Data\NosGm.Master.Library\NosGm.Master.Library.csproj"

foreach ($required in @(
    'NOSGM_CONFIGURATION_GRPC_SHADOW_ENABLED',
    'NOSGM_CONFIGURATION_GRPC_SHADOW_TIMEOUT_MS',
    'bool enabled = false;',
    'DefaultTimeoutMilliseconds = 1500',
    'MinimumTimeoutMilliseconds = 100',
    'MaximumTimeoutMilliseconds = 10000'
)) {
    Require-Text $options $required "Configuration shadow options"
}

foreach ($required in @(
    'AuthenticationGrpcClientOptions.Load(ClusterNodeRole.World)',
    'new CancellationTokenSource(_timeoutMilliseconds)',
    '.GetAwaiter()',
    '.GetResult()',
    'ConfigurationTransportResultCode.Unavailable',
    'ConfigurationGrpcShadowStatus.Matched',
    'ConfigurationGrpcShadowStatus.Seeded',
    'ConfigurationGrpcShadowStatus.Resynchronized',
    'catch (OperationCanceledException)',
    'catch (Exception)',
    'TryGetAuthoritative',
    'TryUpdateAuthoritative',
    'FromTransportSnapshot',
    'RuntimeGenerationId = result.RuntimeGenerationId',
    'value.Kind == DateTimeKind.Unspecified',
    'TimeZoneInfo.Local.GetUtcOffset(value)'
)) {
    Require-Text $mirror $required "Configuration shadow mirror"
}
Require-Before $mirror '_transport.GetAsync(cancellation.Token)' '_transport.UpdateAsync(expected, cancellation.Token)' "Configuration shadow compare-before-write"

foreach ($forbidden in @('MasterAuthKey', 'ConfigurationUpdated', 'GameConfiguration')) {
    if ($mirror.IndexOf($forbidden, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "Configuration shadow mirror contains forbidden legacy coupling '$forbidden'."
    }
}

Require-Before $client '_client.ServiceProxy.GetConfigurationObject()' 'ObserveAuthoritativeConfiguration(authoritative, "Get")' "Configuration Get authority order"
Require-Before $client '_client.ServiceProxy.UpdateConfigurationObject(configurationObject)' 'ObserveAuthoritativeConfiguration(configurationObject, "Update")' "Configuration Update authority order"
Require-Before $client 'ObserveAuthoritativeConfiguration(authoritative, "Get")' 'return authoritative;' "Configuration Get preserves the SCS object"
Require-Text $client 'SCS remains authoritative' "Configuration shadow authority log"
Require-Text $client 'SCS result remains authoritative' "Configuration shadow failure log"
Require-Text $client 'Typed Configuration update subscriber started' "Configuration subscriber startup"
foreach ($required in @(
    'ConfigurationGrpcShadowSubscriber',
    'RecoveredFromSnapshot',
    'ledger.RecordGrpc(update)',
    'ConfigurationUpdateParityDiagnostics.Observe',
    '_authorityCallback(update)',
    'Applied typed Configuration update',
    'current authority selector applied no typed gameplay effect'
)) {
    Require-Text $subscriberLifecycle $required "Configuration shadow subscriber lifecycle"
}

$callbackPattern = '(?s)internal void OnConfigurationUpdated\(ConfigurationObject configurationObject\)\s*\{(?<body>.*?)\n\s*\}'
$callbackMatch = [regex]::Match($client, $callbackPattern)
if (-not $callbackMatch.Success) {
    throw "Unable to locate ConfigurationUpdated callback body."
}
if ($callbackMatch.Groups['body'].Value.IndexOf('ObserveAuthoritativeConfiguration', [StringComparison]::Ordinal) -ge 0) {
    throw "ConfigurationUpdated callback must not mirror into gRPC during this slice."
}
Require-Text $callbackMatch.Groups['body'].Value 'ObserveScsConfigurationCallback(configurationObject)' "ConfigurationUpdated SCS observation"
Require-Before $client 'ObserveScsConfigurationCallback(configurationObject)' 'ConfigurationAuthorityCoordinator.Instance.TryApplyCallback' "Configuration callback observation order"
foreach ($required in @(
    'ConfigurationAuthorityOperation.Get',
    'ConfigurationAuthorityOperation.Update',
    'ConfigurationAuthorityOperation.Callback',
    'TryGetAuthoritative',
    'TryUpdateAuthoritative',
    'IsCurrentAuthorityResult',
    'SynchronizeScsStandby',
    'SCS rollback standby',
    'RollBackAuthority',
    'ConfigurationAuthoritySource.Scs',
    'ConfigurationAuthoritySource.TypedGrpc'
)) {
    Require-Text $client $required "Configuration joint authority client"
}
Require-Text $client 'ledger.RecordScs' "Configuration SCS callback ledger"
Require-Before $client 'ledger.RecordScs' 'ConfigurationUpdateParityDiagnostics.Observe' "Configuration SCS parity evaluation order"
Require-Before $subscriberLifecycle 'ledger.RecordGrpc(update)' 'ConfigurationUpdateParityDiagnostics.Observe' "Configuration gRPC parity evaluation order"
Require-Text $subscriberLifecycle 'report.HasTerminalMismatch' "Configuration terminal parity diagnostic severity"
Require-Text $subscriberLifecycle 'authority selection is evaluated separately and this evidence has no direct gameplay effect' "Configuration parity evidence isolation"
Require-Text $client 'the authoritative callback will continue unchanged' "Configuration SCS observation isolation"
Require-Text $subscriberLifecycle 'failed closed to SCS' "Configuration gRPC authority failure isolation"

Require-Text $project 'Client\ConfigurationGrpcShadowMirror.cs' "Configuration shadow mirror compile item"
Require-Text $project 'Client\ConfigurationGrpcShadowOptions.cs' "Configuration shadow options compile item"
Require-Text $project 'Client\ConfigurationGrpcShadowSubscriberLifecycle.cs' "Configuration subscriber lifecycle compile item"

Write-Host "[PASS] Configuration gRPC shadow mode is explicit, disabled by default and timeout-bounded." -ForegroundColor Green
Write-Host "[PASS] SCS Get/Update remain the default and shadow synchronization follows SCS fallback." -ForegroundColor Green
Write-Host "[PASS] Configuration shadow compares before writing and tolerates gRPC timeout/failure." -ForegroundColor Green
Write-Host "[PASS] Legacy DateTime conversion handles Unspecified values as local wall time explicitly." -ForegroundColor Green
Write-Host "[PASS] ConfigurationUpdated and the typed stream share the atomic authority selector." -ForegroundColor Green
Write-Host "[PASS] SCS and gRPC callbacks enter the bounded parity ledger without duplicating gameplay effects." -ForegroundColor Green
Write-Host "[PASS] Typed authority failures return Get, Update and callback together to SCS." -ForegroundColor Green
