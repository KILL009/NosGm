param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

function Read-Source([string]$relativePath) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required source file was not found: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Assert-Contains([string]$source, [string]$expected, [string]$name) {
    if (-not $source.Contains($expected)) {
        throw "$name failed. Missing source contract: $expected"
    }

    Write-Host "[PASS] $name"
}

function Assert-NotContains([string]$source, [string]$unexpected, [string]$name) {
    if ($source.Contains($unexpected)) {
        throw "$name failed. Forbidden source contract remains: $unexpected"
    }

    Write-Host "[PASS] $name"
}

$discord = Read-Source "Launcher/src/NosGM.Launcher/DiscordRichPresenceClient.cs"
$pipe = Read-Source "Launcher/src/NosGM.Launcher/LauncherPresencePipeServer.cs"
$state = Read-Source "Launcher/src/NosGM.Launcher/LauncherPresenceState.cs"
$presenceWindow = Read-Source "Launcher/src/NosGM.Launcher/MainWindow.Presence.cs"
$launcher = Read-Source "Launcher/src/NosGM.Launcher/ModernGameLauncher.cs"
$settings = Read-Source "Launcher/src/NosGM.Launcher/LauncherSettings.cs"
$world = Read-Source "Data/NosGm.Program/NosGm.World/Properties/AssemblyInfo.cs"

Assert-Contains $discord '"discord-ipc-{index}"' `
    "Launcher probes only the local Discord desktop RPC pipes"
Assert-Contains $discord 'cmd = "SET_ACTIVITY"' `
    "Launcher publishes Discord SET_ACTIVITY commands"
Assert-Contains $discord 'client_id = _applicationId' `
    "Discord handshake uses the configured NosGM Application ID"
Assert-Contains $discord 'MaximumPayloadBytes = 64 * 1024' `
    "Discord RPC frames are bounded"
Assert-Contains $discord 'Disconnect();' `
    "Discord reconnect path clears failed pipes"
Assert-NotContains $discord 'password' `
    "Discord RPC contains no account password field"
Assert-NotContains $discord 'AuthorizationCode' `
    "Discord RPC contains no authentication ticket"

Assert-Contains $pipe 'PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly' `
    "World-to-launcher presence pipe is restricted to the current Windows user"
Assert-Contains $pipe 'SHA256.HashData' `
    "Raw account names are reduced to a SHA-256 route"
Assert-Contains $pipe 'MaximumPayloadBytes = 8 * 1024' `
    "Gameplay presence snapshots are bounded"
Assert-Contains $pipe 'SchemaVersion != 1' `
    "Launcher rejects unknown presence schema versions"

Assert-Contains $state 'DiscordShowCharacterName' `
    "Character-name sharing is controlled by privacy settings"
Assert-Contains $state 'DiscordShowMap' `
    "Map sharing is controlled by privacy settings"
Assert-Contains $state 'DiscordShowChannel' `
    "Channel sharing is controlled by privacy settings"
Assert-Contains $state 'DiscordShowParty' `
    "Party sharing is controlled by privacy settings"
Assert-Contains $state '"combat" => "Combatiendo"' `
    "Combat has a map-free privacy fallback"
Assert-Contains $state '"fishing" => "Pescando"' `
    "Fishing has a map-free privacy fallback"
Assert-Contains $state '"minigame" => "Participando en un minijuego"' `
    "Minigames have a map-free privacy fallback"
Assert-Contains $state '"trading" => "Intercambiando objetos"' `
    "Trading has a map-free privacy fallback"
Assert-Contains $state '"shopping" => "Revisando una tienda"' `
    "Shopping has a map-free privacy fallback"
Assert-Contains $state '"afk" => "Ausente por un momento"' `
    "AFK has a map-free privacy fallback"

Assert-Contains $presenceWindow 'CloseAfterLaunch = false' `
    "Launcher remains alive while it owns dynamic Rich Presence"
Assert-Contains $presenceWindow 'ModernGameLauncher.GameLaunched +=' `
    "Launcher attaches presence to the real game process"
Assert-Contains $presenceWindow 'ClearAsync' `
    "Discord activity is cleared after the game exits"
Assert-Contains $presenceWindow 'PresenceGameProcess_Exited' `
    "Game-process exit drives presence cleanup"

Assert-Contains $launcher 'ReportPresence("Iniciando sesión"' `
    "Authentication stage is represented in Discord"
Assert-Contains $launcher 'ReportPresence("Entrando al mundo"' `
    "Client and character-selection stages are represented in Discord"
Assert-Contains $launcher 'GameLaunched?.Invoke' `
    "Successful modern launch exposes the process to presence ownership"

Assert-Contains $settings 'NOSGM_DISCORD_APPLICATION_ID' `
    "Discord Application ID supports a process-only override"
Assert-Contains $settings 'DiscordRichPresenceEnabled' `
    "Rich Presence can be disabled"
