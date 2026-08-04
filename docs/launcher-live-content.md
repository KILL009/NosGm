# NosGM Launcher live content

The launcher dashboard can consume the existing versioned NosGM public portal API instead of displaying hardcoded news and local TCP results only.

## Data flow

```text
World server
    -> sanitized HMAC-signed public snapshot
NosGM Web portal
    -> bounded HTTPS or loopback JSON API
NosGM Launcher
    -> live dashboard plus validated offline cache
```

The launcher never connects to SQL Server, Master, Login or World to retrieve public news or population. It receives only the intentionally public models already exposed by the portal.

## Endpoints

The first launcher slice consumes:

```text
GET /api/v1/public/metadata
GET /api/v1/public/news?lang=<language>&limit=3
GET /api/v1/public/status
```

The portal verifies the World snapshot signature, schema, key ID, field limits and freshness before returning public information.

## Dashboard behavior

- the three placeholder news rows are replaced by localized portal news;
- hovering a news title shows its summary;
- the footer link opens the portal news page;
- Login and World health are enriched by the portal status;
- the dashboard shows total online players;
- local TCP checks remain active as a fallback;
- a refresh button updates both local and portal state;
- portal data refreshes every thirty seconds;
- changing launcher language reloads localized news;
- portal failures never block updates, login or game launch.

## Cache

A validated cache is stored at:

```text
%LOCALAPPDATA%\NosGM\Launcher\live-content-cache.json
```

The cache:

- is limited to 512 KiB;
- is replaced atomically;
- expires after seven days;
- is validated again before display;
- contains public metadata, news and service status only;
- contains no account name, password, authorization code or game session.

Cached content is labeled `datos en caché`. Fresh portal content is labeled `datos en vivo`.

## Portal URL configuration

The launcher defaults to the local development portal:

```text
http://localhost:5080/
```

Production should use a clean HTTPS base URL ending in `/`.

A process-only override is available:

```powershell
$env:NOSGM_PORTAL_BASE_URI = "https://portal.example.com/"
```

Safety rules:

- non-loopback HTTP is rejected;
- redirects are rejected;
- embedded credentials are rejected;
- fixed query strings and fragments are rejected;
- portal cookies are disabled;
- certificate revocation checking is enabled for HTTPS;
- each JSON response is limited to 256 KiB;
- unknown JSON fields fail closed.

## Local portal setup

The World publisher and Web portal already use the signed public snapshot contract described in:

```text
Web/docs/PUBLIC_API_V1.md
```

For manual local testing:

1. generate a private 32-byte HMAC key;
2. configure World with `NOSGM_PUBLIC_SNAPSHOT_DIRECTORY` and `NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64`;
3. configure Web with the same key and snapshot path;
4. start Web at `http://localhost:5080/`;
5. start the launcher normally.

A later slice will integrate this local portal startup into `start-modern-login-local.ps1`, so the normal local-stack command can launch the complete live dashboard automatically without writing the HMAC key to disk.

## Acceptance test

1. Start the configured portal and local game stack.
2. Open the launcher.
3. Confirm the news panel no longer displays the three original placeholders.
4. Confirm online player count and live-data timestamp appear.
5. Stop the portal and refresh.
6. Confirm the launcher remains usable and labels the portal unavailable.
7. Restart the launcher while the portal is offline.
8. Confirm the last validated cache appears as cached data.
9. Change language and restore the portal.
10. Confirm localized news reloads without restarting the launcher.
