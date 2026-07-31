# Legacy SCS callback observation ledger

## Purpose

Login and World now retain bounded local evidence for comparable callbacks
received through the legacy SCS transport. The evidence uses the same semantic
SHA-256 vocabulary as the typed gRPC shadow ledger, enabling a later parity
comparator without changing callback authority.

This ledger is observation-only:

- SCS still applies every gameplay, account and lifecycle effect;
- the typed callback handler still applies no gameplay effect;
- recording publishes no network traffic and acknowledges no callback;
- a fingerprint or ledger failure is logged and cannot interrupt SCS delivery.

## Observation window

The SCS ledger is coupled to the validated typed subscriber stream rather than
opened at process startup:

1. typed stream warmup opens a new SCS window with process identity and runtime
   generation;
2. callbacks received before the negotiated replay-complete barrier are marked
   `Warmup`;
3. accepting that barrier moves the active SCS window to `Live`;
4. stream termination closes recording but retains the last diagnostic
   snapshot;
5. a later validated stream starts a fresh window and clears evidence from the
   prior generation.

An active window cannot be replaced silently, replay completion is accepted
only once, and evidence for another runtime generation fails closed.

## Comparable callback inventory

The receiver records these eleven legacy methods as nine typed callback kinds:

| Legacy SCS method | Semantic callback kind |
| --- | --- |
| `CharacterConnected` | character presence: connected |
| `CharacterDisconnected` | character presence: disconnected |
| `KickSession` | kick session |
| `Restart` | lifecycle: restart |
| `Shutdown` | lifecycle: shutdown |
| `RunGlobalEvent` | global event |
| `UpdateBazaar` | Bazaar refresh |
| `UpdateFamily` | Family refresh |
| `UpdatePenaltyLog` | penalty refresh |
| `UpdateRelation` | relation refresh |
| `UpdateStaticBonus` | static-bonus refresh |

`SendMessageToCharacter` remains deliberately excluded. Its legacy DTO may
contain rendered packet text and routing behavior, so it needs a dedicated
typed messaging contract rather than a synthetic fingerprint.

## Semantic fingerprint

Each legacy argument builder constructs the same payload-only Protobuf envelope
used by the typed side and hashes its deterministic serialized bytes with
SHA-256. Event metadata, target metadata and transport timing are not included.

Optional Protobuf presence is preserved. For example, a missing kick account ID
does not hash like an explicitly supplied zero account ID.

The .NET 10 runtime self-test compares every legacy argument builder with the
equivalent typed payload. The compiled .NET Framework verification exercises
the production ledger through reflection after the full server build.

## Bounded evidence

Each observation contains:

- process identity;
- canonical runtime generation ID;
- monotonic local ordinal;
- typed callback kind;
- warmup or live phase;
- uppercase 64-character semantic SHA-256;
- local UTC observation timestamp.

The FIFO capacity defaults to 4,096 and cannot exceed 16,384. At capacity the
oldest entry is evicted. Window-local observed and eviction counters remain
available, and snapshots never expose the mutable queue.

Unknown callback kinds, malformed fingerprints, oversized identities,
duplicate replay barriers and overlapping windows fail closed.

## Validation

GitHub Actions validates both halves:

```powershell
dotnet run --project tests/NosGm.Authentication.Runtime.SelfTest/NosGm.Authentication.Runtime.SelfTest.csproj -c Release
./scripts/verify-scs-callback-observation-ledger-runtime.ps1
```

The second command expects the Release Master assemblies produced by the
Windows .NET Framework build.

## Next boundary

The next stage may add a bounded comparator that pairs live typed and SCS
observations by generation, callback kind, fingerprint and FIFO order. A parity
report must reject windows with unexplained evictions, missing events or
reordering.

No successful comparison alone authorizes transport cutover. SCS remains the
sole effect authority until a separate atomic inbound gate is designed, tested
and enabled explicitly.
