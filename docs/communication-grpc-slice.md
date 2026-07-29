# NosGM communication gRPC slice

## Purpose

This slice begins the replacement of `ICommunicationService` after the
Gameforge authentication path was proven with the real client. It defines the
typed Master/Login/World coordination contract only. It does not start a new
listener, switch runtime traffic, remove SCS, or alter any NosTale packet.

## First vertical slice

The first `ClusterCommunication` service covers the state needed by the
verified login and character-reselection path:

| Legacy method | Typed RPC | Caller role |
| --- | --- | --- |
| `RegisterAccountLogin` | `RegisterAccountLogin` | Login |
| `IsAccountSessionRegistered` | `IsAccountSessionRegistered` | Login |
| `IsLoginPermitted` | `IsLoginPermitted` | World |
| `IsAccountConnected` | `IsAccountConnected` | Login or World |
| `ConnectAccount` | `ConnectAccount` | World |
| `DisconnectAccount` | `DisconnectAccount` | Login or World |
| `PulseAccount` | `PulseAccount` | World |
| `ConnectCharacter` | `ConnectCharacter` | World |
| `DisconnectCharacter` | `DisconnectCharacter` | World |
| `RegisterWorldServer` | `RegisterWorldServer` | World |
| `UnregisterWorldServer` | `UnregisterWorldServer` | World |
| `RetrieveRegisteredWorldServers` | `ListWorldServers` | Login |

Every remaining `ICommunicationService` method has an explicit deferred or
transport-identity disposition in
`contracts/cluster/v1/communication-migration-map.json`. Nothing disappears
silently while the migration proceeds.

## Packet boundary

`ListWorldServers` returns typed world/channel records and the bounded character
count. It does not transport an `NsTeST` string. The Login adapter remains the
only component that renders the exact client packet, preserving:

- the double space after `NsTeST`;
- the fixed modern mode field;
- the four character-slot pairs;
- the 56 padding pairs;
- the stable SessionID position;
- the existing channel/group layout.

The internal transport therefore cannot accidentally become a second client
protocol.

## Security and correctness rules

- Every request carries the common versioned `RequestContext`.
- The requested service must be `Communication`.
- Caller roles are checked per RPC.
- Account, session and character IDs are positive where required.
- World IDs are canonical non-empty GUIDs.
- IP addresses are parsed and length-bounded.
- Endpoint ports, account limits and world-list sizes are bounded.
- The Gameforge reselection operation represents
  `preserve_session_registration` explicitly and requires a positive exact
  SessionID.
- Shared SCS authentication keys are replaced by transport identity, not copied
  into Protobuf.
- No generic `Invoke`, CLR object graph, raw packet string or untyped payload is
  accepted.

## Runtime sequence after this contract

1. Add a .NET 10 communication state host with bounded dispatch and mTLS role
   authorization.
2. Add dual-target `net481`/`net10.0` caller adapters.
3. Select exactly one `SCS` or `GRPC` communication transport before a call.
4. Keep SCS authoritative by default and provide coordinated rollback.
5. Prove the full real-client path: launcher, Login, channel list, World entry,
   repeated character reselection, disconnect and reconnect.
6. Expand the typed service to cross-server transfers, snapshots, statistics,
   administrative fan-out and communication callbacks.
7. Remove SCS only after every frozen legacy method has a proven typed
   replacement or an explicit retirement decision.
