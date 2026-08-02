# Pet acceptance criteria

The fix is accepted only when all of the following are true:

- A hostile monster targeting the owner causes the active pet to attack.
- A pet attack creates threat for the pet.
- The monster can retain the pet as its target.
- The pet receives damage and sends updated HP packets.
- A living active pet gains experience after eligible monster kills.
- The pet levels only while its level is below the owner level.
- No `MATE_AI_ERROR` is produced during the test.
