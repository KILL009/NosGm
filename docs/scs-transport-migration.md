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

## Authentication contract slice

The first service-specific contract now maps the stateful Gameforge
authentication boundary without changing runtime routing:

| Legacy SCS operation | Typed RPC or disposition | Authorized role |
| --- | --- | --- |
| `RegisterGameforgeAuthTicket` | `IssueAuthTicket` | AuthBridge |
| `ConsumeGameforgeAuthTicket` | `ConsumeAuthTicket` | Login |
| `RegisterGameforgeWorldPermit` | `IssueWorldPermit` | Login |
| `ConsumeGameforgeWorldPermit` | `ConsumeWorldPermit` | World |
| `RevokeGameforgeWorldPermit` | `RevokeWorldPermit` | Login |
| `Authenticate` | transport identity; future mTLS | n/a |
| `ValidateAccount` | deferred; no active authentication-service consumer | n/a |
| `ValidateAccountAndCharacter` | deferred; no active consumer | n/a |

Every request uses the common versioned request context. Authorization material
is capped at 4,096 characters, installation IDs are canonical non-empty GUIDs,
country IDs are limited to `0..9`, account/session IDs must be positive, and IP
bindings are parsed and length-bounded. Password hashes, authentication keys,
arbitrary DTO graphs, generic method names, and untyped byte payloads are not
part of this contract.

All five RPCs are side-effecting. A ticket or one-use World permit must be
handled by exactly one transport. The future adapter may select gRPC or SCS for
an operation, but it must never shadow-execute, retry blindly, or compare these
operations by running both.

The isolated .NET 10 authentication runtime now implements these five RPCs. It
binds only to loopback HTTP/2, requires an OS-valid mTLS client certificate,
maps distinct certificate SHA-256 allow-lists to AuthBridge, Login, and World
roles, enforces bounded request and transport deadlines, rejects replayed
request IDs, and applies bounded dispatch. It preserves stable SessionID reuse
for exactly three ticket consumptions and one-use World permit behavior.

This runtime is not yet selected by AuthBridge, Login, or World. SCS remains
the default transport and authoritative state owner. The shared transport
router selects exactly one implementation before a side effect begins; it
never mirrors an operation or retries a failed gRPC call through SCS.

## Authentication runtime configuration

The runtime refuses to start unless its TLS identity and all three caller
allow-lists are configured:

| Variable | Purpose |
| --- | --- |
| `NOSGM_AUTH_GRPC_SERVER_CERT_PATH` | Absolute path to the server PKCS#12 certificate |
| `NOSGM_AUTH_GRPC_SERVER_CERT_PASSWORD` | Optional PKCS#12 password; never logged |
| `NOSGM_AUTH_GRPC_AUTHBRIDGE_CERT_SHA256` | Allowed AuthBridge client certificate fingerprint(s) |
| `NOSGM_AUTH_GRPC_LOGIN_CERT_SHA256` | Allowed Login client certificate fingerprint(s) |
| `NOSGM_AUTH_GRPC_WORLD_CERT_SHA256` | Allowed World client certificate fingerprint(s) |
| `NOSGM_AUTH_GRPC_PORT` | Loopback port; default `7443` |
| `NOSGM_AUTH_GRPC_TICKET_TTL_SECONDS` | Ticket lifetime from 15 to 600 seconds; default `120` |
| `NOSGM_AUTH_GRPC_PERMIT_TTL_SECONDS` | World permit lifetime from 15 to 600 seconds; default `120` |
| `NOSGM_AUTH_GRPC_INSTANCE_ID` | Bounded runtime identity used only in safe operational logs |

Fingerprints may be comma-separated but cannot be reused across roles.
Certificate chain validation remains enabled; no “accept any certificate”
escape hatch exists.

The future adapters use a single `SCS` or `GRPC` transport value. Missing or
unknown values fail closed, and an absent value intentionally resolves to
`SCS`. A failed stateful call is returned to its caller without cross-transport
fallback because the remote side may already have committed the mutation.

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
2. **Authentication contract** — add the five typed ticket/permit RPCs, caller
   policies, strict validators, and a complete legacy-method disposition map.
   No runtime traffic changes.
3. **Authentication runtime** — the isolated mTLS host and safe selector are
   present. Next add the three caller adapters, then route each side effect
   through exactly one explicitly selected transport.
4. **Communication slice** — migrate account/session and World registration
   calls while preserving the verified Login → Master → World sequence.
5. **Supporting services** — configuration, mail, mall, callbacks, and
   administrative operations.
6. **Cutover** — enable gRPC per service behind configuration, retain immediate
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
