param(
    [string]$AssemblyPath = "Data/NosGm.Master.Library/bin/Release/NosGm.Master.Library.dll"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $AssemblyPath)) {
    throw "Built NosGm.Master.Library assembly not found: $AssemblyPath"
}

$resolvedAssembly = (Resolve-Path -LiteralPath $AssemblyPath).Path
$assemblyDirectory = Split-Path -Parent $resolvedAssembly
$resolver = [ResolveEventHandler]{
    param($sender, $eventArgs)

    $assemblyName = New-Object Reflection.AssemblyName($eventArgs.Name)
    $candidate = Join-Path $assemblyDirectory ($assemblyName.Name + ".dll")
    if (Test-Path -LiteralPath $candidate) {
        return [Reflection.Assembly]::LoadFrom($candidate)
    }

    return $null
}

[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
try {
    [Reflection.Assembly]::LoadFrom($resolvedAssembly) | Out-Null

    $store = [NosGm.Master.Library.Interface.GameforgeAuthTicketStore]::Instance
    $verticalTab = [char]0x0B
    $token = "37633936363633662D633332352D346461612D383933612D373031346639653063646463"
    $installationId = [Guid]::Parse("CECAE467-B008-4CA5-BCC4-6C793467D0F6")
    $md5 = "8295D53DC9146B5B8EF686262C6FA8A6"

    function Assert-ParsedPacket {
        param(
            [string]$Header
        )

        $raw = "$Header $token  $($installationId.ToString('D')) 0023A85D 5${verticalTab}0.9.3.3256 0 $md5"
        $payload = $null
        $errorCode = $null
        if (-not [NosGm.Master.Library.Interface.GameforgeLoginPacketParser]::TryParse(
                $raw,
                [ref]$payload,
                [ref]$errorCode)) {
            throw "$Header fixture was rejected: $errorCode"
        }

        if ($payload.Header -ne $Header -or
            $payload.AuthToken -ne $token -or
            $payload.InstallationId -ne $installationId -or
            $payload.RandomHex -ne "0023A85D" -or
            $payload.CountryId -ne 5 -or
            $payload.ClientVersion.ToString() -ne "0.9.3.3256" -or
            $payload.UnknownConstant -ne 0 -or
            $payload.ClientMd5 -ne $md5) {
            throw "$Header fixture did not preserve all protocol fields."
        }
    }

    function Assert-RejectedPacket {
        param(
            [string]$RawPacket,
            [string]$Description
        )

        $payload = $null
        $errorCode = $null
        if ([NosGm.Master.Library.Interface.GameforgeLoginPacketParser]::TryParse(
                $RawPacket,
                [ref]$payload,
                [ref]$errorCode)) {
            throw "Invalid fixture was accepted: $Description"
        }

        if ([string]::IsNullOrWhiteSpace($errorCode)) {
            throw "Rejected fixture did not provide a bounded error code: $Description"
        }
    }

    Assert-ParsedPacket "NoS0576"
    Assert-ParsedPacket "NoS0577"

    $validTail = "$($installationId.ToString('D')) 0023A85D 5${verticalTab}0.9.3.3256 0 $md5"
    Assert-RejectedPacket "NoS0576 $token $validTail" "single space before InstallationId"
    Assert-RejectedPacket "NoS0576 $token  $($installationId.ToString('D')) 0023A85D 5 0.9.3.3256 0 $md5" "ordinary space between CountryId and version"
    Assert-RejectedPacket "NoS0576 $token  $($installationId.ToString('D')) 0023A85D 9${verticalTab}0.9.3.3256 0 $md5" "unsupported CountryId"
    Assert-RejectedPacket "NoS0576 $token  $($installationId.ToString('D')) 0023A85D 5${verticalTab}0.9.3.3256 1 $md5" "non-zero constant"
    Assert-RejectedPacket "NoS0576 $token  $($installationId.ToString('D')) 0023A85D 5${verticalTab}0.9.3.3256 0 $($md5.Substring(1))" "short client MD5"
    Assert-RejectedPacket "NoS0575 $token  $validTail" "unsupported packet header"

    $expectedCultures = @{
        0 = "en"
        1 = "de"
        2 = "fr"
        3 = "it"
        4 = "pl"
        5 = "es"
        6 = "cs"
        7 = "ru"
        8 = "tr"
    }

    foreach ($entry in $expectedCultures.GetEnumerator()) {
        $culture = $null
        if (-not [NosGm.Master.Library.Interface.GameforgeLoginPacketParser]::TryGetCulture(
                [byte]$entry.Key,
                [ref]$culture) -or
            $culture -ne $entry.Value) {
            throw "CountryId $($entry.Key) did not map to $($entry.Value)."
        }
    }

    $unsupportedCulture = $null
    if ([NosGm.Master.Library.Interface.GameforgeLoginPacketParser]::TryGetCulture(
            [byte]9,
            [ref]$unsupportedCulture)) {
        throw "CountryId 9 was incorrectly accepted."
    }

    $store.Clear()
    if (-not $store.TryIssue("test_account", $token, $installationId, [byte]5, [TimeSpan]::FromMinutes(2))) {
        throw "A valid one-time ticket could not be issued."
    }

    $accountName = $null
    if (-not $store.TryConsume($token, $installationId, [byte]5, [ref]$accountName) -or
        $accountName -ne "test_account") {
        throw "A valid one-time ticket could not be consumed."
    }

    $replayedAccount = $null
    if ($store.TryConsume($token, $installationId, [byte]5, [ref]$replayedAccount)) {
        throw "A consumed ticket was accepted a second time."
    }

    if (-not $store.TryIssue("test_account", $token, $installationId, [byte]5, [TimeSpan]::FromMinutes(2))) {
        throw "A consumed token could not be safely issued as a new independent ticket."
    }

    $wrongInstallationAccount = $null
    if ($store.TryConsume($token, [Guid]::NewGuid(), [byte]5, [ref]$wrongInstallationAccount)) {
        throw "A ticket accepted the wrong InstallationId."
    }

    $afterMismatchAccount = $null
    if ($store.TryConsume($token, $installationId, [byte]5, [ref]$afterMismatchAccount)) {
        throw "An InstallationId mismatch did not consume the rejected ticket."
    }

    $countryToken = [Guid]::NewGuid().ToString("D")
    if (-not $store.TryIssue("test_account", $countryToken, $installationId, [byte]5, [TimeSpan]::FromMinutes(2))) {
        throw "Country-bound ticket could not be issued."
    }

    $wrongCountryAccount = $null
    if ($store.TryConsume($countryToken, $installationId, [byte]2, [ref]$wrongCountryAccount)) {
        throw "A ticket accepted the wrong CountryId."
    }

    $expiredToken = [Guid]::NewGuid().ToString("D")
    if (-not $store.TryIssue("test_account", $expiredToken, $installationId, [byte]5, [TimeSpan]::FromMilliseconds(1))) {
        throw "Short-lived expiry fixture could not be issued."
    }
    Start-Sleep -Milliseconds 25
    $expiredAccount = $null
    if ($store.TryConsume($expiredToken, $installationId, [byte]5, [ref]$expiredAccount)) {
        throw "An expired ticket was accepted."
    }

    $store.Clear()

    $handlerPath = "Data/NosGm.Handler/PacketHandler/Login/LoginPacketHandler.cs"
    $ticketStorePath = "Data/NosGm.Master.Library/Security/GameforgeAuthTicketStore.cs"
    $configurationPath = "Data/NosGm.Configuration/ServerConfiguration.cs"
    $handlerSource = Get-Content -LiteralPath $handlerPath -Raw
    $ticketStoreSource = Get-Content -LiteralPath $ticketStorePath -Raw
    $configurationSource = Get-Content -LiteralPath $configurationPath -Raw

    if ($handlerSource -notmatch '\[Packet\("NoS0576",\s*"NoS0577"\)\]' -or
        $handlerSource -notmatch 'ConsumeGameforgeAuthTicket' -or
        $handlerSource -notmatch 'GameforgeLoginPacketParser\.TryParse' -or
        $handlerSource -notmatch 'public void VerifyGameforgeLogin\(string rawPacket\)') {
        throw "The Login handler is not wired safely to both modern headers and the one-time ticket service."
    }

    if ($handlerSource -match '\{payload\.AuthToken\}' -or
        $handlerSource -match '\{rawPacket\}') {
        throw "The Login handler appears to interpolate sensitive Gameforge packet data into a log."
    }

    if ($ticketStoreSource -notmatch 'SHA256\.Create\(\)' -or
        $ticketStoreSource -match 'private sealed class Ticket\s*\{[^\}]*AuthToken') {
        throw "The ticket store no longer guarantees hashed token lookup keys."
    }

    if ($configurationSource -notmatch 'EnableGameforgeTokenLogin\s*=\s*false') {
        throw "Gameforge token login must remain disabled by default until the Auth Bridge is configured."
    }

    Write-Host "NoS0576/NoS0577 parsing, country mapping and one-time ticket behavior verified."
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
