# NosGM Public API v1

NosGM publishes a deliberately small read model for the public website. The ASP.NET Core portal never receives a game database connection string and does not load the legacy DAL assemblies.

The trusted World process exports only approved public fields to a signed JSON snapshot. The portal verifies that signature before serving the data.

## Data flow

```text
Game database -> World server -> sanitized signed snapshot -> ASP.NET Core portal -> Internet
```

The snapshot directory must remain outside the website's `wwwroot` folder. For one-machine deployments, use a private local directory readable by the World process and the portal account. For split deployments, synchronize only `public-snapshot.json`; never expose or replicate the database itself.

## 1. Generate a signing key

Run once in PowerShell on the trusted server:

```powershell
$key = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($key)
[Convert]::ToBase64String($key)
```

Store the resulting Base64 value in the process environment or a secret manager. Do not commit it.

## 2. Configure the World server publisher

Required environment variables:

```text
NOSGM_PUBLIC_SNAPSHOT_DIRECTORY=C:\NosGM\PublicData
NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64=<same secret key>
```

Optional variables:

| Variable | Default | Purpose |
| --- | --- | --- |
| `NOSGM_PUBLIC_SNAPSHOT_KEY_ID` | `nosgm-live-v1` | Identifies the active signing key. |
| `NOSGM_PUBLIC_SNAPSHOT_INTERVAL_SECONDS` | `30` | Publication interval, clamped to 15-600 seconds. |
| `NOSGM_PUBLIC_SNAPSHOT_LEADER_CHANNEL` | `1` | Channel that assembles the final snapshot. |
| `NOSGM_PUBLIC_NEWS_FILE` | `<snapshot directory>/public-news.json` | Curated multilingual news source. |
| `NOSGM_PUBLIC_LOGIN_HOST` | `127.0.0.1` | Internal login endpoint checked by the publisher. |
| `NOSGM_PUBLIC_LOGIN_PORT` | `4000` | Internal login port checked by the publisher. |
| `NOSGM_PUBLIC_SERVER_NAME` | `NosGM` | Public server name. |
| `NOSGM_PUBLIC_EXCLUDED_CHARACTER_IDS` | empty | Comma-separated character IDs omitted from rankings. |
| `NOSGM_PUBLIC_EXCLUDED_CHARACTER_NAMES` | empty | Comma-separated character names omitted from rankings. |

Copy `Web/config/public-news.example.json` to the configured news path and edit it. Invalid entries are ignored. The publisher never exports account IDs, emails, passwords, IP addresses, inventory data, database keys or connection strings.

Each channel writes a private heartbeat. The configured leader combines those heartbeats, probes the login listener, queries sanitized ranking fields and atomically replaces `public-snapshot.json`.

## 3. Configure the web portal

Use ASP.NET Core environment variables or an external configuration provider:

```text
PublicData__SnapshotPath=C:\NosGM\PublicData\public-snapshot.json
PublicData__KeyId=nosgm-live-v1
PublicData__HmacKeyBase64=<same secret key>
PublicData__MaximumAgeSeconds=180
PublicData__MaximumSnapshotBytes=1048576
```

The key may be rotated by changing both `KeyId` and the key on the publisher and portal together. During deployment, keep the snapshot path readable only by the World publisher and the portal service account.

## Public endpoints

```text
GET /api/v1/public/metadata
GET /api/v1/public/news?lang=es&limit=5
GET /api/v1/public/status
GET /api/v1/public/rankings/combat?limit=20
GET /api/v1/public/rankings/reputation?limit=20
GET /api/v1/public/rankings/hero?limit=20
GET /health/live
GET /health/ready
```

The old `/api/public/*` routes remain temporarily available and return deprecation headers. New clients must use `/api/v1/public/*`.

## Failure behavior

The portal rejects snapshots with an unknown schema, wrong key ID, invalid HMAC, excessive size, duplicate service IDs, unsupported ranking names or unsafe field values. It keeps the last valid snapshot in memory during a short producer interruption. Missing or badly stale data is reported as degraded or offline, and `/health/ready` fails rather than inventing healthy data.

## Firewall boundary

Only the reverse proxy ports for the public portal should be reachable from the Internet. SQL Server, Login, Master, World and channel ports must stay on the private network or localhost as appropriate. The portal requires no inbound route to SQL Server and no database credentials.
