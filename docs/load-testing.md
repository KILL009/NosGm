# NosGM staged load testing

## Goal

NosGM load testing separates lower-layer capacity from real playable-character capacity so the final number is defensible:

1. **TCP capacity**: can Login/World accept and hold the intended number of concurrent sockets?
2. **Modern Login capacity**: can AuthBridge issue real short-lived tickets and can Login consume `NoS0576` / `NoS0577` entries at the intended rate?
3. **World capacity**: can those authenticated accounts enter World with the one-use Gameforge permit, load the character list, select a character, run `game_start`, enter the map and remain connected?
4. **Gameplay capacity**: can the measured World-ready population survive controlled movement, chat, NPC and combat workloads?

The default staircase is:

```text
100 -> 250 -> 500 -> 750 -> 1000 -> 1250 -> 1500
```

The tool writes `load-test.json` and `load-test.csv` after every completed stage so a partial run still leaves evidence.

## Authentication model

The load tester now defaults to the same modern authentication path as the NosGM launcher:

```text
account + password
      |
      | HTTP POST /api/v1/launcher/ticket
      v
Master LauncherAuthBridge
      |
      | authorizationCode bound to InstallationId + countryId
      v
NoS0577 / NoS0576 -> regional Login
      |
      | stable SessionId + one-use World permit
      v
World -> thisisgfmode -> character list -> select -> game_start -> finit
```

The account password is used only for the AuthBridge request. It is **not** placed in the modern Login packet and it is **not** sent to World. The modern World entry uses `thisisgfmode` after Login has issued the one-use permit.

`NoS0577` is the default modern header. `NoS0576` can be selected explicitly with `-ModernHeader NoS0576`.

Legacy `NoS0575` remains available only as an explicit compatibility mode:

```powershell
-LoginMode Legacy
```

It is not the default benchmark path.

## Safety boundary

The tester accepts loopback and private-network targets by default. A public Internet address is rejected unless `--allow-public-target` is supplied explicitly. In modern mode the World host, Login host and AuthBridge host are checked.

Use the public-target switch only for infrastructure you own or are authorized to test. Do not point this tool at official NosTale services or third-party servers.

## Prerequisites

- stable .NET 10 x64 SDK;
- NosGM Master, Login and World services already running;
- the NosGM database available to those services;
- modern authentication enabled when using the default mode;
- AuthBridge available, locally `http://127.0.0.1:8081/api/v1/launcher/ticket`;
- Windows 10/11 or Windows Server is the primary production test environment;
- one unique dedicated test account per concurrent Login/World client;
- for World load, each test account must have a character in the selected slot.

For a 1,500-client World run, prepare at least 1,500 dedicated accounts and characters. Do not use real player credentials in a large benchmark CSV.

## Start the modern local stack

For the current local development stack:

```powershell
./scripts/start-modern-login-local.ps1 `
  -SkipBuild `
  -AuthenticationTransport GRPC `
  -AuthenticationGrpcWireMode GRPCWEB
```

Spanish region `5` uses Login port `4005`. The load-test runner now derives the default regional Login port as `4000 + Region`, so region `5` automatically chooses `4005`.

## Validate the generator first

Run the built-in acceptance before touching NosGM:

```powershell
./scripts/run-load-test-local.ps1 -SelfTest
```

The self-test verifies:

- modern `NoS0577` packet construction without embedding account/password data;
- Login-to-World `NsTeST` ticket parsing;
- all four World client transforms;
- fragmented World server-packet decoding;
- a 250-client asynchronous loopback socket run;
- JSON and CSV report creation.

## Phase A: TCP socket capacity

```powershell
./scripts/run-load-test-local.ps1 `
  -Scenario tcp `
  -HostName 127.0.0.1 `
  -Port 1337 `
  -Stages "100,250,500,750,1000,1250,1500" `
  -RampPerSecond 100 `
  -HoldSeconds 30
```

This does **not** claim playable-character capacity. It measures connection acceptance and retention only.

At each stage record server-side telemetry as well:

```text
$Perf runtime
$Perf packets max
$Perf ingress
$Perf scheduler
```

For production-grade measurements, run the generator from a second machine so its own CPU/network usage does not contaminate the NosGM host.

## Account CSV

The same CSV is used for modern Login and World tests:

```csv
username,password,slot
load0001,temporary-test-password,0
load0002,temporary-test-password,0
load0003,temporary-test-password,1
```

The password is sent only to AuthBridge to obtain a short-lived authorization code. It is never written into generated reports or modern Login/World packets.

Slot defaults to `0` when omitted.

## Phase B: modern NoS0577 Login load

Start with a small stage:

```powershell
./scripts/run-load-test-local.ps1 `
  -Scenario login `
  -HostName 127.0.0.1 `
  -AccountsPath C:\NosGM-Test\accounts.csv `
  -Region 5 `
  -LoginMode Modern `
  -AuthBridgeUrl http://127.0.0.1:8081/api/v1/launcher/ticket `
  -ModernHeader NoS0577 `
  -Stages "1,10,25,50,100" `
  -RampPerSecond 10 `
  -HoldSeconds 30
```

Because region `5` is selected and `-Port` is omitted, the runner targets Login `4005` automatically.

