# NosGM launcher authentication

NosGM can launch an authorized NosTale client through the modern `NoS0576` / `NoS0577` flow without placing the account password in the game packet.

For the one-command Windows development environment, use [`modern-login-local-runbook.md`](modern-login-local-runbook.md).

## Trust boundaries

1. the launcher sends the NosGM account name and password to the versioned authentication endpoint;
2. Master verifies the account and returns a short-lived, one-use authorization code;
3. the launcher gives that code to the client through the current-user `GameforgeClientJSONRPC` named pipe;
4. Login consumes the ticket and creates a separate one-use World permit.

The password is never stored in launcher settings, written to logs or sent to Login or World.

## Runtime flow

```text
NosGM Launcher
    |
    | HTTPS POST /api/v1/launcher/ticket
    | accountName + password + InstallationId + countryId
    v
Master LauncherAuthBridge
    |
    | verifies credentials, maintenance and bans
    | stores one-use ticket by SHA-256 lookup key
    v
GameforgeClientJSONRPC pipe
    |
    | queryAuthorizationCode + queryGameAccountName
    v
NostaleClientX.exe gf <countryId>
    |
    | NoS0576 / NoS0577
    v
Login -> Master -> one-use World permit -> World
```

Tickets and World permits expire quickly, are consumed atomically and cannot be replayed successfully.

## Shared InstallationId

The client and launcher use the same per-user registry value:

```text
HKCU\Software\Gameforge4d\TNTClient\MainApp\InstallationId
```

If the value is absent, the launcher creates one GUID before starting the client. It is not copied into `settings.json`.

## Runtime configuration

Modern Login remains disabled by default. Enable it through process environment variables instead of editing or committing secrets:

```powershell
$env:NOSGM_MASTER_AUTH_KEY = "<master-secret-at-least-32-characters>"
$env:NOSGM_AUTH_SERVICE_KEY = "<world-secret-at-least-32-characters>"
$env:NOSGM_GAMEFORGE_TICKET_ISSUER_KEY = "<issuer-secret-at-least-32-characters>"
$env:NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY = "<consumer-secret-at-least-32-characters>"
$env:NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN = "true"
$env:NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE = "true"
$env:NOSGM_START_ALL_REGIONAL_LOGIN_PORTS = "true"
$env:NOSGM_LAUNCHER_AUTH_BRIDGE_PREFIX = "http://127.0.0.1:8081/"
$env:NOSGM_AUTH_ENDPOINT = "http://127.0.0.1:8081/api/v1/launcher/ticket"
```

All four service secrets must be different. Invalid or short values stop initialization rather than falling back to committed defaults.

Supported optional bounds:

```text
NOSGM_GAMEFORGE_AUTH_TICKET_TTL_SECONDS          15..600
NOSGM_GAMEFORGE_WORLD_PERMIT_TTL_SECONDS         15..600
NOSGM_LAUNCHER_AUTH_ATTEMPT_WINDOW_SECONDS       10..600
NOSGM_LAUNCHER_AUTH_MAX_ATTEMPTS_PER_WINDOW      1..100
```

`NOSGM_AUTH_ENDPOINT` has process-level precedence over an existing launcher `settings.json`, but the runtime value is never persisted when launcher preferences are saved.

## Local development

The normal path is:

```powershell
./scripts/start-modern-login-local.ps1 -ConfigureUrlAcl
```

After the first URL reservation, later starts can use:

```powershell
./scripts/start-modern-login-local.ps1
```

Stop the processes safely with:

```powershell
./scripts/stop-modern-login-local.ps1
```

The startup script generates independent secrets in memory, launches the services in dependency order and waits for Master `4545`, AuthBridge `8081`, World `1337` and Spanish Login `4005`.

Do not run the launcher or game as administrator. The named pipe is restricted to the current Windows user and validates `_TNT_SESSION_ID` for every supported method.

## Production TLS

Do not expose the loopback HTTP listener directly. Terminate TLS in a maintained reverse proxy and forward only the exact ticket route.

```nginx
location = /api/v1/launcher/ticket {
    client_max_body_size 8k;
    proxy_pass http://127.0.0.1:8081;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-Proto https;
    proxy_request_buffering on;
}
```

The public launcher endpoint then uses a URL such as:

```text
https://auth.example.org/api/v1/launcher/ticket
```

The launcher rejects remote plain HTTP, redirects, user information, fragments and paths other than `/api/v1/launcher/ticket`. Certificate revocation checking is enabled.

## Language and region mapping

| Launcher language | CountryId | Login port |
| --- | ---: | ---: |
| `en` | 0 | 4000 |
| `de` | 1 | 4001 |
| `fr` | 2 | 4002 |
| `it` | 3 | 4003 |
| `pl` | 4 | 4004 |
| `es` | 5 | 4005 |
| `cz` | 6 | 4006 |
| `ru` | 7 | 4007 |
| `jp` | 8 | 4008 |
| `cn` | 9 | 4009 |

Login rejects a `countryId` that does not match the trusted port accepting the connection.

## Verification

```powershell
./scripts/verify-modern-login-runtime-activation.ps1
./scripts/verify-launcher-auth-bridge.ps1
./scripts/verify-repaired-login.ps1
./scripts/verify-launcher.ps1
```

The final real-client test must confirm Spanish region `5`, Login port `4005`, server-list delivery and one-use World permit consumption.
