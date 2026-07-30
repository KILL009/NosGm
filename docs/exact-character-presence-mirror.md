# Exact character presence mirror routing

Character connection and disconnection callbacks now join the disabled-by-default Master publication mirror without widening the Protobuf target contract. SCS remains the only transport allowed to apply presence effects.

## Legacy recipient parity

The legacy Master callback sends character presence to every other World in the source World group and excludes the source World. The source already owns the local connection state and must not receive its own cross-World notification again.

Before the legacy callback runs, Master snapshots the registered World topology through `CharacterPresenceMirrorRoutePlanner`. The planner:

- finds the exact source World;
- compares World groups with ordinal equality;
- excludes the source World;
- excludes other groups and empty World IDs;
- removes duplicate World IDs;
- returns a deterministic order.

After SCS completes successfully, Master enqueues one `WORLD_ID` publication per peer World. The source World never receives a typed mirror copy. A failed or redundant legacy character operation produces no presence publication.

## Bounded publication behavior

Presence copies use the same bounded FIFO worker, non-blocking `TryAdd`, immutable `EventId`, retry policy and local TTL as the other mirrored callbacks. There is no second queue, channel or worker.

A target World may not have its callback-only route or active subscriber yet. For presence only:

- `NotFound` becomes a counted `TARGET_NOT_REGISTERED` drop;
- a successful publication with zero matched subscribers becomes a counted `TARGET_NOT_SUBSCRIBED` drop;
- neither condition faults the shared mirror worker;
- SCS is never retried or rolled back.

Transient runtime and transport failures retain their existing bounded retry behavior.

## Contract boundary

`CharacterPresenceCallback` now requires an exact `WORLD_ID` target. The previous `WORLD_GROUP` pairing fails contract validation and fails envelope validation before the durable cursor can advance.

The central runtime therefore retains and replays each presence copy only for its exact peer World. Master performs the fan-out once; the runtime does not broadcast the event a second time.

## Validation

The migration includes:

- pure route-planner tests for source exclusion, group isolation, duplicate removal, unknown sources and deterministic ordering;
- contract tests accepting `CharacterPresence + WORLD_ID` and rejecting `CharacterPresence + WORLD_GROUP`;
- hub tests proving one exact World receives the presence event;
- compiled CLR interface-map checks proving `ConnectCharacter` and `DisconnectCharacter` dispatch through `MirroredCommunicationService`;
- source-order guards proving SCS executes before mirror enqueue;
- guards preventing redundant disconnect callbacks from producing mirror events.

## Remaining cutover boundary

Shadow subscribers still observe only and typed dispatch remains blocked. The next stage collects parity evidence and adds a server-issued replay-complete barrier. Login and World must prove their generation-scoped cursors have consumed the mirrored sequence before any matching SCS callback can be disabled and the typed dispatcher enabled exactly once.
