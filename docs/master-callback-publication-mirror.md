# Master communication callback publication mirror

The legacy Master process can now publish typed copies of selected SCS callbacks to the central .NET 10 callback runtime. This is an observation and replay-validation stage. SCS remains the only transport allowed to apply gameplay, account or lifecycle effects.

## Activation

The mirror reads:

```text
NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_ENABLED
NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_QUEUE_CAPACITY
NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_STOP_TIMEOUT_MILLISECONDS
```

Activation defaults to `false`. When disabled, Master does not load the callback-publisher certificate and creates no worker or queue.

The bounded queue defaults to 4,096 entries and accepts values from 64 through 16,384. Shutdown waits up to 5,000 milliseconds by default and accepts values from 1,000 through 30,000 milliseconds. Boolean values accept only `true` or `false` without surrounding whitespace.

Publisher credentials remain in the communication-specific Master namespace:

```text
NOSGM_COMMUNICATION_GRPC_URL
NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PATH
NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PASSWORD
NOSGM_COMMUNICATION_GRPC_TRUSTED_ROOT_CERT_PATH
NOSGM_COMMUNICATION_GRPC_MASTER_INSTANCE_ID
NOSGM_COMMUNICATION_GRPC_DEADLINE_MILLISECONDS
NOSGM_COMMUNICATION_GRPC_WIRE_MODE
```

The AuthBridge certificate namespace is never read by the callback publisher.

## SCS-first ordering

Master registers `MirroredCommunicationService` as its SCS communication service. It inherits the complete legacy implementation and reimplements the ten emitters whose typed destinations preserve the current legacy recipient set.

Every reimplemented method follows the same authority boundary:

1. execute the original `CommunicationService` method;
2. preserve its existing result or completed side effect;
3. enqueue a typed mirror publication without waiting for network I/O.

A mirror construction, queue or publication failure cannot replace the SCS result. `ConnectCharacter` publishes only after the legacy method returns success. `DisconnectCharacter` snapshots the authenticated legacy connection before teardown and publishes only after the SCS method completes.

## Character presence scope

The current legacy `ConnectCharacter` and `DisconnectCharacter` implementations broadcast to every registered World whose `WorldGroup` matches the connected account, including the source World. The receiving `CommunicationClient` forwards that callback without another source-World filter.

The typed `CharacterPresenceCallback` therefore uses the existing `WORLD_GROUP` target. This matches the actual SCS recipient set directly and does not require a new Protobuf target or per-World fan-out.

An unexpectedly missing World group is logged and discarded after SCS has completed. It cannot fault the mirror worker or alter the legacy result.

## Bounded asynchronous publication

`MasterCommunicationCallbackMirror` owns one bounded FIFO `BlockingCollection` and one publisher worker. Callback threads use non-blocking `TryAdd`; they never wait for gRPC.

Each queued item contains one immutable Protobuf publication template with a canonical event GUID. Every network attempt clones that template and creates a fresh request GUID and deadline while preserving the event GUID and semantic payload. The runtime therefore treats a transient retry as idempotent and returns the original accepted sequence.

Transient transport and capacity failures retry in FIFO order with bounded exponential delay. A local item expires after its callback TTL. Queue saturation, local expiry, build failures and terminal publisher failures are counted and logged. None of them invokes or retries the SCS callback.

On shutdown Master completes the queue and allows a bounded drain. When the timeout expires, it cancels the worker and disposes the gRPC channel without aborting a thread.

## Mirrored callback inventory

| Legacy emitter | Typed payload | Target |
| --- | --- | --- |
| `ConnectCharacter` | character connected | World group |
| `DisconnectCharacter` | character disconnected | World group |
| `KickSession` | kick session | All Worlds |
| `Restart` | lifecycle restart | All Worlds or World group |
| `RunGlobalEvent` | global event | All Worlds |
| `Shutdown` | lifecycle shutdown | All Worlds or World group |
| `UpdateBazaar` | Bazaar refresh | World group |
| `UpdateFamily` | Family refresh | World group |
| `RefreshPenalty` | Penalty refresh | All Login and World nodes |
| `UpdateRelation` | Relation refresh | World group |

`UpdateStaticBonus` has a typed request builder for the future `CharacterId` route, but the current `ICommunicationService` exposes no SCS emitter to mirror. It is not fabricated from another operation.

`SendMessageToCharacter` remains deferred because its DTO may contain rendered client packets and complex routing rules. It requires a separate typed messaging contract.

## Observability

Master records:

- enabled or disabled startup;
- bounded queue capacity and shutdown timeout;
- queue drops with logarithmically throttled warnings;
- missing presence scope after legacy delivery;
- locally expired publications;
- terminal publisher failure while declaring SCS authoritative;
- final enqueued, published, dropped and expired counts.

A publication count proves that the central runtime accepted the typed event. It does not mean the shadow subscriber applied a gameplay effect. Shadow handlers remain observation-only.

## Next cutover boundary

The next stage collects subscriber parity evidence and introduces a server-issued replay-complete barrier. Login and World must prove that their shadow cursors have consumed every mirrored event up to a declared sequence. Only then can an atomic inbound gate disable the matching SCS callback and enable the typed dispatcher exactly once, without a gap or duplicate effect.
