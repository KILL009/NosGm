# Modern Login local runbook

This runbook starts the complete NosGM modern authentication path on one Windows development machine without editing `ServerConfiguration.cs` and without saving authentication secrets.

## What the startup command does

`start-modern-login-local.ps1`:

1. optionally restores and builds the server and launcher in Release;
2. generates four independent 48-byte cryptographic secrets;
3. exposes those values only through the temporary process environment;
4. enables the Launcher AuthBridge on loopback;
5. starts Master, World, all ten regional Login listeners and the launcher;
6. waits until Master, AuthBridge, World and Spanish Login are accepting connections;
7. removes the temporary secrets from the calling PowerShell process;
8. records only process IDs, process names, start times, ports and public loopback endpoints under the ignored `artifacts` directory;
9. runs a non-destructive readiness inspection and writes a machine-readable report.

SCS is the default internal authentication transport. An explicit
`-AuthenticationTransport GRPC` additionally starts the .NET 10 authentication
runtime and scopes a different mTLS client certificate to Master/AuthBridge,
Login, and World. The default `AUTO` wire selection uses `GRPCWEB` for the
legacy `net481` callers on Windows 10 and native `HTTP2` on Windows 11 or
supported Windows Server versions. That choice is made before any stateful
authentication call and never changes as a failure fallback.

The account password is not accepted by this script. It is entered only in the launcher dialog and remains in memory for the HTTPS request.

## Requirements

- Windows 10 or Windows 11 for SCS and the explicit gRPC path
- Windows 11 or Windows Server 2019 or later only when forcing the legacy
  callers to use native gRPC/HTTP2 instead of gRPC-Web
- .NET Framework 4.8.1
- Visual Studio Build Tools 2022 with MSBuild
- .NET 10 SDK
- .NET 9 compatibility SDK installed side by side for the legacy server build
- SQL Server and a working NosGM database
- an authorized NosTale client installation configured in NosGM Launcher

NuGet CLI is optional. When `nuget.exe` is available, the startup script uses it. Otherwise, Visual Studio Build Tools 2022 restores the legacy `packages.config` dependencies through MSBuild with `RestorePackagesConfig=true`.

Visual Studio 2022 contains MSBuild 17.x, which cannot load the repository-wide
.NET 10 SDK directly. The startup script automatically finds the newest stable
.NET 9 compatibility SDK, temporarily assigns its `Sdks` directory to
`MSBuildSDKsPath` for the legacy `NosGm.sln` restore/build, then restores the
previous environment before building the .NET 10 launcher and authentication
runtime. It never rewrites `global.json`.

If .NET 9 is missing, install it side by side without removing .NET 10:

```powershell
winget install --id Microsoft.DotNet.SDK.9 --exact --source winget
```

## First local start

Open an elevated PowerShell window in the repository and run:

```powershell
./scripts/start-modern-login-local.ps1 -ConfigureUrlAcl
```

`-ConfigureUrlAcl` creates the one-time `HttpListener` URL reservation for the current Windows user and then starts the stack.

Later starts do not require elevation:

```powershell
./scripts/start-modern-login-local.ps1
```

When the Release binaries already exist, skip restoration and compilation:

```powershell
./scripts/start-modern-login-local.ps1 -SkipBuild
```

To start only the server stack while debugging the launcher separately:

```powershell
./scripts/start-modern-login-local.ps1 -SkipLauncher
```

## First local gRPC acceptance

Do this once per local certificate rotation, using the same Windows account
that will run NosGM:

```powershell
./scripts/new-local-authentication-certificates.ps1 -TrustRootCertificate
./scripts/test-authentication-grpc-local.ps1
```

The first command creates an ignored current-user-only certificate directory,
one server identity and separate AuthBridge, Login, and World identities. The
public development root is installed only because
`-TrustRootCertificate` was supplied explicitly. PKCS#12 passwords are random
and stored through Windows DPAPI, never in `manifest.json`.

The second command performs the live mTLS ticket and World-permit acceptance
against the real Kestrel runtime. It always exercises binary gRPC-Web first.
On Windows 11 and supported Windows Server versions it also exercises native
HTTP/2; Windows 10 skips that unsupported legacy-caller wire mode. It stops its
temporary runtime automatically. The complete stack start below is still
required to validate the `net481` callers inside Master, Login, and World.

Only after that command passes, start the complete stack with gRPC:

```powershell
./scripts/start-modern-login-local.ps1 -AuthenticationTransport GRPC
```

