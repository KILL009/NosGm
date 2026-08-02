# Pet combat, tanking and experience investigation

## Reported symptoms

- Active pet follows the character but does not attack.
- Monsters keep attacking the owner instead of the pet.
- The pet remains at level 1 and 0% experience.

## Confirmed combat root causes

### Behavior-tree and legacy target state were disconnected

The new mob AI stored its target only in the behavior-tree blackboard. Pet defence still queried the legacy `MapMonster.Target` property through `TargettedByMonstersList`. As a result, a monster could attack the owner while the pet saw no attacker.

### Monster target validation only accepted players

`HasTargetCondition` required `target.Character != null`. A pet is represented by `BattleEntity.Mate`, therefore a monster could not retain a pet as its target and the pet could not tank.

### Pet damage did not create pet threat

Pet attacks were executed, but the target monster was not explicitly given the pet as an aggro target. This allowed the owner to remain the preferred target.

## Experience findings

`Character.GenerateXp` already forwards calculated combat XP to every living active mate through `mate.GenerateXp(xp)`. Therefore the 0% display is not explained solely by the missing forwarding call.

Runtime validation must distinguish between two cases:

1. The internal `Experience` value remains unchanged. Check whether the kill produced eligible character XP, whether the pet is alive and an active team member, and whether the pet is below the owner level.
2. The internal `Experience` value increases but the client still displays 0%. Capture and compare the `sc_p` packet because its current field layout has not yet been verified against the same official client version.

## Implemented correction

- Keep `MapMonster.Target` synchronized with the AI blackboard.
- Validate any living attackable `BattleEntity`, including mates.
- Prefer valid entries from the monster aggro list before acquiring a new player target.
- Let pets detect monsters targeting the owner or active team mates.
- Add the pet itself to monster aggro and switch the monster target when the pet attacks.
- Add regression contracts for combat targeting, pet threat and the existing mate XP forwarding path.

## Runtime acceptance test

1. Summon one living pet whose level is below the character level.
2. Record the pet level, percentage and the `Experience` field from `sc_p`.
3. Let a hostile monster attack the character without repeatedly ordering the pet.
4. Confirm that the pet attacks the monster.
5. Confirm that the monster can switch its attacks to the pet and reduce its HP.
6. Kill an eligible monster that awards non-zero character XP.
7. Capture `sc_p` again and compare the raw `Experience` field.
8. Reopen the pet information window and confirm that the percentage increases.
9. Continue until the pet levels, while its level remains below the owner level.
