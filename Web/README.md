# NosGM Web

A standalone, package-free ASP.NET Core 10 public portal for NosGM.
It is deliberately outside `NosGm.sln`, does not reference the legacy game DAL and never receives a game database connection string.

## Included

- original responsive interface with no commercial theme assets;
- multilingual weekly technical progress sourced from confirmed `main` changes;
- Spanish, English, German, French, Italian, Polish, Czech, Russian, Japanese and Simplified Chinese;
- public home, news, service status, rankings and launcher download pages;
- versioned public JSON endpoints under `/api/v1/public`;
- real status, multilingual news and rankings supplied through a signed, sanitized snapshot;
- legacy `/api/public` compatibility routes with deprecation headers;
- HTTPS redirection, HSTS, strict security headers, antiforgery services and API rate limiting;
- liveness and snapshot-aware readiness checks;
- no login, registration, store, payments, GM panel or administrator panel;
- no production endpoint, credential, private key, database connection string or proprietary client file.

## Run locally

```powershell
dotnet restore Web/NosGM.Web.sln
dotnet run --project Web/src/NosGM.Web/NosGM.Web.csproj
```

The development profile listens on localhost only. Production deployment must terminate HTTPS,
provide a trusted reverse-proxy configuration explicitly, and keep all secrets outside Git.

Without a valid signed snapshot, the portal starts safely but reports public services as unavailable and `/health/ready` fails. See `Web/docs/PUBLIC_API_V1.md` for publisher, signing-key, news and deployment configuration.

## Public API v1

- `GET /api/v1/public/metadata`
- `GET /api/v1/public/news?lang=es&limit=5`
- `GET /api/v1/public/status`
- `GET /api/v1/public/rankings/combat?limit=20`
- `GET /api/v1/public/rankings/reputation?limit=20`
- `GET /api/v1/public/rankings/hero?limit=20`
- `GET /health/live`
- `GET /health/ready`

## Security boundary

The trusted World process reads the game database and writes only approved public fields into `public-snapshot.json`. It signs the exact payload with HMAC-SHA256. The portal validates the schema, key ID, signature, size, freshness and every exposed field before serving it.

The snapshot directory must not be placed under `wwwroot`. SQL Server, Login, Master, World and channel ports remain private. The Internet-facing portal receives neither database credentials nor a direct database route.
