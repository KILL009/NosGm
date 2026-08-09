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
        throw "Expected callback authority file was not found: $RelativePath"
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
        throw "$Name does not match the required callback ordering."
    }
    Write-Host "[PASS] $Name" -ForegroundColor Green
}

$options = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\CommunicationCallbackMirrorOptions.cs"
$publisher = Read-RequiredFile `
    "Data\NosGm.Authentication.Client\Communication\GrpcCommunicationCallbackPublisher.cs"
$mirror = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Master.Server\MasterCommunicationCallbackMirror.cs"
$service = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Master.Server\MirroredCommunicationService.cs"
$program = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Master.Server\Program.cs"
$project = Read-RequiredFile `
    "Data\NosGm.Program\NosGm.Master.Server\NosGm.Master.Server.csproj"
$clientInterface = Read-RequiredFile `
    "Data\NosGm.Master.Library\Interface\ICommunicationClient.cs"
$migrationMap = Read-RequiredFile `
    "contracts\cluster\v1\communication-callback-migration-map.json"
$interfaceMapTest = Read-RequiredFile `
    "scripts\verify-master-callback-mirror-interface-map.ps1"
$windowsWorkflow = Read-RequiredFile `
    ".github\workflows\build-windows.yml"

Require $options "NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_ENABLED" `
    "Remaining callback mirror retains an explicit activation flag"
Require $options "DefaultQueueCapacity = 4096" `
    "Remaining callback mirror queue stays bounded"
Require $publisher "options.CallerRole != ClusterNodeRole.Master" `
    "Callback publisher accepts only the Master role"
Require $publisher "publicationTemplate.Clone()" `
    "Callback retries clone immutable semantic templates"

Require $mirror "new BlockingCollection<MirrorItem>" `
    "Remaining callback mirror owns one bounded FIFO queue"
Require $mirror "queue.TryAdd" `
    "Legacy callback threads do not block on mirror enqueue"
Require $mirror "TryCharacterPresence" `
    "Character presence retains its typed shadow builder"
Require $mirror "TryStaticBonusRefresh" `
    "Static bonus retains its typed shadow builder"

Require $service ": CommunicationService," `
    "Communication wrapper inherits the remaining legacy implementation"
Require $service "ICommunicationService" `
    "Communication wrapper reimplements the SCS request interface"
Require $service "CALLBACK_MIRROR_ISOLATED_FAILURE" `
    "Remaining mirror failures stay isolated from SCS request results"
Forbid $service "SendMessageToCharacter" `
    "Rendered character messaging is not mirrored"

$orderedMethods = @(
    @{ Name = "KickSession"; Mirror = "TryKickSession" },
    @{ Name = "Restart"; Mirror = "TryRestart" },
    @{ Name = "RunGlobalEvent"; Mirror = "TryGlobalEvent" },
    @{ Name = "Shutdown"; Mirror = "TryShutdown" },
    @{ Name = "UpdateBazaar"; Mirror = "TryBazaarRefresh" },
    @{ Name = "UpdateFamily"; Mirror = "TryFamilyRefresh" },
    @{ Name = "UpdateRelation"; Mirror = "TryRelationRefresh" }
)
foreach ($entry in $orderedMethods) {
    $methodName = [regex]::Escape($entry.Name)
    $mirrorName = [regex]::Escape($entry.Mirror)
    Require-Match $service `
        ("public\s+new\s+[^\{]+\b" + $methodName +
         "\s*\(.*?base\." + $methodName +
         "\s*\(.*?" + $mirrorName + "\s*\(") `
        ("SCS still executes before the typed shadow mirror for " + $entry.Name)
}

Require-Match $service `
    'public\s+new\s+void\s+RefreshPenalty\s*\(\s*int\s+penaltyId\s*\).*?IsCurrentClientAuthenticated\(\).*?MasterPenaltyRefreshGrpcAuthority\.Instance\.Publish\(penaltyId\)' `
    "PenaltyRefresh request dispatches only to the gRPC authority"
Forbid $service "base.RefreshPenalty(penaltyId)" `
    "PenaltyRefresh never invokes the legacy SCS callback fanout"
Forbid $service "TryPenaltyRefresh(penaltyId)" `
    "PenaltyRefresh no longer uses the asynchronous shadow mirror"
Require $service "class MasterPenaltyRefreshGrpcAuthority" `
    "Master owns a dedicated PenaltyRefresh authority publisher"
Require $service 'EventId = eventId' `
    "PenaltyRefresh retries preserve one idempotent EventId"
Require $service "CommunicationCallbackTargetKind.AllNodes" `
    "PenaltyRefresh retains ALL_NODES routing"
Require $service "response.AcceptedSequence > 0" `
    "PenaltyRefresh requires a positive accepted runtime sequence"
Require $service "no SCS callback was attempted" `
    "PenaltyRefresh publication failure is explicitly fail-closed"
Require $service "new GrpcCommunicationCallbackPublisher(options)" `
    "PenaltyRefresh authority uses the Master mTLS gRPC publisher"

Forbid $clientInterface "void UpdatePenaltyLog(int penaltyLogId);" `
    "PenaltyRefresh is absent from the SCS callback interface"
Require $clientInterface "PenaltyRefresh is gRPC-authoritative and has no SCS fallback" `
    "Any dead legacy base call fails closed instead of reviving SCS"

Require $migrationMap '"schemaVersion": 2' `
    "Callback migration map records the completed authority slice"
Require $migrationMap '"legacyMethod": "UpdatePenaltyLog"' `
    "Migration history retains the retired SCS method name"
Require $migrationMap '"disposition": "grpc_authoritative"' `
    "Migration history marks PenaltyRefresh gRPC authoritative"
Require $migrationMap '"legacySurfaceRemoved": true' `
    "Migration history records physical SCS surface removal"
Require $migrationMap '"fallback": null' `
    "Migration history records no PenaltyRefresh fallback"

Require-Match $program `
    'AddService<ICommunicationService, MirroredCommunicationService>\(new MirroredCommunicationService\(\)\)' `
    "Master exposes the wrapper whose PenaltyRefresh path is gRPC authoritative"
Require $project '<Compile Include="MirroredCommunicationService.cs" />' `
    "Classic Master project compiles the PenaltyRefresh authority"
Require $interfaceMapTest "GetInterfaceMap" `
    "Compiled verification checks CLR interface dispatch"
Require $windowsWorkflow "Verify Master callback mirror interface dispatch" `
    "Windows CI keeps the compiled interface-dispatch check"

Write-Host `
    "NosGM callback publication contracts passed: PenaltyRefresh is gRPC authoritative; remaining callbacks keep SCS-first shadow mirrors." `
    -ForegroundColor Green
