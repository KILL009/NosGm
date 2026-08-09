# NosGM staged load testing

## Goal

The first load-testing slice answers two different questions without pretending they are the same benchmark:

1. **TCP capacity**: can Login/World accept and hold the intended number of concurrent sockets without connection failures, process saturation, or memory growth?
2. **Login capacity**: can Login process real `NoS0575` authentication attempts with unique test accounts while Master, SQL Server, the packet pipeline, and the current internal transport are active?

The default staircase is:

```text
100 -> 250 -> 500 -> 750 -> 1000 -> 1250 -> 1500
```

The tool writes `load-test.json` and `load-test.csv` after every completed stage so a partial run still leaves evidence.

## Safety boundary

The tester accepts loopback and private-network targets by default. A public Internet address is rejected unless `--allow-public-target` is supplied explicitly. Use that switch only for infrastructure you own or are authorized to test.

Do not point this tool at the official NosTale service or third-party servers.

## Prerequisites

- stable .NET 10 x64 SDK;
- NosGM services already running;
- Windows 10/11 or Windows Server is the primary production test environment;
- for the `login` scenario, one unique test account per concurrent simulated client.

For a 1,500-client login run, prepare at least 1,500 dedicated test accounts. Do not use real player credentials in the CSV.

## Validate the generator first

Run the built-in loopback acceptance before touching NosGM:

```powershell
./scripts/run-load-test-local.ps1 -SelfTest
```

It starts an ephemeral loopback listener and verifies that the generator can establish and hold 250 asynchronous TCP clients and produce both report files.

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

This does **not** claim 1,500 playable characters. It measures connection acceptance/retention and gives us a clean lower layer before authentication and gameplay are added.

At each stage record the server-side GM telemetry as well:

```text
$Perf runtime
$Perf packets max
$Perf ingress
$Perf scheduler
```

`$Perf runtime` already exposes process CPU, working set/private/managed heap, GC collections, packets/s, handler throughput/latency, thread-pool usage and scheduler/ingress health. The load generator separately samples the configured NosGM process names when it runs on the same Windows machine.

For production-grade measurements, run the load generator from a second machine so its own CPU/network usage does not contaminate the NosGM host.

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

The default login packet template follows the current `LoginPacket` field order:

```text
NoS0575 0 {username} {password} {gameforgeId} {clientDataOld} {region} {clientData}
```

The load client applies the inverse of `LoginCryptography.Decrypt` before sending the packet and decodes the first server response using the current Login server transform. A response that is not `failc` and contains the normal server-list shape is counted as `loginAccepted`.

If the active client protocol changes, override the template instead of editing the tester:

```text
--login-template "... {username} ... {password} ..."
```

Supported tokens are `{index}`, `{username}`, `{password}`, `{gameforgeId}`, `{clientDataOld}`, `{region}`, `{clientData}`, and `{clientVersion}`.

## Reports

Each stage records:

- attempted clients;
- currently connected clients;
- connect failures;
- accepted login responses and rejected/timed-out login responses;
- connect latency p50/p95/p99;
- bytes sent/received by the generator;
- aggregate sampled CPU for the selected NosGM processes;
- maximum working set/private memory;
- observed NosGM process count.

The files are stored under `artifacts/load-test/<UTC timestamp>/` unless `--output` is supplied.

## What this first slice does not measure yet

This foundation deliberately does not invent numbers for areas that require server-side instrumentation or a real selected character:

- SQL queries/s and slow-query latency;
- gRPC RPC latency by method;
- full character selection/world handshake;
- movement, map broadcasts, monsters, skills and combat;
- cross-channel traffic.

Those belong to the next two slices:

1. **World scenario**: authenticated account -> World permit/session -> character selection -> selected character held in a map.
2. **Gameplay scenario**: controlled movement/chat/skill pulses plus automatic server-side SQL/gRPC telemetry export.

The target is to make the final 1,000-player claim only after the gameplay scenario remains healthy at 1,000 concurrent selected characters, not merely because 1,000 sockets can connect.

## Stop conditions

Stop the staircase and inspect the previous healthy stage if any of these appear:

- connection/login failure rate rises materially;
- p95/p99 latency climbs continuously between stages;
- one NosGM process pins a CPU core or the host approaches saturation;
- managed heap/working set grows without stabilizing;
- Gen2 collections accelerate sharply;
- `$Perf` handler latency or ingress queue wait grows continuously;
- scheduler maximum tick latency spikes;
- SQL or gRPC failures appear in the service logs.

The last stable stage becomes the current measured capacity floor. Optimizations should then be tested against the exact same staircase and ramp so before/after results remain comparable.
