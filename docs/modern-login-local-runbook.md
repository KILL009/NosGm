# Modern Login local runbook

This runbook starts the complete NosGM local stack on Windows without writing authentication secrets into source files or the runtime state JSON.

## Architecture after the Configuration cutover

Configuration is always authoritative over gRPC. The wider Login/Gameforge authentication path may still be selected independently with `-AuthenticationTransport SCS` or `-AuthenticationTransport GRPC`.

The local process order is fixed:

1. Authentication/Configuration gRPC host
2. Master
3. World
4. Login
5. Launcher
6. Public portal when using the integrated entrypoint

Master uses a dedicated Master mTLS certificate to confirm an existing Configuration snapshot or seed the initial snapshot when the Configuration runtime is empty. World uses its own World certificate for Configuration Get/Update and is the only role allowed to subscribe to Configuration updates. There is no Configuration SCS fallback.

## Requirements

- Windows 10 or Windows 11
- .NET Framework 4.8.1
- Visual Studio Build Tools 2022 with MSBuild
- stable .NET 10 SDK
- .NET 9 compatibility SDK installed side by side for the legacy Visual Studio 2022 build bridge
- SQL Server and a working NosGM database
- an authorized NosTale client installation configured in NosGM Launcher

NuGet CLI is optional. When `nuget.exe` is available, the startup script prefers it for the legacy `packages.config` restore. Otherwise Visual Studio Build Tools restores those dependencies with MSBuild and `RestorePackagesConfig=true`.

Visual Studio 2022 uses the side-by-side .NET 9 compatibility SDK only while restoring and building the legacy server solution. During that scoped build the startup path sets `MSBuildEnableWorkloadResolver=false`, then restores the previous SDK/workload environment before building the .NET 10 components. The repository-wide `global.json` is never rewritten.

Modern Login service secrets remain process-only. `NOSGM_MASTER_AUTH_KEY`, `NOSGM_AUTH_SERVICE_KEY`, `NOSGM_GAMEFORGE_TICKET_ISSUER_KEY` and `NOSGM_GAMEFORGE_TICKET_CONSUMER_KEY` must be distinct values of at least 32 characters whenever modern Login is enabled. The local startup script generates them cryptographically and removes them from its own environment after the child processes inherit them.

Native HTTP/2 for the legacy `net481` gRPC caller requires Windows 11 or a supported Windows Server release. `AUTO` selects `GRPCWEB` on Windows 10 and `HTTP2` on Windows 11. Both modes use HTTPS, Protobuf, bounded deadlines and the same mTLS role checks.

If .NET 9 is missing:

```powershell
winget install --id Microsoft.DotNet.SDK.9 --exact --source winget
```

## Create the local certificate bundle

Do this once per certificate rotation with the same Windows account that will run NosGM:

```powershell
./scripts/new-local-authentication-certificates.ps1 -TrustRootCertificate
```

The bundle contains one server identity and four distinct client identities:

- AuthBridge
- Login
- World
- Master

PKCS#12 passwords are random and stored through Windows DPAPI. `manifest.json` contains paths and certificate fingerprints only.

The Master identity is mandatory even when `-AuthenticationTransport SCS` is selected because Configuration cold-boot seeding is always gRPC.

## Start the complete local stack

First start, when the AuthBridge URL ACL still needs to be created:

```powershell
./scripts/start-modern-login-local.ps1 -ConfigureUrlAcl
```

Later starts:

```powershell
./scripts/start-modern-login-local.ps1
```

This keeps the wider authentication transport on SCS while still starting the mandatory Authentication/Configuration gRPC host for Configuration.

To migrate the wider authentication path too:

```powershell
./scripts/start-modern-login-local.ps1 -AuthenticationTransport GRPC
```

To force gRPC-Web:

```powershell
./scripts/start-modern-login-local.ps1 `
    -AuthenticationTransport GRPC `
    -AuthenticationGrpcWireMode GRPCWEB
```

When Release binaries already exist:

```powershell
./scripts/start-modern-login-local.ps1 -SkipBuild
```

To start the server stack without the launcher:

