[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$repositoryRoot =
    [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Read-RepositoryFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $fullPath = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Expected file was not found: $RelativePath"
    }

    return [System.IO.File]::ReadAllText($fullPath)
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedText,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if (-not $Content.Contains($ExpectedText)) {
        throw "$Name is missing '$ExpectedText'."
    }

    Write-Host "[PASS] $Name" -ForegroundColor Green
}

function Assert-NotContains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$ForbiddenText,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($Content.Contains($ForbiddenText)) {
        throw "$Name contains forbidden text '$ForbiddenText'."
    }

    Write-Host "[PASS] $Name" -ForegroundColor Green
}

$project = Read-RepositoryFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\NosGm.Authentication.Server.csproj"
$program = Read-RepositoryFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Program.cs"
$options = Read-RepositoryFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\AuthenticationServerOptions.cs"
$service = Read-RepositoryFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\Services\GameforgeAuthenticationService.cs"
$state = Read-RepositoryFile `
    "Data\NosGm.Program\NosGm.Authentication.Server\State\GameforgeAuthenticationState.cs"
$router = Read-RepositoryFile `
    "Data\NosGm.Cluster.Contracts\Authentication\Runtime\AuthenticationTransportRouter.cs"
$mode = Read-RepositoryFile `
    "Data\NosGm.Cluster.Contracts\Authentication\Runtime\AuthenticationTransportMode.cs"

Assert-Contains $project "<TargetFramework>net10.0</TargetFramework>" `
    "Authentication runtime targets .NET 10"
Assert-Contains $program "IPAddress.Loopback" `
    "Authentication runtime binds only to loopback"
Assert-Contains $program "HttpProtocols.Http2" `
    "Authentication runtime accepts only HTTP/2"
Assert-Contains $program "ClientCertificateMode.RequireCertificate" `
    "Authentication runtime requires mTLS client certificates"
Assert-Contains $program "errors == SslPolicyErrors.None" `
    "Authentication runtime requires an OS-valid certificate chain"
Assert-Contains $program "MaxReceiveMessageSize" `
    "Authentication runtime bounds inbound gRPC messages"
Assert-Contains $program "MaxStreamsPerConnection" `
    "Authentication runtime bounds HTTP/2 concurrency"

Assert-Contains $options "NOSGM_AUTH_GRPC_AUTHBRIDGE_CERT_SHA256" `
    "AuthBridge has an explicit certificate allow-list"
Assert-Contains $options "NOSGM_AUTH_GRPC_LOGIN_CERT_SHA256" `
    "Login has an explicit certificate allow-list"
Assert-Contains $options "NOSGM_AUTH_GRPC_WORLD_CERT_SHA256" `
    "World has an explicit certificate allow-list"
Assert-Contains $options "RejectCrossRoleCertificateReuse" `
    "A certificate fingerprint cannot own multiple roles"

Assert-Contains $service "StatusCode.PermissionDenied" `
    "Unauthorized certificate roles fail closed"
Assert-Contains $service "AuthenticationRequestReplayGuard" `
    "Authentication calls use replay protection"
Assert-Contains $service "callContext.Deadline" `
    "Authentication calls require a transport deadline"
Assert-Contains $service "AuthenticationDispatchGate" `
    "Authentication calls use bounded dispatch"
Assert-Contains $service "ThrowIfCancellationRequested" `
    "Authentication cancellation flows into handlers"
Assert-Contains $service "request {RequestId}" `
    "Authentication audit logs contain only bounded correlation metadata"
Assert-NotContains $service "AuthorizationCode}" `
    "Authentication material is not interpolated into logs"

Assert-Contains $state "MaximumConsumptionsPerTicket = 3" `
    "Ticket consumption count remains compatible"
Assert-Contains $state "_permits.TryRemove" `
    "World permits remain one-use"
Assert-Contains $state "SHA256.HashData" `
    "Authorization material is indexed only by SHA-256"

Assert-Contains $mode "return AuthenticationTransportMode.Scs;" `
    "SCS remains the default rollback transport"
Assert-Contains $router "_selectedTransport" `
    "Exactly one authentication transport is selected"
Assert-NotContains $router "catch (" `
    "Stateful transport failures are never automatically retried"
Assert-NotContains $router "Task.WhenAll" `
    "Stateful operations are never mirrored"

$repositoryText =
    (Get-ChildItem -LiteralPath $repositoryRoot -File -Recurse `
        -Include *.cs,*.csproj |
        ForEach-Object { [System.IO.File]::ReadAllText($_.FullName) }) `
        -join "`n"
Assert-NotContains $repositoryText `
    "DangerousAcceptAnyServerCertificateValidator" `
    "Unsafe certificate bypass is absent"

Write-Host `
    "NosGM authentication gRPC runtime security contract passed." `
    -ForegroundColor Green
