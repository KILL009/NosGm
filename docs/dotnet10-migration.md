# NosGM .NET 10 migration

## Goal

Move the complete NosGM repository to .NET 10 LTS without changing the verified
NosTale client protocol, login behavior, game data, database contents, or GM
operations.

This is an incremental migration. The known-good .NET Framework 4.8.1 server
remains the rollback baseline until the .NET 10 server passes the same
acceptance tests.

## Non-regression contract

Every migration wave must preserve:

- NosTale client `0.9.3.3254`.
- The exact NoS0577 login and World packet layouts.
- Regional login listeners on ports `4000` through `4009`.
- Stable Gameforge authentication and Session ID propagation.
- Master, Login, World, launcher AuthBridge, and Discord GM Bridge behavior.
- Character list, channel selection, `game_start`, and in-game entry.
- SQL Server, MongoDB, UDP, plugins, and dynamic modules.
- All ten supported cultures: English, German, French, Italian, Polish,
  Spanish, Czech, Russian, Japanese, and Chinese.
- Existing SP, item, equipment, shell, map, and combat behavior.

The working in-game session is the functional baseline. A build is not accepted
as migrated if the real client can no longer reach the same state.

## Repository inventory

| Group | Projects | Current state |
| --- | ---: | --- |
| Web, launcher, launcher tests, and tools | 15 | Target or inherit .NET 10 in wave 0 |
| Foundation bridge libraries | 6 | SDK style; target both `net481` and `net10.0` through wave 2A |
| Cluster contract bridge and self-test | 2 | Versioned SCS replacement foundation in wave 2B |
| Authentication gRPC runtime and self-test | 2 | Isolated .NET 10 host with mTLS, deadlines, replay protection, and SCS rollback |
| Modern game modules | 2 | Temporarily remain on .NET 7 because they reference the legacy server graph |
| Remaining classic server and libraries | 22 | .NET Framework 4.8.1 only; migrate in dependency order |
| Total | 49 | Migration tracked by waves below |

Of the 15 wave-0 projects, 14 moved from .NET 9 to .NET 10 and the
`NosGM.SteamAuthStub` project was already on .NET 10.

## Migration waves

| Wave | Scope | Exit condition |
| --- | --- | --- |
| 0 | Web, launcher, launcher self-tests, updater, manifest builder, and tools | Restore, build, and self-tests pass on the stable .NET 10 SDK |
| 1 | Domain, Algorithm, XMLModel, and ChickenAPI.DAL leaf libraries | SDK-style projects build for both `net481` rollback and `net10.0` migration targets |
| 2A | Configuration and Data DTO libraries | SDK-style projects build for both `net481` rollback and `net10.0` migration targets |
| 2B | Cluster contracts, Packets, PathFinder, Core, and SCS transport | Typed gRPC/Protobuf adapters replace `BinaryFormatter` and .NET Remoting |
| 3 | DAL Interface, Mapper, DAO, EF6, and Extension | SQL Server CRUD and migrations pass against a test database |
| 4 | GameObject, Handler, plugins, Bazaar, and modules | Module loading, commands, inventory, combat, SP, and packet tests pass |
| 5 | Logger, Parser, ServiceManager, Master, Login, and World | Full regional login and real-client acceptance suite passes |
| 6 | Cross-platform hardening and deployment | Windows production build passes; Linux-supported services are explicitly identified and tested |

Login, Master, and World are intentionally last. Moving executable projects
before their libraries would hide compatibility problems and put the working
server at unnecessary risk.

Wave 1 deliberately keeps a dual-target bridge. The legacy solution passes
`NosGmLegacyBuild=true` and receives the `net481` assemblies, while the .NET 10
foundation workflow compiles the same sources as `net10.0`. This preserves the
known-good server while proving that the migrated leaf libraries are ready for
the modern dependency graph. `PathFinder` moved to wave 2 after the inventory
confirmed that it directly references `NosGm.Core` and is not a leaf library.

