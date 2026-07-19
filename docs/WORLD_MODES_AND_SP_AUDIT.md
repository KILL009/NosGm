# NosGM world modes, zero-EXP channel and specialist audit

## What this change adds

Each World Server process can now have an independent gameplay policy:

- a PvE world which blocks open-world player-versus-player damage;
- a PvP world which enables PvP on base maps except configured safe maps;
- a channel with zero normal combat EXP and zero hero EXP;
- optional instanced PvP inside a PvE world;
- a startup audit for specialist cards, specialist skills, BCard handlers and the five +20 elemental buffs.

Job EXP and specialist EXP are intentionally not disabled by the zero-EXP flags. This keeps SP progression available. They can be given separate policy flags later if required.

## Environment variables

| Variable | Default | Purpose |
| --- | --- | --- |
| `NOSGM_SERVER_GROUP` | Existing server group | Separates the worlds shown in the login list |
| `NOSGM_SERVER_NAME` | Existing server name | Reserved per-process display name |
| `NOSGM_WORLD_PORT` | Existing world port | Listening port for the World Server process |
| `NOSGM_WORLD_MODE` | `STANDARD` | Accepts `STANDARD`, `PVE` or `PVP`; STANDARD preserves existing behavior |
| `NOSGM_DISABLE_NORMAL_EXP` | `false` | Disables normal combat/quest EXP |
| `NOSGM_DISABLE_HERO_EXP` | `false` | Disables hero combat/quest EXP |
| `NOSGM_PVE_ALLOW_INSTANCED_PVP` | `true` | Keeps Arena, Ice Breaker, Rainbow Battle and other dedicated PvP instances enabled |
| `NOSGM_PVP_SAFE_MAP_IDS` | `1,145` | Comma-separated safe map IDs in the PvP world |

Boolean values accept `true`, `1`, `yes` or `on`.

## Start a zero-EXP PvE channel

Open a dedicated PowerShell window:

```powershell
$env:NOSGM_SERVER_GROUP = "NosGM-PvE"
$env:NOSGM_WORLD_MODE = "PVE"
$env:NOSGM_WORLD_PORT = "1337"
$env:NOSGM_DISABLE_NORMAL_EXP = "true"
$env:NOSGM_DISABLE_HERO_EXP = "true"
$env:NOSGM_PVE_ALLOW_INSTANCED_PVP = "true"

.\Frostvein.World.exe --port 1337
```

The process grants no normal or hero EXP, even when a map has its own positive EXP rate. Job EXP and SP EXP still work.

## Start a separate PvP world

Open a second PowerShell window:

```powershell
$env:NOSGM_SERVER_GROUP = "NosGM-PvP"
$env:NOSGM_WORLD_MODE = "PVP"
$env:NOSGM_WORLD_PORT = "1338"
$env:NOSGM_PVP_SAFE_MAP_IDS = "1,145"

.\Frostvein.World.exe --port 1338
```

Add every town or protected map to `NOSGM_PVP_SAFE_MAP_IDS` before opening this world to players.

## Specialist audit

At startup, search the World Server log for:

- `[SP_AUDIT]`: number of loaded specialist cards, morphs, skills and BCard types;
- `Specialist morphs without skills`: a card exists but no character skills were loaded for its morph;
- `Specialist BCard types without runtime handlers`: the database/client data uses an effect the emulator currently ignores;
- `Missing +20 elemental buff cards`: one or more of cards 942–946 are absent;
- `[BCARD_HANDLER_MISSING]`: the first live attempt to execute an unsupported effect;
- `[BCARD_HANDLER_FAILED]`: a handler threw an exception instead of silently failing.

This turns the SP1–SP12 review into a reproducible data-driven check. Run the server with the current client data and SQL database, then use these messages as the repair list.

## Current official compatibility notes

The July 2026 SP12 set consists of Achilles (Swordsman), Admiral Yi (Archer), Merlin (Mage) and Thor (Martial Artist). The current official patch also caps attack-skill cooldown reset effects at 80%; the combat code now applies that cap.

The +16 to +20 upgrade path now uses fractional rolls and the official success rates (1.2%, 1.0%, 0.8%, 0.6% and 0.4%), validates all gold/material requirements before consuming anything, and consumes one Dragon Card Protection Scroll. The protection scroll converts soul destruction into an ordinary failure, matching the protected upgrade flow.

References:

- https://forum.nostale.gameforge.com/forum/thread/5672-act-10-part-2-sp12-patch-notes/
- https://forum.nostale.gameforge.com/forum/thread/352-sp-upgrade-guide/
