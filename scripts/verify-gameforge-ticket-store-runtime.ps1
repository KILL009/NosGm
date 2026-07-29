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
    [byte]$CountryId,
    [int]$ProposedSessionId) {
    $arguments = [object[]]@($Token, $InstallationId, $CountryId, $ProposedSessionId, $null)
    $success = [bool]$tryConsume.Invoke($instance, $arguments)
    $consumption = $arguments[4]
    return [pscustomobject]@{
        Success = $success
        AccountName = if ($null -eq $consumption) { $null } else { [string]$consumption.AccountName }
        ConsumptionNumber = if ($null -eq $consumption) { 0 } else { [int]$consumption.ConsumptionNumber }
        SessionId = if ($null -eq $consumption) { 0 } else { [int]$consumption.SessionId }
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

Assert-False (Consume-Ticket $token $installationId 5 0).Success `
    "A non-positive proposed SessionId must be rejected without consuming the ticket."

$first = Consume-Ticket $token $installationId 5 101
Assert-True $first.Success "The first language-list authentication should activate the session ticket."
Assert-Equal "test1" $first.AccountName "The first ticket entry resolved the wrong account."
Assert-Equal 1 $first.ConsumptionNumber "The first ticket entry has the wrong sequence number."
Assert-Equal 101 $first.SessionId "The first ticket entry must bind the proposed SessionId."

$second = Consume-Ticket $token $installationId 5 202
Assert-True $second.Success "The regional selection authentication should consume the second ticket entry."
Assert-Equal "test1" $second.AccountName "The second ticket entry resolved the wrong account."
Assert-Equal 2 $second.ConsumptionNumber "The second ticket entry has the wrong sequence number."
Assert-Equal $first.SessionId $second.SessionId "The second ticket entry must reuse the first SessionId."

$third = Consume-Ticket $token $installationId 5 303
Assert-True $third.Success "The channel selection authentication should consume the third ticket entry."
Assert-Equal "test1" $third.AccountName "The third ticket entry resolved the wrong account."
Assert-Equal 3 $third.ConsumptionNumber "The third ticket entry has the wrong sequence number."
Assert-Equal $first.SessionId $third.SessionId "The third ticket entry must reuse the first SessionId."

$fourth = Consume-Ticket $token $installationId 5 404
Assert-True $fourth.Success "A fourth character-selection entry must remain valid while the active session lease exists."
Assert-Equal 4 $fourth.ConsumptionNumber "The fourth ticket entry has the wrong sequence number."
Assert-Equal $first.SessionId $fourth.SessionId "The fourth ticket entry must reuse the first SessionId."

$fifth = Consume-Ticket $token $installationId 5 505
Assert-True $fifth.Success "Repeated character reselection must remain valid beyond the old three-entry cap."
Assert-Equal 5 $fifth.ConsumptionNumber "The fifth ticket entry has the wrong sequence number."
Assert-Equal $first.SessionId $fifth.SessionId "The fifth ticket entry must reuse the first SessionId."
Assert-Equal 1 ([int]$countProperty.GetValue($instance, $null)) "An active session ticket must remain registered."
Write-Host "[PASS] Modern Login ticket remains renewable for repeated character-selection entries."

Clear-Tickets
$activationToken = [Guid]::NewGuid().ToString("D")
$activationInstallationId = [Guid]::NewGuid()
Assert-True (Issue-Ticket "test1" $activationToken $activationInstallationId 5 ([TimeSpan]::FromMilliseconds(25))) `
    "The activation fixture ticket should be issued."
$activationFirst = Consume-Ticket $activationToken $activationInstallationId 5 606
Assert-True $activationFirst.Success "The short authorization ticket should activate before expiry."
Start-Sleep -Milliseconds 100
$activationSecond = Consume-Ticket $activationToken $activationInstallationId 5 607
Assert-True $activationSecond.Success "The first valid entry must convert the short ticket into the bounded active-session lease."
Assert-Equal $activationFirst.SessionId $activationSecond.SessionId "The activated lease must retain its SessionId."
Write-Host "[PASS] First consumption converts the short authorization ticket into an active-session lease."

Clear-Tickets
$mismatchToken = [Guid]::NewGuid().ToString("D")
$mismatchInstallationId = [Guid]::NewGuid()
Assert-True (Issue-Ticket "test1" $mismatchToken $mismatchInstallationId 5 ([TimeSpan]::FromMinutes(2))) `
    "The mismatch fixture ticket should be issued."
Assert-False (Consume-Ticket $mismatchToken ([Guid]::NewGuid()) 5 707).Success `
    "A mismatched InstallationId must be rejected."
Assert-False (Consume-Ticket $mismatchToken $mismatchInstallationId 5 708).Success `
    "A mismatched attempt must invalidate the ticket."
Write-Host "[PASS] InstallationId mismatch invalidates the ticket."

Clear-Tickets
$regionToken = [Guid]::NewGuid().ToString("D")
$regionInstallationId = [Guid]::NewGuid()
Assert-True (Issue-Ticket "test1" $regionToken $regionInstallationId 5 ([TimeSpan]::FromMinutes(2))) `
    "The region mismatch fixture ticket should be issued."
Assert-False (Consume-Ticket $regionToken $regionInstallationId 4 808).Success `
    "A mismatched region must be rejected."
Assert-False (Consume-Ticket $regionToken $regionInstallationId 5 809).Success `
    "A region mismatch must invalidate the ticket."
Write-Host "[PASS] Region mismatch invalidates the ticket."

Clear-Tickets
$expiredToken = [Guid]::NewGuid().ToString("D")
$expiredInstallationId = [Guid]::NewGuid()
Assert-True (Issue-Ticket "test1" $expiredToken $expiredInstallationId 5 ([TimeSpan]::FromMilliseconds(25))) `
    "The expiry fixture ticket should be issued."
Start-Sleep -Milliseconds 100
Assert-False (Consume-Ticket $expiredToken $expiredInstallationId 5 909).Success `
    "An unused expired authorization ticket must be rejected."
Write-Host "[PASS] Expired unused modern Login ticket is rejected."

Clear-Tickets
Write-Host "Gameforge modern Login tickets bind one stable SessionId, become bounded active-session leases after first use and preserve TTL, InstallationId and region binding."