```powershell
./scripts/start-modern-login-local.ps1 -SkipLauncher
```

## Optional Configuration-only runtime control

The Configuration runtime-control RPCs remain disabled by default. To enable the guarded Master-only control surface for a local test:

```powershell
./scripts/start-modern-login-local.ps1 -EnableConfigurationRuntimeControl
```

Read its status:

```powershell
./scripts/invoke-configuration-grpc-runtime-control.ps1 -Operation Status
```

Restart only the Configuration runtime:

```powershell
./scripts/invoke-configuration-grpc-runtime-control.ps1 -Operation Restart
```

Restart uses exact runtime-generation compare-and-swap. The tool waits for the World subscriber to attach to the replacement runtime. It does not retry automatically when that bounded wait fails.

## Expected ready checks

A normal start should include:

```text
[READY] Authentication/Configuration gRPC on 127.0.0.1:7443
[READY] Master on 127.0.0.1:4545
[READY] Launcher AuthBridge on 127.0.0.1:8081
[READY] World on 127.0.0.1:1337
[READY] Spanish Login on 127.0.0.1:4005
```

The state file under `artifacts/modern-login-local/processes.json` uses schema 2 and records:

- `ConfigurationAuthority = "gRPC"`
- `ConfigurationFallback = null`
- `ConfigurationSubscriberRole = "World"`
- the selected wider `AuthenticationTransport`
- the selected gRPC wire mode
- process IDs and non-secret runtime metadata

No certificate passwords or service secrets are written into that state file.

## Readiness inspector

Run at any time while the stack is active:

```powershell
./scripts/test-modern-login-readiness.ps1 -RequireLauncher
```

The inspector verifies the schema-2 state, exactly one AuthenticationGrpc/Master/World/Login process, the mandatory gRPC endpoint, Configuration gRPC-only authority metadata, Master/World/Login ports, AuthBridge health, launcher settings and the configured client.

Its report is written to:

```text
artifacts/modern-login-local/readiness.json
```

## Local gRPC authentication acceptance

The separate authentication acceptance remains useful when the wider authentication path is being migrated:

```powershell
./scripts/test-authentication-grpc-local.ps1
```

It exercises the real Kestrel mTLS runtime with the generated certificate bundle. This is separate from the final Configuration authority guard.

## Configuration contract verification

The canonical static guards are:

```powershell
./scripts/verify-configuration-grpc-authority-final.ps1
./scripts/verify-configuration-runtime-controller.ps1
./scripts/verify-scs-transport-contracts.ps1
```

The first proves that Configuration SCS service/callback/rollback/shadow/selector surfaces are absent, Master is seed-only, World is the sole subscriber, startup is ordered correctly and gameplay mutations publish authority before effects. The SCS inventory guard verifies only the remaining non-Configuration legacy services.

The old Configuration shadow, parity, acceptance-pulse and LiveEffects collection scripts are intentionally removed after the cutover.

## Sanitized diagnostics

When a real-client test fails while the stack is running:

```powershell
./scripts/collect-modern-login-diagnostics.ps1
```

The ZIP is written under `artifacts/modern-login-diagnostics/`. The collector uses bounded log tails and redacts credentials, packet payloads and other sensitive values.

## Stop the stack

```powershell
./scripts/stop-modern-login-local.ps1
```

The stop script uses the recorded PID allowlist and verifies process identity before terminating anything.

## First real-client test

1. Start the stack.
2. Run the readiness inspector.
3. Select Spanish in NosGM Launcher.
4. Authenticate with a valid NosGM account.
5. Confirm the Spanish Login listener on port `4005` accepts the modern login flow.
6. Confirm the server/channel list appears.
7. Enter a character and confirm World consumes the one-use permit.
8. Confirm World receives Configuration through the gRPC authority and its subscriber remains connected.
9. Test one Configuration mutation such as a family EXP/gold buff and confirm no local effect is applied if the authoritative update is unavailable.
10. Collect sanitized diagnostics if any stage fails.
11. Stop the stack.

For the complete modern Login symptom map, also see `modern-login-acceptance-test.md`.
