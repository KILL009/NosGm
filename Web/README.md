# NosGM Web

A standalone, package-free ASP.NET Core 9 public portal foundation for NosGM.
It is deliberately outside `NosGm.sln` and does not connect directly to the game database.

## Included in this phase

- original responsive interface with no commercial theme assets;
- Spanish, English, German, French, Italian, Polish, Czech, Russian, Japanese and Simplified Chinese;
- public home, news, service status, rankings and launcher download pages;
- bounded public JSON endpoints for news, status, rankings and portal metadata;
- deterministic in-memory demonstration data while production contracts are designed;
- HTTPS redirection, HSTS, strict security headers, antiforgery services and API rate limiting;
- no login, registration, store, payments, GM panel or administrator panel;
- no production endpoint, credential, private key, database connection string or proprietary client file.

## Run locally

```powershell
dotnet restore Web/NosGM.Web.sln
dotnet run --project Web/src/NosGM.Web/NosGM.Web.csproj
```

The development profile listens on localhost only. Production deployment must terminate HTTPS,
provide a trusted reverse-proxy configuration explicitly, and keep all secrets outside Git.

## Public API

- `GET /api/public/metadata`
- `GET /api/public/news?lang=es&limit=5`
- `GET /api/public/status`
- `GET /api/public/rankings/combat?limit=20`
- `GET /health/live`
- `GET /health/ready`

## Current boundary

The default data source is intentionally synthetic. Authentication, account operations, purchases,
launcher release hosting and server-side administration remain disabled until dedicated versioned
contracts, audit logging and threat-model reviews exist.
