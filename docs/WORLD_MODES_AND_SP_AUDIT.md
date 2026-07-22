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

.\NosGm.World.exe --port 1337
```

The process grants no normal or hero EXP, even when a map has its own positive EXP rate. Job EXP and SP EXP still work.

## Start a separate PvP world

Open a second PowerShell window:

```powershell
$env:NOSGM_SERVER_GROUP = "NosGM-PvP"
$env:NOSGM_WORLD_MODE = "PVP"
$env:NOSGM_WORLD_PORT = "1338"
$env:NOSGM_PVP_SAFE_MAP_IDS = "1,145"

.\NosGm.World.exe --port 1338
```

Add every town or protected map to `NOSGM_PVP_SAFE_MAP_IDS` before opening this world to players.

## Specialist audit

At startup, search the World Server log for:

- `[SP_AUDIT]`: number of loaded specialist cards, morphs, skills and BCard types;
- `Specialist morphs without skills`: a card exists but no character skills were loaded for its morph;
- `Specialist BCard types without active plugin handlers`: the type is not registered as an active handler; some older types are passive and are evaluated directly by combat code;
- `Missing +20 elemental buff cards`: one or more of cards 942–946 are absent;
- `[BCARD_HANDLER_MISSING]`: the first live attempt to execute an unsupported effect;
- `[BCARD_HANDLER_FAILED]`: a handler threw an exception instead of silently failing.

This turns the SP1–SP12 review into a reproducible data-driven check. Run the server with the current client data and SQL database, then use these messages as the repair list.

## Database audit result (20 July 2026)

The supplied SQL snapshot was inspected without executing it and with the analysis restricted to `Item`, `Skill`, `BCard` and `Card`.

- 60 player specialist-card item rows across 52 player morphs;
- 614 specialist skills and 1,672 skill BCards;
- no player specialist morph without skills;
- no specialist BCard references a missing `Card` row;
- all five +20 blessing cards (942–946) are present;
- Achilles (morph 55): 11 skills / 43 BCards;
- Admiral Yi (morph 56): 13 skills / 47 BCards;
- Merlin (morph 57): 11 skills / 44 BCards;
- Thor (morph 58): 11 skills / 39 BCards.

This confirms that the SP12 data exists, but not that every mechanic executes. The current source has no active plugin handlers for modern resource mechanics 118–125 and 130. In particular, SP12 depends on type 124 (`TokenGauge`) on its regular attacks and type 130 (`DimensionalSynchronization`) on each ultimate. SP10 and SP11 also depend on several of the earlier resource types. These are now named explicitly and reported as `[SP_MECHANIC_UNIMPLEMENTED]`; naming and detecting a type is not the same as implementing it.

A full server export also contains account and character data. Future audit exports must contain only `Item`, `Skill`, `BCard` and `Card`, with no account, character, mail or log tables.

## Current official compatibility notes

The July 2026 SP12 set consists of Achilles (Swordsman), Admiral Yi (Archer), Merlin (Mage) and Thor (Martial Artist). The current official patch also caps attack-skill cooldown reset effects at 80%; the combat code now applies that cap.

The +16 to +20 upgrade path now uses fractional rolls and the official success rates (1.2%, 1.0%, 0.8%, 0.6% and 0.4%), validates all gold/material requirements before consuming anything, and consumes one Dragon Card Protection Scroll. The protection scroll converts soul destruction into an ordinary failure, matching the protected upgrade flow.

References:

- https://forum.nostale.gameforge.com/forum/thread/5672-act-10-part-2-sp12-patch-notes/
- https://forum.nostale.gameforge.com/forum/thread/352-sp-upgrade-guide/