Each simulated modern Login client:

1. creates a unique `InstallationId`;
2. POSTs the account/password, InstallationId and country `5` to AuthBridge;
3. receives a short-lived `authorizationCode`;
4. builds the current `NoS0577` packet with the mandatory double-space token boundary, eight-hex random field, vertical-tab country/version field, constant `0` and 32-hex client MD5;
5. encrypts the packet with the Login transport transform;
6. connects to regional Login `4005`;
7. accepts only a usable server-list response.

The console exposes separate `auth-ok` and `login-ok` counters so AuthBridge failures can be distinguished from Login failures.

Use `-ModernHeader NoS0576` to exercise the other accepted modern header.

## Phase C: real modern World character load

Start with **one** real test account first:

```powershell
./scripts/run-load-test-local.ps1 `
  -Scenario world `
  -HostName 127.0.0.1 `
  -Port 1337 `
  -LoginHostName 127.0.0.1 `
  -AccountsPath C:\NosGM-Test\accounts.csv `
  -Region 5 `
  -LoginMode Modern `
  -AuthBridgeUrl http://127.0.0.1:8081/api/v1/launcher/ticket `
  -ModernHeader NoS0577 `
  -Stages "1" `
  -RampPerSecond 1 `
  -HoldSeconds 30
```

The modern World scenario follows the complete server path. Each simulated client:

1. obtains a real AuthBridge authorization code;
2. sends `NoS0577` or `NoS0576` to regional Login;
3. parses its SessionId and advertised endpoint from `NsTeST`;
4. connects to the configured World target;
5. sends the World custom-parameter handshake with that SessionId;
6. sends the current entry bundle, using `thisisgfmode` instead of the account password;
7. consumes the one-use World permit and waits for `clist_end`;
8. sends `select <slot>` and waits for `OK`;
9. sends `game_start`;
10. counts as `WorldReady` only after receiving `finit` by default;
11. keeps the World socket alive during the hold period.

The desired one-client result is:

```text
auth-ok=1
login-ok=1
entry=1
selected=1
world-ready=1
failed=0
```

Once one client is clean, use:

```text
10 -> 25 -> 50 -> 100
```

Then move to the capacity staircase:

```powershell
./scripts/run-load-test-local.ps1 `
  -Scenario world `
  -HostName 127.0.0.1 `
  -Port 1337 `
  -AccountsPath C:\NosGM-Test\accounts.csv `
  -Region 5 `
  -LoginMode Modern `
  -Stages "100,250,500,750,1000,1250,1500" `
  -RampPerSecond 25 `
  -HoldSeconds 60
```

`finit` is intentionally used instead of `OK` as the default ready proof. `OK` confirms character selection, while `finit` is emitted much later in `game_start`, after map entry and a substantial part of character/world initialization.

## Legacy NoS0575 compatibility test

Only use this when deliberately comparing the historical path:

```powershell
./scripts/run-load-test-local.ps1 `
  -Scenario login `
  -LoginMode Legacy `
  -HostName 127.0.0.1 `
  -Port 4005 `
  -AccountsPath C:\NosGM-Test\accounts.csv `
  -Region 5 `
  -Stages "10,25,50" 
```

This path places the legacy credential in `NoS0575` and therefore does not represent the modern launcher architecture.

## Reports

Each stage records:

- attempted and currently connected clients;
- failures plus up to five distinct failure samples on the console and JSON report;
- modern AuthBridge tickets issued;
- AuthBridge p95 latency;
- accepted and rejected/timed-out Login attempts;
- World entries that reached `clist_end`;
- characters that reached selection `OK`;
- characters that reached `WorldReady` after `game_start`;
- connect latency p50/p95/p99;
- Login p95 latency;
- WorldReady latency p50/p95/p99;
- bytes sent/received by the TCP generator;
- aggregate sampled CPU for selected NosGM processes;
- maximum working set/private memory;
- observed NosGM process count.

The files are stored under `artifacts/load-test/<UTC timestamp>/` unless `--output` is supplied.

The reports never include the CSV password, authorization code or raw modern Login packet.

## What is still deliberately separate

WorldReady is a strong capacity floor, but it is not yet a gameplay simulation. The next slice adds controlled workloads for:

- movement and map broadcasts;
- chat;
- NPC/monster interaction;
- skills and combat;
- cross-channel traffic;
- SQL queries/s and slow-query latency export;
- gRPC latency by RPC/method.

The final 1,000-player claim should require 1,000 concurrent `WorldReady` characters and then survive the controlled gameplay workload, not merely 1,000 sockets.

## Stop conditions

Stop the staircase and inspect the previous healthy stage if any of these appear:

- AuthBridge, Login, World entry, selection or WorldReady failures rise materially;
- p95/p99 latency climbs continuously between stages;
- one NosGM process pins a CPU core or the host approaches saturation;
- managed heap/working set grows without stabilizing;
- Gen2 collections accelerate sharply;
- `$Perf` handler latency or ingress queue wait grows continuously;
- scheduler maximum tick latency spikes;
- SQL or gRPC failures appear in service logs.

The last stable stage becomes the current measured capacity floor. Optimizations should be tested against the same staircase and ramp so before/after results remain comparable.
