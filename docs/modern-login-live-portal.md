# Modern Login local stack with live portal

The public local entrypoint now starts the NosGM portal together with Authentication, Master, World, Login and the launcher. The launcher receives news, population and service health from the signed public snapshot while keeping its local TCP probes as fallback.

## Start everything

From a Windows PowerShell terminal at the repository root:

```powershell
./scripts/start-modern-login-local.ps1 `
    -SkipBuild `
    -AuthenticationTransport GRPC `
    -AuthenticationGrpcWireMode GRPCWEB
```

The command publishes and starts the portal on `http://127.0.0.1:5080/`, unless `-PortalPort` selects another local port. Use `-SkipPortalBuild` only after the portal has already been published into `artifacts/modern-login-local/portal`.

The original server startup implementation remains isolated in `scripts/start-modern-login-core-local.ps1`. Do not execute that core script for normal testing because it intentionally omits the portal wrapper and signed-snapshot setup.

## Data path

```text
Game database
  -> trusted World process
  -> sanitized HMAC-signed snapshot
  -> local ASP.NET Core portal
  -> launcher dashboard
```

The private runtime files are created under:

```text
artifacts/modern-login-local/public-data/
```

The launcher never receives database credentials. It only calls the versioned public API:

```text
GET /api/v1/public/metadata
GET /api/v1/public/news
GET /api/v1/public/status
```

## Signing-key boundary

A fresh 32-byte key is generated for every local stack start. The wrapper supplies the same key to World through `NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64` and to the portal through `PublicData__HmacKeyBase64`.

The key exists only in the process environment inherited by those child processes. It is restored out of the parent PowerShell environment after startup, is never written to `processes.json`, is never placed in launcher settings and is never committed to the repository.

The state file records only non-secret operational details such as process identities, the portal URI and the snapshot path. The stop command validates process name and start time before terminating each recorded PID.

## Readiness

The wrapper first waits for:

```text
GET /health/live
```

After World starts publishing, it also waits for:

```text
GET /health/ready
```

A readiness warning does not invent healthy data. Inspect World logs and the snapshot directory when the portal stays degraded.

## News

On the first run, the wrapper copies `Web/config/public-news.example.json` into the private runtime directory. Edit the private copy to test launcher news without modifying the repository example.

## Stop everything

```powershell
./scripts/stop-modern-login-local.ps1
```

The portal is appended to the same validated process ledger as the server components, so the existing stop command closes the complete stack safely.

## Useful options

```powershell
-PortalPort 5080
-SkipPortalBuild
-SkipLauncher
-SkipBuild
-StartupTimeoutSeconds 60
```

For Windows 10 with gRPC, keep `-AuthenticationGrpcWireMode GRPCWEB`. HTTP/2 for the .NET Framework callers remains reserved for supported newer Windows versions.
