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
    'SCS callback remains authoritative',
    'no gameplay state was applied'
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

Require-Text $project 'Client\ConfigurationGrpcShadowMirror.cs' "Configuration shadow mirror compile item"
Require-Text $project 'Client\ConfigurationGrpcShadowOptions.cs' "Configuration shadow options compile item"
Require-Text $project 'Client\ConfigurationGrpcShadowSubscriberLifecycle.cs' "Configuration subscriber lifecycle compile item"

Write-Host "[PASS] Configuration gRPC shadow mode is explicit, disabled by default and timeout-bounded." -ForegroundColor Green
Write-Host "[PASS] SCS Get/Update remain authoritative and execute before shadow synchronization." -ForegroundColor Green
Write-Host "[PASS] Configuration shadow compares before writing and tolerates gRPC timeout/failure." -ForegroundColor Green
Write-Host "[PASS] Legacy DateTime conversion handles Unspecified values as local wall time explicitly." -ForegroundColor Green
Write-Host "[PASS] ConfigurationUpdated remains SCS-authoritative while the typed stream is observation-only." -ForegroundColor Green
