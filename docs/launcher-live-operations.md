# Launcher live operations

This slice extends the signed public launcher dashboard with current rates, maintenance state, channel population and a countdown-ready event calendar.

## Data flow

```text
GameConfiguration + ServerConfiguration + operator calendar
    -> NosGm.GameObject public operations publisher
    -> HMAC-signed public-operations.json
    -> /api/v1/public/operations
    -> NosGM Launcher
```

The existing `/api/v1/public/status` endpoint remains the source of per-channel population. The launcher combines both public endpoints locally.

## Public fields

The operations payload contains only:

- observation timestamp;
- public rate identifiers, labels and multipliers;
- maintenance title, message and optional public window;
- public event title, category, time window, channel and level range.

It does not contain accounts, character identifiers, sessions, IP addresses, coordinates, tickets, database settings or signing keys.

## Runtime rates

World publishes the current values for:

```text
EXP
Hero EXP
Drop
Fairy EXP
Gold
Reputation
Job EXP
```

Changes made through the existing runtime rate events appear on the next operations publication cycle.

## Maintenance

`ServerConfiguration.MaintenanceMode` is authoritative for active maintenance. An optional public schedule can add a title, explanation and start/end time. The launcher gives active maintenance priority over event countdowns.

## Event calendar file

World reads the optional file:

```text
<public snapshot directory>/public-events.json
```

The local stack currently uses:

```text
artifacts/modern-login-local/public-data/public-events.json
```

Start from `Web/config/public-events.example.json`. A populated example is:

```json
{
  "maintenance": {
    "title": "Mantenimiento semanal",
    "message": "Aplicaremos mejoras y reiniciaremos los canales.",
    "startsAt": "2026-08-09T02:00:00-04:00",
    "endsAt": "2026-08-09T03:00:00-04:00"
  },
  "events": [
    {
      "id": "instant-battle-20260808-2100",
      "type": "instant-battle",
      "title": "Instant Battle",
      "category": "pve",
      "startsAt": "2026-08-08T21:00:00-04:00",
      "endsAt": "2026-08-08T21:30:00-04:00",
      "channel": 1,
      "minimumLevel": 30,
      "maximumLevel": 99,
      "details": "Reúnete antes de que cierre la entrada."
    }
  ]
}
```

Use real server times. The publisher deliberately ignores malformed entries, expired entries and events more than 30 days in the future.

## Quick local acceptance test

With the local stack running, create a countdown that starts in two minutes:

```powershell
./scripts/set-local-launcher-operations-test.ps1
```

World reads the file on its next publication cycle. Allow about 15 seconds for `Próximo: Instant Battle de prueba` to appear in the launcher.

Test maintenance priority immediately:

```powershell
./scripts/set-local-launcher-operations-test.ps1 `
    -Maintenance `
    -StartsInMinutes 0
```

Clear all temporary test data:

```powershell
./scripts/set-local-launcher-operations-test.ps1 -Clear
```

The helper writes only to the ignored local artifacts directory. It does not read environment variables, signing keys, accounts or server credentials.

## Launcher presentation

The operation card gains three compact lines:

```text
Tasas: EXP ×5 • Hero ×5 • Drop ×1 • Fairy ×10
Canales: C1 18 • C2 11
Próximo: Instant Battle • en 12m 04s
```

When an event is active, the third line changes to `En curso` and counts down to its end. Active or imminent maintenance replaces the event line with a warning.

The launcher refreshes operations every 20 seconds and updates the visible countdown every second without repeatedly calling the portal.

## Security and resilience

- The operations file uses the same 32-byte HMAC key and key id as the existing signed snapshot.
- The portal validates the signature using constant-time comparison.
- The endpoint rejects unknown JSON fields and responses larger than 256 KiB.
- Collection sizes, dates, token formats, levels, channels and multipliers are bounded.
- Discord, login, updates and game startup do not depend on the operations endpoint.
- If operations are unavailable, the launcher retains its existing TCP and live-status fallbacks.
