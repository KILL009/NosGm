# Bazaar Audit Inspector

`$BazaarAudit` is a read-only GM investigation command. It never changes listings,
item instances, balances or ledger rows.

## Permission

The command requires `AuthorityType.GM` and the `Investigation` capability when a
restrictive staff-permission profile is enabled.

## Commands

```text
$BazaarAudit status
$BazaarAudit recent [take]
$BazaarAudit suspicious [take]
$BazaarAudit listing <BazaarItemId> [take]
$BazaarAudit character <CharacterId|Name> [take]
$BazaarAudit item <ItemInstanceId> [take]
```

`take` is clamped between 1 and 50.

## What it reads

The inspector combines these append-only operation tables when they are installed:

- `BazaarListingOperation`
- `BazaarPurchaseOperation`
- `BazaarPriceChangeOperation`
- `BazaarRecollectOperation`

The status command reports missing tables rather than failing the world server. History
is partial when one or more atomic bazaar migrations have not been installed.

## Anomaly checks

The suspicious view currently detects:

- active listings whose item or seller no longer exists;
- listings whose item owner or inventory type is incorrect;
- invalid prices, durations and remaining quantities;
- multiple listings referencing the same `ItemInstanceId`;
- orphaned items stored in `InventoryType.Bazaar`;
- arithmetic inconsistencies inside listing, purchase and recollection ledgers;
- self-purchase records;
- recollected listings that remain active;
- active listing quantities or prices that disagree with their atomic ledgers.

The command intentionally does not repair findings. A future repair workflow should use
an explicit preview and confirmation step, create an audit event and never delete history.
