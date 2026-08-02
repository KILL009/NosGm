# Useful pet packet diagnostics

Capture these packet families during the runtime test:

- `sc_p`: pet roster, level, experience and displayed statistics.
- `pst 2`: pet HP and MP refresh.
- `st 2`: pet combat status and buffs.
- `su 2`: pet skill execution and damage animation.
- `cond 2`: pet attack and movement restrictions.

Also retain World log entries containing `MATE_AI_ERROR`.
