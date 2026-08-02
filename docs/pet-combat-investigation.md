# Pet combat, tanking and experience investigation

## Reported symptoms

- Active pet follows the character but does not attack.
- Monsters keep attacking the owner instead of the pet.
- The pet remains at level 1 and 0% experience.

## Root causes

### Behavior-tree and legacy target state were disconnected

The new mob AI stored its target only in the behavior-tree blackboard. Pet defence still queried the legacy `MapMonster.Target` property through `TargettedByMonstersList`. As a result, a monster could attack the owner while the pet saw no attacker.

### Monster target validation only accepted players

`HasTargetCondition` required `target.Character != null`. A pet is represented by `BattleEntity.Mate`, therefore a monster could not retain a pet as its target and the pet could not tank.

### Pet damage did not create pet threat

Pet attacks were executed, but the target monster was not explicitly given the pet as an aggro target. This allowed the owner to remain the preferred target.

### Experience forwarding already existed

`Character.GenerateXp` already forwards the calculated combat XP to every living active mate through `mate.GenerateXp(xp)`. The visible 0% was therefore downstream of the combat failure: a pet that never participates in working combat does not exercise the expected progression path.

## Implemented correction

- Keep `MapMonster.Target` synchronized with the AI blackboard.
- Validate any living attackable `BattleEntity`, including mates.
- Prefer valid entries from the monster aggro list before acquiring a new player target.
- Let pets detect monsters targeting the owner or active team mates.
- Add the pet itself to monster aggro and switch the monster target when the pet attacks.
- Add regression contracts for combat targeting, pet threat and mate XP forwarding.

## Runtime acceptance test

1. Summon one pet below the character's level.
2. Record its current level and experience percentage.
3. Let a hostile monster attack the character without manually ordering the pet.
4. Confirm that the pet attacks the monster.
5. Confirm that the monster can switch its attacks to the pet.
6. Kill several monsters with the pet active and alive.
7. Reopen the pet information window and confirm that experience increases.
8. Continue until the pet levels, provided its level remains below the owner's level.
