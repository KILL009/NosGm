# NosGM SCS transport migration

## Decision

NosGM will replace SCS with typed gRPC services and Protobuf contracts. The
replacement is internal communication between NosGM processes. It does not
change the NosTale client protocol, NoS0577, `NsTeST`, the World handshake, or
any gameplay packet.

The migration is intentionally incremental. SCS remains the active transport
until a complete service slice has passed compatibility and rollback testing.

## Why SCS must be replaced

The current SCS implementation:

- serializes arbitrary CLR objects with `BinaryFormatter`;
- creates dynamic service proxies with .NET Remoting `RealProxy`;
- accepts frames up to 128 MiB;
- discovers methods and parameters at runtime;
- performs blocking request/reply calls without an explicit cancellation
  contract.

`BinaryFormatter` and .NET Remoting are not valid foundations for .NET 10. A
compatibility package that restores unsafe deserialization is not an accepted
production solution.

## Version 1 foundation

The first contract is deliberately small:

- `ClusterControl.Negotiate` selects a supported major/minor protocol version
  and service set.
- `ClusterControl.CheckHealth` reports whether a node can serve cluster calls.
- Every request carries a canonical correlation ID, caller identity, node role,
  requested service, issue time, and bounded deadline.
- Inbound and outbound messages are capped at 4 MiB.
- The maximum deadline is 60 seconds.
- The dispatch contract reserves a bounded queue of 2,048 calls and no
  reflection-style `Invoke(string, object[])` operation.
- Credentials, Gameforge tokens, password hashes, and authentication keys are
  excluded from the control schema and must never be logged.

The `.proto` file is the wire source of truth. `Grpc.Tools` generates typed
client and server stubs during the build, in an isolated
`NosGm.Cluster.Wire.V1` namespace. The dual-target
`NosGm.Cluster.Contracts` library provides those generated types plus policy
and validation types that can be consumed by both the `net481` compatibility
side and .NET 10 services.

## Frozen legacy surface

`contracts/cluster/v1/legacy-scs-surface.json` inventories all 99 methods across
the six server interfaces and three callback interfaces. CI compares that
manifest with the source declarations. A legacy interface cannot silently gain,
lose, or rename a method while its typed Protobuf replacement is being built.

The inventory is a migration checklist, not a new dynamic RPC contract. Each
service will receive explicit request and response messages.

## Rollout

1. **Contract foundation** — version negotiation, health, limits, validation,
   legacy inventory, and self-tests. No runtime traffic changes.
2. **Authentication slice** — add typed authentication RPCs behind an adapter.
   Compare read-only results with SCS; route side effects through only one
   transport.
3. **Communication slice** — migrate account/session and World registration
   calls while preserving the verified Login → Master → World sequence.
4. **Supporting services** — configuration, mail, mall, callbacks, and
   administrative operations.
5. **Cutover** — enable gRPC per service behind configuration, retain immediate
   rollback, then remove `BinaryFormatter`, `RealProxy`, and the SCS code only
   after full acceptance.

## Security requirements for runtime phases

- TLS is mandatory between machines; production should use mutually
  authenticated service identities.
- Every endpoint uses an explicit allow-list and authorization policy.
- gRPC deadlines and cancellation flow into handlers.
- Bounded channels apply backpressure instead of creating unbounded tasks.
- Request IDs are included in sanitized audit logs; credentials and payload
  bodies are not.
- Side-effecting calls are never mirrored to two transports.
- Replay protection remains mandatory for temporary login tickets and World
  permits.

## Non-regression boundary

The following remain unchanged throughout this migration:

- client `0.9.3.3254`;
- NoS0575, NoS0576, and exact NoS0577 byte layout;
- ten regional Login listeners and culture mapping;
- Gameforge ticket consumption and one-use World permits;
- `NsTeST`, channel selection, character list, `game_start`, and map entry;
- database schemas and contents;
- inventory, maps, combat, SP behavior, GM Bridge, and Discord integration.

World remains the final executable migrated to .NET 10.
