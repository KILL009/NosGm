# NosGM staged load testing

## Goal

NosGM load testing separates lower-layer capacity from real playable-character capacity so the final number is defensible:

1. **TCP capacity**: can Login/World accept and hold the intended number of concurrent sockets?
2. **Login capacity**: can Login process real `NoS0575` authentication attempts with unique test accounts while Master and SQL are active?
3. **World capacity**: can those accounts obtain a real SessionId, enter World, load the character list, select a character, run `game_start`, enter the map and remain connected?

The default staircase is:

```text
100 -> 250 -> 500 -> 750 -> 1000 -> 1250 -> 1500
```

The tool writes `load-test.json` and `load-test.csv` after every completed stage so a partial run still leaves evidence.

## Safety boundary

The tester accepts loopback and private-network targets by default. A public Internet address is rejected unless `--allow-public-target` is supplied explicitly. In the World scenario both the Login host and World host pass this check. Use the public-target switch only for infrastructure you own or are authorized to test.

Do not point this tool at the official NosTale service or third-party servers.

## Prerequisites

- stable .NET 10 x64 SDK;
- NosGM Master, Login and World services already running;
- the NosGM database available to those services;
- Windows 10/11 or Windows Server is the primary production test environment;
- one unique dedicated test account per concurrent `login` or `world` client;
- for `world`, each test account must have a character in the selected slot.

For a 1,500-client World run, prepare at least 1,500 dedicated accounts and characters. Do not use real player credentials in the CSV.

## Validate the generator first

Run the built-in acceptance before touching NosGM:

```powershell
./scripts/run-load-test-local.ps1 -SelfTest
```

The self-test now verifies the Login-to-World ticket parser, the World custom-parameter/client-packet codecs, fragmented World server-packet decoding, and a 250-client asynchronous loopback socket run. It also verifies that JSON and CSV reports are produced.

## Phase A: TCP socket capacity

Start with World on the normal local port:

```powershell
./scripts/run-load-test-local.ps1 `
  -Scenario tcp `
  -HostName 127.0.0.1 `
  -Port 1337 `
  -Stages "100,250,500,750,1000,1250,1500" `
  -RampPerSecond 100 `
  -HoldSeconds 30
```

This does **not** claim 1,500 playable characters. It measures connection acceptance and retention only.

At each stage record the server-side GM telemetry as well:

```text
$Perf runtime
$Perf packets max
$Perf ingress
$Perf scheduler
```

`$Perf runtime` exposes process CPU, working set/private/managed heap, GC collections, packets/s, handler throughput/latency, thread-pool usage and scheduler/ingress health. The load generator separately samples the configured NosGM process names when it runs on the same Windows machine.

For production-grade measurements, run the generator from a second machine so its own CPU/network usage does not contaminate the NosGM host.

## Phase B: real Login load

Create a CSV outside the repository:

```csv
username,password
load0001,temporary-test-password
load0002,temporary-test-password
load0003,temporary-test-password
```

Then run:

```powershell
./scripts/run-load-test-local.ps1 `
  -Scenario login `
  -HostName 127.0.0.1 `
  -Port 4000 `
  -AccountsPath C:\NosGM-Test\accounts.csv `
  -Region 5 `
  -ClientVersion 0.9.3.3254 `
  -Stages "100,250,500,750,1000" `
  -RampPerSecond 50 `
  -HoldSeconds 30
```

The default Login packet template follows the current `LoginPacket` field order:

```text
NoS0575 0 {username} {password} {gameforgeId} {clientDataOld} {region} {clientData}
```

The load client applies the inverse of `LoginCryptography.Decrypt` before sending the packet and decodes the first server response using the current Login server transform. A response that is not `failc` and contains the normal server-list shape is counted as `loginAccepted`.

If the active client protocol changes, override the template instead of editing the tester:

```text
--login-template "... {username} ... {password} ..."
```

Supported tokens are `{index}`, `{username}`, `{password}`, `{gameforgeId}`, `{clientDataOld}`, `{region}`, `{clientData}`, and `{clientVersion}`.

## Phase C: real World character load

The World scenario uses the complete server path instead of manufacturing a SessionId. Each simulated client:

1. connects to Login and sends the real `NoS0575` packet;
2. parses its SessionId and advertised World endpoint from `NsTeST`;
3. connects to the configured World target;
4. sends the World custom-parameter handshake with that SessionId;
5. sends the two real entry-bundle parts required by the current `NosGm.EntryPoint` handler;
6. waits for `clist_end`;
7. sends `select <slot>` and waits for `OK`;
8. sends `game_start`;
9. counts as `WorldReady` only after receiving the configured late startup packet, `finit` by default;
10. keeps the World socket alive for the stage hold period.

For World load, add the character slot to the CSV. Slot defaults to `0` when omitted:

```csv
username,password,slot
load0001,temporary-test-password,0
load0002,temporary-test-password,0
load0003,temporary-test-password,1
```

Start small before using the full staircase:

```powershell
./scripts/run-load-test-local.ps1 `
  -Scenario world `
  -HostName 127.0.0.1 `
  -Port 1337 `
  -LoginHostName 127.0.0.1 `
  -LoginPort 4000 `
  -AccountsPath C:\NosGM-Test\accounts.csv `
  -Region 5 `
  -Stages "10,25,50,100" `
  -RampPerSecond 10 `
  -HoldSeconds 30
```

Once 100 is clean, run the capacity staircase:

```powershell
./scripts/run-load-test-local.ps1 `
  -Scenario world `
  -HostName 127.0.0.1 `
  -Port 1337 `
  -LoginPort 4000 `
  -AccountsPath C:\NosGM-Test\accounts.csv `
  -Stages "100,250,500,750,1000,1250,1500" `
  -RampPerSecond 25 `
  -HoldSeconds 60
```

`finit` is intentionally used instead of `OK` as the default ready proof. `OK` confirms character selection, while `finit` is emitted much later in `game_start`, after map entry and a substantial part of character/world initialization. It can be changed with `-WorldReadyPacket` / `--world-ready-packet` if the server startup sequence changes.

The World target is explicit even though `NsTeST` contains an advertised endpoint. The tester records/parses the Login ticket but does not silently redirect its load to another host. This keeps the target under operator control and preserves the safety boundary.

## Reports

Each stage records:

- attempted and currently connected clients;
- failures;
- accepted and rejected/timed-out Login attempts;
- World entries that reached `clist_end`;
- characters that reached selection `OK`;
- characters that reached `WorldReady` after `game_start`;
- connect latency p50/p95/p99;
- Login p95 latency;
- WorldReady latency p50/p95/p99;
- bytes sent/received by the generator;
- aggregate sampled CPU for selected NosGM processes;
- maximum working set/private memory;
- observed NosGM process count.

The files are stored under `artifacts/load-test/<UTC timestamp>/` unless `--output` is supplied.

## What is still deliberately separate

The World scenario establishes a much stronger capacity floor, but it still does not manufacture fake gameplay activity. The next slice will add controlled workloads for:

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

- Login, World entry, selection or WorldReady failures rise materially;
- p95/p99 latency climbs continuously between stages;
- one NosGM process pins a CPU core or the host approaches saturation;
- managed heap/working set grows without stabilizing;
- Gen2 collections accelerate sharply;
- `$Perf` handler latency or ingress queue wait grows continuously;
- scheduler maximum tick latency spikes;
- SQL or gRPC failures appear in service logs.

The last stable stage becomes the current measured capacity floor. Optimizations should then be tested against the same staircase and ramp so before/after results remain comparable.
