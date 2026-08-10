# PenaltyRefresh gRPC authority cutover

## Status

`PenaltyRefresh` is the first Communication callback to complete its SCS-to-gRPC authority cutover after Configuration.

Final architecture:

- Master publishes one typed `PenaltyRefreshCallback` through `ClusterCommunicationCallbacks`;
- the publication uses the Master mTLS identity and one idempotent `EventId` across bounded transient retries;
- a successful central acceptance requires `Success` plus a positive accepted runtime sequence;
- Login and World consume `PenaltyRefresh` through authenticated server-streaming subscriptions with separate durable cursor files;
- `PenaltyRefresh` is applied directly from the validated typed envelope;
- the callback processor commits its durable sequence only after the typed effect completes successfully;
- `UpdatePenaltyLog` is absent from `ICommunicationClient` and from the remaining SCS callback inventory;
- no SCS fallback exists for `PenaltyRefresh`;
- every other Communication callback keeps its existing SCS authority until its own cutover.

Configuration remains gRPC-only and is unchanged by this slice.

## Publication and failure semantics

`MirroredCommunicationService.RefreshPenalty` no longer calls the legacy `CommunicationService.RefreshPenalty` SCS fanout and no longer queues `TryPenaltyRefresh` on the shadow mirror.

Instead, `MasterPenaltyRefreshGrpcAuthority` publishes `PenaltyRefreshCallback` synchronously through the typed gRPC publisher. Transient `Unavailable`, `CapacityExceeded`, and transport availability failures receive a small bounded retry window while preserving the same `EventId`. If no valid central acceptance is obtained, the request fails closed. The implementation never retries the effect over SCS.

The production authority does not use the raw live subscriber count as a correctness gate. The central accepted sequence is the durable publication boundary, allowing the callback replay model to remain useful when a subscriber reconnects. The final local acceptance separately proves that the intended Login and World routes are both live.

## Subscriber authority

`CommunicationCallbackEnvelopeDispatcher` treats only `PenaltyRefresh` as completed typed authority. It applies that callback directly after the subscriber validates the envelope. Other callback kinds still pass through their transitional coordinator, so `NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED=false` continues to prevent broad typed effect activation.

`CommunicationCallbackProcessor` saves the cursor only after handler completion. Therefore an application failure cannot advance the durable cursor and silently lose the event.

## SCS retirement

The legacy callback surface now has these invariants:

- `ICommunicationClient` contains no `UpdatePenaltyLog` method;
- `CommunicationClient` contains no SCS `UpdatePenaltyLog` receiver;
- the legacy SCS inventory contains 94 methods rather than 95;
- the callback migration map schema version 2 records `UpdatePenaltyLog` under `completed` with `grpc_authoritative`, `legacySurfaceRemoved: true`, and `fallback: null`;
- a dead compile-compatibility extension throws `NotSupportedException` if the old base path is accidentally called, so it cannot silently resurrect SCS delivery.

## Local final acceptance

Start the final Windows stack with:

```powershell
./scripts/start-penaltyrefresh-grpc-authority-local.ps1
```

The compatibility command `start-communication-callback-shadow-local.ps1` now redirects to the same final startup so an old operator command cannot claim that SCS still owns `PenaltyRefresh`.

The final startup records:

- `CommunicationCallbackMode = PenaltyRefreshAuthority`;
- `PenaltyRefreshCallbackAuthority = gRPC`;
- `PenaltyRefreshCallbackFallback = null`;
- `RemainingCommunicationCallbackAuthority = SCS`.

Run the focused acceptance with:

```powershell
./scripts/test-communication-callback-shadow-local.ps1
```

The historical filename is retained for compatibility, but the test now validates final authority. Its dedicated .NET 10 probe publishes `PenaltyRefresh` with the reserved nonexistent positive ID `int.MaxValue`. The probe itself never accesses `PenaltyLogDAO` or calls a gameplay service directly. Login and World must both durably advance to the accepted sequence in the same callback runtime generation.

Expected terminal line:

```text
Communication PenaltyRefresh real-process gRPC authority acceptance passed.
```

Stop normally with:

```powershell
./scripts/stop-modern-login-local.ps1
```

## Explicit non-goals

This slice does not migrate:

- `SendMessageToCharacter`;
- character presence callbacks;
- kick/session callbacks;
- lifecycle restart/shutdown callbacks;
- global events;
- bazaar, family, relation or static-bonus refresh callbacks;
- the remaining `ICommunicationService` request surface.

Those remain separate migrations so one callback cannot accidentally broaden transport authority for another.

## Merge gate

Do not merge the final authority slice until all of the following are green on the exact PR head:

- Windows .NET Framework build and legacy runtime guards;
- .NET 10 foundation and callback contract tests;
- CodeQL and repository security checks;
- final Windows real-process acceptance showing one accepted `PenaltyRefresh` sequence durably committed by Login and World in the same runtime generation;
- no `UpdatePenaltyLog` method in the SCS callback interface and no PenaltyRefresh SCS fallback.
