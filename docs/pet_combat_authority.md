# Pet combat authority after the pathfinder migration

## Root cause

The pathfinder migration also introduced a server-side behavior tree for pets. The client already controls pet movement and combat through separate packet families:

- `ptctl` updates pet movement.
- `suctl` requests a normal pet attack.
- `u_pet` requests the pet's manual special skill.

The behavior-tree attack node later began selecting entries from `Mate.PSkills`. That gave the server AI and `u_pet` simultaneous ownership of special skills. A pet with a 30+ second special cooldown could therefore keep pursuing its target while normal attacks were repeatedly rejected or delayed behind special-skill state.

## Authority model

The repaired model assigns one owner to each action:

| Action | Owner | Cooldown state |
| --- | --- | --- |
| Smooth movement | Client `ptctl` | Client movement cadence |
| Automatic defence/basic attack | `PetAIProfile` and `MateAttackTargetNode` | `Mate.LastBasicSkillUse` |
| Client-requested basic attack | `SuctlPacketHandler` | `Mate.LastBasicSkillUse` |
| Manual pet special skill | `UpetPacketHandler` | Per-mate `NpcMonsterSkill.LastSkillUse` plus `petsr` UI cooldown |

`MateAttackTargetNode` must never inspect or schedule `NpcMonsterSkill` entries. `UpetPacketHandler` must resolve the exact `MateTransportId` and use the pet's cloned `PSkills` collection rather than the shared monster template.

## Diagnostic log

Debug logging identifies the action source without changing packet behavior:

```text
[MATE_COMBAT] Source=AI Action=Basic Mate=... TargetType=... Target=...
[MATE_COMBAT] Source=SUCTL Action=Basic Mate=... TargetType=... Target=...
[MATE_COMBAT] Source=UPET Action=Special Mate=... Skill=... TargetType=... Target=...
```

## Runtime acceptance test

Use a living active pet below the owner's level and a monster that survives for at least one minute.

1. Start combat and confirm the pet attacks and receives monster aggro.
2. Trigger the manual pet special skill.
3. While the special icon still shows approximately 30 seconds of cooldown, confirm basic attacks continue according to `Monster.BasicCooldown`.
4. Confirm the pet does not remain following the monster without attacking.
5. Kill several monsters and confirm pet experience percentage increases.
6. Keep the World debug log and verify that `UPET Action=Special` is followed by repeated `AI Action=Basic` or `SUCTL Action=Basic` entries before the special cooldown finishes.

Expected result: special-skill cooldown affects only the special skill. It never blocks basic attacks, tanking or pet experience progression.
