# Item traceability foundation

This slice adds the append-only audit ledger that will support anti-duplication checks, GM item history, safe compensation and selective rollback.

## Database installation

Run `Database/Migrations/20260720_ItemTrace.sql` once against the Frostvein database before enabling callers that write audit events. The script is idempotent and may be executed again safely.

The table deliberately has no foreign key to `ItemInstance`. Audit history must remain available after an item is consumed or deleted.

## Recording one operation

Create one operation identifier for the complete business transaction and increment the sequence for every affected item:

```csharp
Guid operationId = ItemTraceService.Instance.BeginOperation();

ItemTraceService.Instance.Record(
    operationId,
    sequence: 0,
    action: ItemTraceAction.Transferred,
    source: ItemTraceSource.Trade,
    before: previousItem,
    after: updatedItem,
    actorAccountId: accountId,
    actorCharacterId: characterId,
    actorName: characterName,
    reason: "Player trade");
```

`OperationId + Sequence` is unique. Retrying the same operation returns the existing event instead of inserting a duplicate.

## Stored evidence

Each event records:

- item instance and equipment serial identifiers;
- item VNum;
- amount, owner, inventory and slot before and after;
- action and source subsystem;
- actor account/character/name;
- UTC timestamp, reason and bounded metadata;
- suspicious-state marker.

## Rollout order

1. Apply the SQL migration.
2. Add read-only `$ItemTrace` and duplicate-serial diagnostics.
3. Instrument mail and rewards.
4. Instrument trade and bazaar inside their database transactions.
5. Instrument drops, crafting, upgrades and GM compensation.
6. Add quarantine rules only after sufficient telemetry exists.

No automatic item mutation hooks are enabled in this foundation commit. That is intentional: every subsystem will be connected explicitly so the trace source and operation boundary remain accurate.
