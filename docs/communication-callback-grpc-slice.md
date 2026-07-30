# NosGM communication callback gRPC slice

## Why server streaming

The legacy `ICommunicationClient` surface lets Master call directly into Login
and World processes through SCS callbacks. The current callers still target
.NET Framework 4.8.1, and Windows 10 uses binary gRPC-Web. gRPC-Web supports
server streaming but not bidirectional streaming, so Login and World will open
one authenticated subscription to the central .NET 10 runtime and receive typed
callback envelopes over that stream.

This avoids adding public callback listeners to every legacy process and keeps
all transport identity, replay protection, deadlines and message limits at the
existing loopback mTLS boundary.

## Contract shape

`ClusterCommunicationCallbacks` has two operations:

- `SubscribeCommunicationCallbacks`: Login or World opens a server-streaming
  subscription. World supplies its canonical World ID, channel and group. Login
  supplies no World identity. A subscriber may filter callback kinds and sends
  the last sequence it applied so retained events can be replayed after a short
  reconnect.
- `PublishCommunicationCallback`: Master publishes one typed event with a
  canonical event ID, bounded TTL and explicit target. The runtime assigns a
  monotonic sequence and reports how many active subscribers matched.

Every callback is represented by a Protobuf `oneof`. There is no generic method
name, CLR object graph, `object[]`, raw byte payload or already-rendered NosTale
message string.

## First callback events

| Legacy callback | Typed event | Target |
| --- | --- | --- |
| `CharacterConnected` | `CharacterPresenceCallback` | World group |
| `CharacterDisconnected` | `CharacterPresenceCallback` | World group |
| `KickSession` | `KickSessionCallback` | All Worlds |
| `Restart` | `LifecycleCallback` | All Worlds or one World group |
| `RunGlobalEvent` | `GlobalEventCallback` | All Worlds |
| `Shutdown` | `LifecycleCallback` | All Worlds or one World group |
| `UpdateBazaar` | `BazaarRefreshCallback` | World group |
| `UpdateFamily` | `FamilyRefreshCallback` | World group |
| `UpdatePenaltyLog` | `PenaltyRefreshCallback` | Login and World nodes |
| `UpdateRelation` | `RelationRefreshCallback` | World group |
| `UpdateStaticBonus` | `StaticBonusRefreshCallback` | Character ID |

`SendMessageToCharacter` remains deliberately deferred. The legacy
`SCSCharacterMessage` may contain an already-rendered client packet and has
routing behavior for whispers, shouts, support messages, faction restrictions
and destination lookup. It will receive a dedicated typed messaging slice
instead of tunneling that DTO through Protobuf.

## Delivery and backpressure

The runtime implementation must use:

- a bounded pending queue per subscriber;
- a bounded retained replay window per process identity;
- monotonically increasing sequences;
- canonical event IDs for idempotent subscriber application;
- event expiry so lifecycle or cache notifications cannot be replayed forever;
- cancellation when a subscriber disconnects;
- fail-closed publishing when capacity is exhausted.

The first contract fixes these ceilings:

- 1,024 pending events per subscriber;
- 4,096 retained events per subscriber;
- callback TTL from 1 to 300 seconds;
- restart delay no greater than one hour.

A future implementation may lower those values through stricter runtime
configuration, but it must never exceed the contract ceilings.

## Authorization

- Only the Master certificate may publish.
- World certificates may subscribe to World callbacks.
- Login certificates may subscribe only to callback kinds intended for Login;
  the first slice permits penalty refresh.
- The caller role in `RequestContext` must match the authenticated certificate.
- Shared SCS keys are not copied to the new contract.

The current certificate bundle does not yet issue a Master client certificate.
Runtime activation therefore remains blocked until the server options,
certificate generator, local acceptance script and production startup script
all provision a distinct Master identity.

## Next implementation steps

1. Extend the local certificate bundle and role map with a unique Master client
   certificate.
2. Implement the bounded subscriber registry, replay buffers and sequence
   assignment in the .NET 10 runtime.
3. Add a dual-target publisher client for Master and a server-streaming
   subscriber client for Login and World.
4. Translate typed events into the existing in-process events while SCS remains
   the default rollback transport.
5. Prove reconnect replay, duplicate event suppression, queue exhaustion,
   cancellation and role rejection.
6. Migrate cross-server transitions and the dedicated character messaging
   slice.
7. Remove the guarded communication cutover block only when every operation
   that shares Master state uses the same selected authority.