Wave 2A extends that bridge to `NosGm.Configuration` and `NosGm.Data`. Both
depend only on the already migrated Domain library. Their SDK conversions
explicitly exclude `ServerConfiguration.local-backup.cs` and
`Base/MappingBaseDTO.cs`, because the legacy project files never compiled those
two stale sources. `Packets` and `PathFinder` remain in wave 2B because they
depend on `NosGm.Core`; Core still contains the SCS serialization and dynamic
proxy blockers described below.

Wave 2B begins with a separate `NosGm.Cluster.Contracts` bridge and a .NET 10
self-test. It establishes protocol version 1, typed negotiation and health
contracts, caller/request validation, a 4 MiB message limit, bounded dispatch
policy, and a frozen inventory of all 99 legacy SCS methods. It does not route
runtime traffic or alter any Login, World, or client-facing packet. See
`docs/scs-transport-migration.md` for the vertical-slice rollout.

The authentication contract is the first service slice in wave 2B. It adds
five explicit Gameforge ticket and one-use World permit RPCs, per-operation
caller-role policy, input validation, and a machine-checked disposition for all
eight legacy authentication methods. An isolated .NET 10 runtime now hosts
those RPCs on loopback HTTP/2 with mandatory mTLS, per-role certificate
allow-lists, deadlines, replay protection, bounded dispatch, and compatibility
self-tests. Production callers still select SCS by default: the runtime is not
authoritative until AuthBridge, Login, and World adapters are all present and a
single explicit switch selects gRPC. Stateful calls are never dual-executed or
automatically retried through another transport.

## Known blockers

### SCS serialization

`NosGm.Core` and `NosGm.SCS` use `BinaryFormatter`. Its implementation was
removed from the runtime starting with .NET 9. The unsafe compatibility package
is not an acceptable production solution.

The replacement must use an allow-listed, length-bounded message envelope. The
wire contract between NosGM processes can change only behind a versioned
adapter; the client-facing NoS0577 protocol must not change.

### Dynamic service proxies

SCS creates service proxies with `RealProxy` and types from
`System.Runtime.Remoting`. .NET Remoting is not supported on modern .NET.

Wave 2 will replace these proxies with explicit typed clients over the existing
request/reply messenger or a versioned IPC/network transport. Timeouts,
cancellation, authentication, and request correlation must be explicit.

### Windows-only APIs

The server graph contains `System.Drawing`, Windows Forms, and `System.Web`
usage. The first complete server target is therefore `net10.0-windows`.
Cross-platform work starts only after behavior parity is proven on Windows.

### Data access

EF6 remains temporarily during the runtime migration. EF Core conversion is a
separate data migration because combining both changes would make regressions
harder to isolate. `System.Data.SqlClient` use also needs to be isolated before
any later move to `Microsoft.Data.SqlClient`.

### Legacy project system

The classic graph still uses old-style project files, `packages.config`,
`app.config`, assembly redirects, and explicit compile lists. Each library must
be converted to SDK style only when its direct dependencies are ready.

## SDK policy

`global.json` requests the stable .NET 10 SDK and accepts the newest installed
10.0 feature band. This allows a maintained .NET 10 servicing SDK such as
10.0.3xx without silently moving the repository to .NET 11.

Run:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\verify-dotnet10-foundation.ps1
```

Use `-InventoryOnly` to validate SDK discovery and project targets without
building.

## Acceptance order for every server wave

1. Restore and compile in Release.
2. Run static migration checks and unit/self-tests.
3. Run database tests against a disposable database.
4. Start Master, World, Login, and AuthBridge locally.
5. Run the existing modern-login and official-packet verification scripts.
6. Connect the authorized `0.9.3.3254` client through the regional listener.
7. Verify channel selection, character list, in-game entry, inventory, maps,
   combat, SP behavior, GM Bridge, and clean disconnect.
8. Preserve the prior runnable binaries until the wave is accepted.
