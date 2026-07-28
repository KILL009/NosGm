param(
    [string]$AssemblyPath = "bin/Release/Login/NosGm.Master.Library.dll"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $AssemblyPath -PathType Leaf)) {
    throw "Compiled Gameforge ticket store assembly was not found: $AssemblyPath"
}

$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath))
$storeType = $assembly.GetType(
    "NosGm.Master.Library.Interface.GameforgeAuthTicketStore",
    $true,
    $false)
$instance = $storeType.GetProperty("Instance").GetValue($null, $null)
$tryIssue = $storeType.GetMethod("TryIssue")
$tryConsume = $storeType.GetMethod("TryConsume")
$clear = $storeType.GetMethod("Clear")
$countProperty = $storeType.GetProperty("Count")

if ($null -eq $instance -or $null -eq $tryIssue -or $null -eq $tryConsume -or $null -eq $clear -or $null -eq $countProperty) {
    throw "GameforgeAuthTicketStore runtime contract is incomplete in $AssemblyPath"
}

function Clear-Tickets {
    $clear.Invoke($instance, @()) | Out-Null
}

function Issue-Ticket(
    [string]$AccountName,
    [string]$Token,
    [Guid]$InstallationId,
    [byte]$CountryId,
    [TimeSpan]$Lifetime) {
    return [bool]$tryIssue.Invoke(
        $instance,
        [object[]]@($AccountName, $Token, $InstallationId, $CountryId, $Lifetime))
}

function Consume-Ticket(
    [string]$Token,
    [Guid]$InstallationId,
    [byte]$CountryId) {
    $arguments = [object[]]@($Token, $InstallationId, $CountryId, $null)
    $success = [bool]$tryConsume.Invoke($instance, $arguments)
    return [pscustomobject]@{
        Success = $success
        AccountName = [string]$arguments[3]
    }
}

function Assert-True([bool]$Value, [string]$Message) {
    if (-not $Value) { throw $Message }
}

function Assert-False([bool]$Value, [string]$Message) {
    if ($Value) { throw $Message }
}

function Assert-Equal($Expected, $Actual, [string]$Message) {
    if ($Expected -ne $Actual) {
        throw "$Message Expected=$Expected Actual=$Actual"
    }
}

Clear-Tickets
$token = [Guid]::NewGuid().ToString("D")
$installationId = [Guid]::NewGuid()
Assert-True (Issue-Ticket "test1" $token $installationId 5 ([TimeSpan]::FromMinutes(2))) `
    "A valid modern Login ticket should be issued."

$first = Consume-Ticket $token $installationId 5
Assert-True $first.Success "The first language-list authentication should consume the first ticket stage."
Assert-Equal "test1" $first.AccountName "The first ticket stage resolved the wrong account."

$second = Consume-Ticket $token $installationId 5
Assert-True $second.Success "The regional selection authentication should consume the second ticket stage."
Assert-Equal "test1" $second.AccountName "The second ticket stage resolved the wrong account."

$third = Consume-Ticket $token $installationId 5
Assert-False $third.Success "A third authentication must be rejected."
Assert-Equal 0 ([int]$countProperty.GetValue($instance, $null)) "A fully consumed ticket must be removed."
Write-Host "[PASS] Modern Login ticket permits exactly two matching consumptions."

Clear-Tickets
$mismatchToken = [Guid]::NewGuid().ToString("D")
$mismatchInstallationId = [Guid]::NewGuid()
Assert-True (Issue-Ticket "test1" $mismatchToken $mismatchInstallationId 5 ([TimeSpan]::FromMinutes(2))) `
    "The mismatch fixture ticket should be issued."
Assert-False (Consume-Ticket $mismatchToken ([Guid]::NewGuid()) 5).Success `
    "A mismatched InstallationId must be rejected."
Assert-False (Consume-Ticket $mismatchToken $mismatchInstallationId 5).Success `
    "A mismatched attempt must invalidate the ticket."
Write-Host "[PASS] InstallationId mismatch invalidates the ticket."

Clear-Tickets
$regionToken = [Guid]::NewGuid().ToString("D")
$regionInstallationId = [Guid]::NewGuid()
Assert-True (Issue-Ticket "test1" $regionToken $regionInstallationId 5 ([TimeSpan]::FromMinutes(2))) `
    "The region mismatch fixture ticket should be issued."
Assert-False (Consume-Ticket $regionToken $regionInstallationId 4).Success `
    "A mismatched region must be rejected."
Assert-False (Consume-Ticket $regionToken $regionInstallationId 5).Success `
    "A region mismatch must invalidate the ticket."
Write-Host "[PASS] Region mismatch invalidates the ticket."

Clear-Tickets
$expiredToken = [Guid]::NewGuid().ToString("D")
$expiredInstallationId = [Guid]::NewGuid()
Assert-True (Issue-Ticket "test1" $expiredToken $expiredInstallationId 5 ([TimeSpan]::FromMilliseconds(25))) `
    "The expiry fixture ticket should be issued."
Start-Sleep -Milliseconds 100
Assert-False (Consume-Ticket $expiredToken $expiredInstallationId 5).Success `
    "An expired ticket must be rejected."
Write-Host "[PASS] Expired modern Login ticket is rejected."

Clear-Tickets
Write-Host "Gameforge modern Login tickets support the client's two-stage language flow while preserving TTL, InstallationId and region binding."