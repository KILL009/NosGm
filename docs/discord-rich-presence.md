# NosGM Discord Rich Presence

NosGM owns its Discord activity through the launcher instead of relying on Discord's generic detection of `NostaleClientX.exe`.

```text
World Server
    -> current-user local named pipe
NosGM Launcher
    -> Discord desktop RPC
Discord Rich Presence
```

The game client is not injected, inspected or modified for this feature.

## Official Discord application

NosGM uses this public Discord Application ID:

```text
1534034979363754014
```

It is now the launcher's built-in default. Players do not need to edit an environment variable or `settings.json` for the official application. `NOSGM_DISCORD_APPLICATION_ID` remains available only as a development/test override.

Never distribute a Discord client secret, bot token or OAuth secret with the launcher. None is required by this Rich Presence integration.

## Privacy boundary

World sends only a bounded state snapshot containing:

- activity category;
- public map name;
- character name;
- normal and hero level;
- character class;
- channel;
- party size;
- gameplay-session start time;
- Discord asset keys.

It never sends:

- account name inside the snapshot;
- account or character identifiers;
- password, authorization code or World permit;
- IP address;
- exact map coordinates;
- inventory or equipment;
- private messages;
- other players' identities.

The authenticated account name is used only to derive a fixed-length SHA-256 local pipe route. The raw value is not included in the pipe name or payload. The launcher pipe uses `CurrentUserOnly`, so another Windows user cannot publish activity into the session.

## Discord Developer Portal setup

The application must be named `NosGM` and contain the assets below under Rich Presence assets.

### Initial assets

Upload lowercase asset keys:

| Key | Use |
| --- | --- |
| `nosgm` | Main NosGM logo |
| `launcher` | Launcher/startup stage |
| `class_adventurer` | Adventurer |
| `class_swordsman` | Swordsman |
| `class_archer` | Archer |
| `class_magician` | Magician |
| `class_martialartist` | Martial Artist |

If a class key is not uploaded, the main NosGM image and all text continue working; Discord simply omits the missing small image.

## Configure the local launcher

No extra Discord configuration is required for the official application. Start the local stack normally:

```powershell
.\scripts\start-modern-login-local.ps1 `
    -SkipBuild `
    -AuthenticationTransport GRPC `
    -AuthenticationGrpcWireMode GRPCWEB
```

For isolated development against another Discord application, override the built-in ID for only the current PowerShell process:

```powershell
$env:NOSGM_DISCORD_APPLICATION_ID = "ANOTHER_NUMERIC_APPLICATION_ID"
```

The value can also be stored as `DiscordApplicationId` in:

```text
%LOCALAPPDATA%\NosGM\Launcher\settings.json
```

Example preference fields:

```json
{
  "DiscordRichPresenceEnabled": true,
  "DiscordApplicationId": "1534034979363754014",
  "DiscordShowCharacterName": true,
  "DiscordShowMap": true,
  "DiscordShowChannel": true,
  "DiscordShowParty": true
}
```

Do not replace the entire settings file with this fragment. Add or edit only these properties in the existing JSON document.

## Local World activation

The local gRPC startup already identifies World as:

```text
NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID=world-local-1
```

That identity activates the local presence publisher automatically. Other deployments remain disabled unless they explicitly set:

```text
NOSGM_LAUNCHER_PRESENCE_LOCAL_PIPE_ENABLED=true
```

This first transport is intentionally local-only. A remote production World must not expose a named pipe across the network. A later production deployment requires an authenticated, encrypted presence service with separate short-lived permissions.

## Current activity mapping

| World state | Discord details |
| --- | --- |
| Base/normal map | `Explorando <mapa>` |
| TimeSpace | `Explorando una Piedra del Tiempo` |
| Raid/family raid | `Participando en una raid` |
| LOD | `Combatiendo en Tierra de la Muerte` |
| Caligor | `Luchando contra Caligor` |
| Ice Breaker | `Participando en Ice Breaker` |
| Talent Arena | `Compitiendo en Talent Arena` |
| Rainbow Battle | `Participando en Rainbow Battle` |
| Arena/PvP | `Compitiendo en la arena` |
| Glacernon | `Aventurándose por Glacernon` |
| Act 4/Act 7 ship | `Viajando entre continentes` |
| Celestial Spire | `Ascendiendo la Aguja Celestial` |
| Instant Battle map | `Participando en Instant Battle` |
| Other event instance | `Participando en un evento` |

The secondary line can include the allowed character name, level, hero level and channel. Discord's party field receives the current and maximum group size when that preference is enabled.

## Lifecycle

1. Launcher connects to Discord Desktop and displays its startup stage.
2. Authentication and client preparation update the activity.
3. After a successful game launch, the launcher owns the game process.
4. World publishes a sanitized snapshot every three seconds when meaningful state changes.
5. A fifteen-second heartbeat lets a restarted launcher recover the current state.
6. Closing the game clears the Discord activity.
7. Closing the launcher also clears activity and closes the local pipe.

Because the launcher owns Rich Presence, `CloseAfterLaunch` is disabled in memory while this feature is active. The launcher can remain minimized; it does not need to stay in the foreground.

## Acceptance test

1. Start Discord Desktop before the launcher.
2. Start the local stack normally.
3. Confirm Discord shows **NosGM**, not the generic **NosTale** detection.
4. Confirm `Launcher listo` appears.
5. Press **Jugar** and observe preparation/authentication stages.
6. Enter a character and wait up to fifteen seconds.
7. Confirm character, level, channel and map follow privacy settings.
8. Enter a raid, arena or supported event and confirm the details change.
9. Join or leave a group and confirm party size changes.
10. Close the game and confirm the activity disappears.
11. Restart Discord while the game remains open and confirm presence reconnects.

## Planned action-level expansion

The first slice describes the player's location and game mode. Subsequent slices add short-lived action states from authoritative World events:

- fighting a monster or boss;
- fishing and minigames;
- trading and private shops;
- crafting and upgrading;
- changing specialist card;
- death and resurrection;
- Instant Battle wave progress;
- raid name and progress;
- AFK status.

Action updates will be debounced and expire automatically. They will never publish every attack or packet.