On Windows 10, `AUTO` resolves to `GRPCWEB`. On Windows 11 and supported
Windows Server versions it resolves to `HTTP2`. An operator can request a mode
explicitly:

```powershell
./scripts/start-modern-login-local.ps1 `
    -AuthenticationTransport GRPC `
    -AuthenticationGrpcWireMode GRPCWEB
```

Forcing `HTTP2` on Windows 10 fails before any process is started. gRPC-Web
still uses HTTPS, the same Protobuf messages, deadlines, bounded payloads, and
the same role-specific mTLS certificates. It never falls back to SCS after a
call begins.

To use a rotated or externally provisioned local bundle:

```powershell
./scripts/start-modern-login-local.ps1 `
    -AuthenticationTransport GRPC `
    -AuthenticationCertificateManifest "D:\NosGM-secrets\auth\manifest.json"
```

Without the explicit `GRPC` selector, the startup path remains on SCS. The
presence of a certificate bundle does not change runtime behavior.

## Expected ready checks

The script should report:

```text
[READY] Master on 127.0.0.1:4545
[READY] Launcher AuthBridge on 127.0.0.1:8081
[READY] World on 127.0.0.1:1337
[READY] Spanish Login on 127.0.0.1:4005
```

An explicit gRPC start also reports:

```text
[READY] Authentication gRPC on 127.0.0.1:7443
Authentication wire mode: GRPCWEB
```

The loopback-only health endpoint is:

```text
http://127.0.0.1:8081/api/v1/launcher/health
```

It reports only service readiness, maintenance state, feature flags, TTL values and the canonical region count. It does not expose accounts, database information, keys, IP configuration or secrets.

Select **Español** in the launcher. The launcher then starts the client with region `5`, which must connect to Login port `4005`.

## Readiness inspector

The startup command runs the readiness inspector automatically without stopping the stack when a launcher or client blocker is found.

Run it again at any time:

```powershell
./scripts/test-modern-login-readiness.ps1 -RequireLauncher
```

The machine-readable report is written to:

```text
artifacts/modern-login-local/readiness.json
```

The inspector validates:

- exactly one recorded Master, World and Login process;
- PID, process name and original start time;
- Master, World and Spanish Login TCP connectivity;
- the authentication gRPC process and loopback port when `GRPC` is selected;
- the loopback AuthBridge health response and all ten regions;
- launcher settings without credential-shaped properties;
- the configured authorized client executable and file version;
- Spanish region `5` for the first acceptance test;
- the shared current-user Gameforge `InstallationId`.

A missing `InstallationId` before the first **Play** action is a warning, because the launcher creates it before starting the client.

## Sanitized diagnostic bundle

When a real-client test fails, keep the stack running and execute:

```powershell
./scripts/collect-modern-login-diagnostics.ps1
```

The ZIP is created under:

```text
artifacts/modern-login-diagnostics/
```

The bundle contains readiness results, sanitized process metadata, component versions and bounded log tails. It redacts raw `NoS0576`, `NoS0577` and `NsTeST` packets, passwords, codes, keys, account identifiers, email addresses, GUID values, external IP addresses, Windows profile names and long secret-shaped values.

The collector never reads the environment blocks of running processes and never copies the complete launcher settings or Gameforge registry key.

See the complete symptom map and validation sequence in [`modern-login-acceptance-test.md`](modern-login-acceptance-test.md).

## Stop the local stack

Run:

```powershell
./scripts/stop-modern-login-local.ps1
```

The shutdown command reads the PID allowlist created by the startup command. Before stopping anything, it verifies both the process name and the original start time so a recycled Windows PID cannot target an unrelated process.

## Runtime environment variables

Every server process reads these values during static configuration initialization:

| Variable | Purpose |
| --- | --- |
| `NOSGM_MASTER_AUTH_KEY` | Master communication authentication |
| `NOSGM_AUTH_SERVICE_KEY` | World authentication and World permit consumption |
| `NOSGM_GAMEFORGE_TICKET_ISSUER_KEY` | Trusted ticket issuer role |
| `NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY` | Login ticket consumer role |
| `NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN` | Enables `NoS0576` and `NoS0577` authentication |
| `NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE` | Enables the Master HTTP ticket endpoint |
| `NOSGM_START_ALL_REGIONAL_LOGIN_PORTS` | Starts Login ports `4000` through `4009` |
| `NOSGM_LAUNCHER_AUTH_BRIDGE_PREFIX` | Internal `HttpListener` root |
| `NOSGM_GAMEFORGE_AUTH_TICKET_TTL_SECONDS` | Authorization ticket lifetime, 15 to 600 seconds |
| `NOSGM_GAMEFORGE_WORLD_PERMIT_TTL_SECONDS` | World permit lifetime, 15 to 600 seconds |
| `NOSGM_LAUNCHER_AUTH_ATTEMPT_WINDOW_SECONDS` | Rate-limit window, 10 to 600 seconds |
| `NOSGM_LAUNCHER_AUTH_MAX_ATTEMPTS_PER_WINDOW` | Attempts per IP and account, 1 to 100 |
| `NOSGM_AUTH_ENDPOINT` | Launcher ticket URL |

