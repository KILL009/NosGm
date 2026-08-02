# Pet fix summary

This branch repairs the demonstrated combat chain:

1. Mob AI mirrors its blackboard target to `MapMonster.Target`.
2. Mob AI accepts living mates as valid targets.
3. Existing aggro entries are considered before acquiring a new player.
4. Pet AI can detect monsters attacking the owner or active mates.
5. A pet attack adds pet threat and can redirect the monster onto the pet.
6. Existing character XP logic forwards combat XP to living active mates.

The branch intentionally does not guess a new `sc_p` packet layout without a current official capture.
