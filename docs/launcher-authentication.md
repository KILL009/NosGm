# NosGM launcher authentication

NosGM can launch the authorized NosTale client through the modern `NoS0576` / `NoS0577` flow without placing the account password in the game packet.

The implementation has three trust boundaries:

1. the launcher sends the NosGM account name and password to the versioned authentication endpoint;
2. Master verifies the account and returns a short-lived, one-use authorization code;
3. the launcher gives that code to the game client through the current-user `GameforgeClientJSONRPC` named pipe.

The password is never stored in launcher settings, never written to logs and never sent to Login or World.

## Runtime flow

```text
NosGM Launcher
    |
    | HTTPS POST /api/v1/launcher/ticket
    | accountName + password + InstallationId + countryId
    v
Master LauncherAuthBridge
    |
    | verifies password, maintenance and bans
    | stores one-use ticket as a SHA-256 lookup key
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

The authorization ticket and World permit expire quickly. Both are consumed atomically and cannot be replayed successfully.

## Shared InstallationId

The current client uses the per-user Gameforge registry value:

```text
HKCU\Software\Gameforge4d\TNTClient\MainApp\InstallationId
```

The launcher reads the same value. If the value is absent, it creates one GUID before starting the client. It is not copied into `settings.json`.

This identity must match because Master binds the one-use authorization code to the `InstallationId` later received inside `NoS0577`.

## Server configuration

Modern login and the HTTP bridge are disabled by default. Configure them deliberately in `ServerConfiguration.cs` or the deployment-specific configuration layer:

```csharp
EnableGameforgeTokenLogin = true;
EnableLauncherAuthBridge = true;
LauncherAuthBridgePrefix = "http://127.0.0.1:8081/";
GameforgeAuthTicketTtlSeconds = 120;
GameforgeWorldPermitTtlSeconds = 120;
LauncherAuthBridgeAttemptWindowSeconds = 60;
LauncherAuthBridgeMaxAttemptsPerWindow = 10;
```

The authentication service still separates issuer and consumer roles. Configure three different secrets:

```csharp
AuthServiceKey = "<world-authentication-secret>";
GameforgeTicketIssuerKey = "<ticket-issuer-secret>";
GameforgeTicketConsumerKey = "<login-consumer-secret>";
```

Each Gameforge key must contain at least 32 characters. Never commit production values. Generate them on the server and inject them through the deployment configuration.

A PowerShell example for generating one random value:

```powershell
$bytes = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

Run it separately for every secret.

## Local development

The default bridge prefix is loopback-only:

```text
http://127.0.0.1:8081/
```

The launcher endpoint must include the exact versioned path:

```powershell
$env:NOSGM_AUTH_ENDPOINT = "http://127.0.0.1:8081/api/v1/launcher/ticket"
```

`HttpListener` may require a one-time URL ACL when Master runs without administrator rights. Execute this from an elevated deployment shell, replacing the user as appropriate:

```powershell
netsh http add urlacl url=http://127.0.0.1:8081/ user="$env:USERDOMAIN\$env:USERNAME"
```

Do not run the launcher or game as administrator. The named pipe is restricted to the current Windows user.

## Production TLS

Do not expose the loopback HTTP listener directly. Terminate TLS in a maintained reverse proxy and forward only the exact ticket path to Master.

Example Nginx location:

```nginx
location = /api/v1/launcher/ticket {
    client_max_body_size 8k;
    proxy_pass http://127.0.0.1:8081;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Forwarded-Proto https;
    proxy_set_header X-Content-Type-Options nosniff;
    proxy_request_buffering on;
}
```

The public launcher configuration then uses:

```text
https://auth.example.org/api/v1/launcher/ticket
```

The launcher rejects remote plain HTTP, URLs containing user information, fragments, redirects and paths other than `/api/v1/launcher/ticket`. Certificate revocation checking is enabled.

## Language and region mapping

The language selected in the launcher controls the `gf <countryId>` argument and must match the private Login endpoint already configured in the authorized client:

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

Login rejects a `countryId` that does not match the trusted local port accepting the connection.

## Launcher behavior

When `AuthenticationEndpoint` is empty, the existing legacy launch action remains available for compatibility.

When the endpoint is configured:

1. pressing **Play** opens a localized account dialog;
2. only the account name may be remembered;
3. the launcher obtains a one-use ticket;
4. it starts the named-pipe listener before starting the client;
5. it launches `NostaleClientX.exe` with `gf <countryId>` and the two `_TNT_*` environment variables;
6. it terminates the spawned client if the handshake fails or times out.

## Verification checklist

1. Build the complete server solution in Release.
2. Build `Launcher/NosGM.Launcher.sln` in Release.
3. Start Master and confirm the bridge reports its loopback prefix.
4. Start Login and confirm the ten regional listeners.
5. Start World.
6. Set `NOSGM_AUTH_ENDPOINT` to the local or production endpoint.
7. Select Spanish and confirm the client connects through region `5` and Login port `4005`.
8. Enter a valid account password and confirm the character list appears.
9. Retry the same authorization code and confirm it is rejected.
10. Enter an invalid password repeatedly and confirm rate limiting returns HTTP `429`.
11. Confirm launcher settings contain the account name but no password or authorization code.
12. Repeat with French on `4002` and Japanese on `4008`.

Automated source contracts run through:

```powershell
./scripts/verify-launcher-auth-bridge.ps1
./scripts/verify-launcher.ps1
```