When modern Login is enabled, all four service secrets must contain at least 32 characters and must be different from one another. Invalid values stop process initialization instead of silently falling back to insecure defaults.

Plain HTTP is accepted only for a loopback listener. Remote launcher endpoints must continue to use HTTPS.

## Manual environment example

For a persistent deployment, inject secrets through the service manager or deployment platform. Do not place production values in Git, source files, launcher settings or command-line arguments.

Example process-only PowerShell configuration:

```powershell
function New-NosGmSecret {
    $bytes = New-Object byte[] 48
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    [Convert]::ToBase64String($bytes)
}

$env:NOSGM_MASTER_AUTH_KEY = New-NosGmSecret
$env:NOSGM_AUTH_SERVICE_KEY = New-NosGmSecret
$env:NOSGM_GAMEFORGE_TICKET_ISSUER_KEY = New-NosGmSecret
$env:NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY = New-NosGmSecret
$env:NOSGM_ENABLE_GAMEFORGE_TOKEN_LOGIN = "true"
$env:NOSGM_ENABLE_LAUNCHER_AUTH_BRIDGE = "true"
$env:NOSGM_START_ALL_REGIONAL_LOGIN_PORTS = "true"
$env:NOSGM_LAUNCHER_AUTH_BRIDGE_PREFIX = "http://127.0.0.1:8081/"
$env:NOSGM_AUTH_ENDPOINT = "http://127.0.0.1:8081/api/v1/launcher/ticket"
```

For production, expose only the exact ticket path through a maintained TLS reverse proxy. The Master listener and health endpoint should remain reachable only through loopback.

## First real-client test

1. Start the stack with the startup script.
2. Run `./scripts/test-modern-login-readiness.ps1 -RequireLauncher`.
3. Select Spanish in NosGM Launcher.
4. Enter a valid NosGM account and password.
5. Confirm Master logs one successful ticket issue without printing the token.
6. Confirm Login accepts `NoS0576` or `NoS0577` on port `4005`.
7. Confirm Login reports stages `1/3`, `2/3` and `3/3` with the same `SessionID`.
8. Confirm the server list appears.
9. Enter a character and confirm World consumes the one-use permit.
10. Disconnect and repeat with an invalid password to confirm the generic rejection.
11. Attempt more than ten invalid passwords in sixty seconds to confirm HTTP `429`.
12. Collect a sanitized bundle if any stage fails.
13. Stop the stack with the shutdown script.

For a channel-entry failure, inspect one `ClientId` through the bounded
`[WORLD_HANDSHAKE]` and `[WORLD_ENTRY]` records. A healthy path ends in
`GAMEFORGE_WORLD_PERMIT_ACCEPTED`, `ACCOUNT_INITIALIZED` and
`CHARACTER_LIST_SENT`. Stable `Code=` values identify the exact rejection
without writing account names, session identifiers, credentials, IP addresses
or packet contents. These records are flushed immediately to the bounded
`nosgm-world-handshake.log`; its first record identifies diagnostic revision
`20260728.4`. The diagnostic ZIP also contains `binary-summary.json`
with SHA-256 fingerprints for the executable and fixed NosGM module allowlist,
so evidence can be matched to the deployed build.

## Automated contracts

Run:

```powershell
./scripts/verify-modern-login-runtime-activation.ps1
./scripts/verify-modern-login-observability.ps1
./scripts/verify-launcher-auth-bridge.ps1
./scripts/verify-repaired-login.ps1
./scripts/verify-gameforge-stable-session.ps1
./scripts/verify-gameforge-ticket-store-runtime.ps1
./scripts/verify-authentication-grpc-local-acceptance.ps1
```

The Windows CI workflow also runs these checks after compiling the complete .NET Framework solution.
