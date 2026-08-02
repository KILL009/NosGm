# Case-linked GM sanctions

The `$Sanction` command is the supported moderation path for GM and ADMIN accounts. It requires an existing GM case, validates that the case belongs to the target, presents a preview and requires a short-lived confirmation code before changing any penalty.

## Database

Apply these migrations in order:

1. `Database/Migrations/20260720_GmCase.sql`
2. `Database/Migrations/20260720_GmSanctionAction.sql`

The second migration expands GM case note types and creates the append-only `dbo.GmSanctionAction` ledger.

## Commands

```text
$Sanction status
$Sanction preview <CaseId> <warning|mute|ban|ipban|unmute|unban> <duration> <Character> <reason>
$Sanction confirm <code>
$Sanction cancel
$Sanction recent <CaseId> [take]
```

Durations use minutes for mutes, days for bans and zero for warnings or reversals.

## Limits

- GM mute: 1 to 10,080 minutes.
- ADMIN or DEV mute: 1 to 525,600 minutes.
- GM ban: 1 to 30 days.
- ADMIN or DEV ban: 1 to 3,650 days.
- Permanent ban: duration `0`, ADMIN or DEV only. It is stored with a fifteen-year end date for compatibility with existing penalty checks.
- IP ban: ADMIN or DEV only, target must be online so the current address can be captured.

## Safety rules

- A staff account cannot sanction itself.
- The target must have lower authority than the actor.
- New sanctions require a case in Open, Investigating or Waiting state.
- Dismissed cases cannot authorize sanctions or reversals.
- The case account and optional character must match the target.
- A meaningful reason is mandatory.
- A preview expires after 120 seconds.
- Applying or reversing a sanction writes the penalty mutation, sanction ledger row and case note inside one SQL transaction.
- Reversals expire existing penalty rows; they never delete history.
- Repeated database execution with the same operation identifier returns the existing result.

## Legacy commands

For GM and ADMIN accounts, `$Ban`, `$Mute`, `$Warning`, `$Unban` and `$Unmute` redirect to `$Sanction`. DEV retains the old handlers as an emergency recovery path.