Assert-Contains $settings 'normalized.All(char.IsDigit)' `
    "Discord Application IDs are validated as numeric snowflakes"

Assert-Contains $world 'NOSGM_LAUNCHER_PRESENCE_LOCAL_PIPE_ENABLED' `
    "World presence publishing has an explicit activation flag"
Assert-Contains $world 'world-local-1' `
    "The local gRPC development World enables presence automatically"
Assert-Contains $world 'session.Account.Name' `
    "World derives the private account route from the authenticated session"
Assert-Contains $world 'BuildRoute(accountName)' `
    "World never uses the raw account as the pipe name"
Assert-Contains $world '"nosgm-presence-" + route' `
    "World publishes to the same hashed pipe namespace as the launcher"
Assert-Contains $world 'SessionStartedUnixSeconds' `
    "Gameplay session timestamps reach Discord"
Assert-Contains $world 'PartyMaximum' `
    "Group capacity reaches Discord"
Assert-Contains $world 'MapInstanceType.RaidInstance' `
    "Raid activity is classified"
Assert-Contains $world 'MapInstanceType.IceBreakerInstance' `
    "Ice Breaker activity is classified"
Assert-Contains $world 'MapInstanceType.RainbowBattleInstance' `
    "Rainbow Battle activity is classified"
Assert-Contains $world 'MapInstanceType.CelestialSpire' `
    "Celestial Spire activity is classified"

Assert-Contains $world 'LauncherPresenceActionClassifier.Resolve' `
    "World resolves action-level presence after map classification"
Assert-Contains $world 'TimeSpan.FromSeconds(15)' `
    "Combat action presence expires after a short bounded window"
Assert-Contains $world 'TimeSpan.FromMinutes(5)' `
    "AFK requires a bounded inactivity threshold"
Assert-Contains $world 'if (character.IsFishing)' `
    "Fishing is driven by the authoritative character state"
Assert-Contains $world 'if (character.CurrentMinigame > 0)' `
    "Minigames are driven by the authoritative character state"
Assert-Contains $world 'if (character.ExchangeInfo != null)' `
    "Trading is driven by the authoritative exchange state"
Assert-Contains $world 'if (character.IsShopping)' `
    "Shopping is driven by the authoritative shop state"
Assert-Contains $world 'character.LastSkillUse' `
    "Recent skill use contributes to combat presence"
Assert-Contains $world 'character.LastDefence' `
    "Recent defence contributes to combat presence"
Assert-Contains $world 'character.LastMove' `
    "Movement contributes to AFK expiry"
Assert-Contains $world 'character.LastMessage' `
    "Player communication contributes to AFK expiry"
Assert-Contains $world 'return new LauncherPresenceAction(`
                fallbackActivity,' `
    "Expired actions return to the authoritative map or event state"

Assert-NotContains $world 'character.LastSkillUseNew' `
    "Action presence avoids login-time combat false positives"
Assert-NotContains $world 'character.LastMonsterAggro' `
    "Action presence avoids constructor-time aggro false positives"
Assert-NotContains $world 'public string AccountName' `
    "World snapshots do not expose account names"
Assert-NotContains $world 'public string Password' `
    "World snapshots do not expose passwords"
Assert-NotContains $world 'public string TargetName' `
    "World snapshots do not expose combat target names"
Assert-NotContains $world 'public long TargetId' `
    "World snapshots do not expose combat target identifiers"
Assert-NotContains $world 'public long MonsterId' `
    "World snapshots do not expose monster identifiers"
Assert-NotContains $world 'MapX' `
    "World snapshots do not expose precise X coordinates"
Assert-NotContains $world 'MapY' `
    "World snapshots do not expose precise Y coordinates"

$priorityMarkers = @(
    'if (character.IsFishing)',
    'if (character.CurrentMinigame > 0)',
    'if (character.ExchangeInfo != null)',
    'if (character.IsShopping)',
    'if (IsRecent(lastCombat, now, CombatActivityWindow))',
    'if (IsInactive(lastActivity, now, AfkThreshold))'
)
$previousIndex = -1
foreach ($marker in $priorityMarkers) {
    $index = $world.IndexOf($marker, [StringComparison]::Ordinal)
    if ($index -le $previousIndex) {
        throw "Action presence priority is invalid near: $marker"
    }

    $previousIndex = $index
}
Write-Host "[PASS] Action presence priority is deterministic"

$sample = "ES_EDGARDO1"
$sha = [Security.Cryptography.SHA256]::Create()
try {
    $hash = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($sample))
    $route = -join ($hash[0..11] | ForEach-Object { $_.ToString("x2") })
    if ($route.Length -ne 24 -or $route -notmatch '^[a-f0-9]{24}$') {
        throw "Presence route derivation did not produce a bounded lowercase hash."
    }
}
finally {
    $sha.Dispose()
}
Write-Host "[PASS] Presence account routes are fixed-length SHA-256 derivations"

Write-Host "NosGM Discord Rich Presence security, privacy, action expiry and lifecycle contracts passed."
