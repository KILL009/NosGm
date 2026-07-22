# Legacy color-tool research

This document records historical behavior for provenance and review. It is not an active patch profile.

## Elendan/Notale-Text-Picker

Reviewed commit:

```text
9eb44d2a0041b49375fabb730121a01acd7bae87
```

The 2019 tool searched the selected executable for a fixed prefix followed by the previously configured color and replaced the color bytes. It created timestamped backups, but did not bind changes to an exact executable hash, version or unique signature count.

NosGM.ClientThemeEditor does not ship those historical values as an executable profile. They must not be assumed compatible with client `0.9.3.3255`.

## Pumba98/Nostale-ClientColorizer

Reviewed commit:

```text
9d1e61c717b6a49ca221a5f2d855dfa5fa11591c
```

The repository demonstrated object-label and weapon-glow color categories. No reuse license was found in the reviewed tree, so the NosGM implementation does not copy its code, byte signatures or offsets.

Future exact-client research should record:

- executable SHA-256 and file version;
- architecture and file length;
- signature derivation evidence;
- exact expected match count;
- original bytes;
- replacement encoding;
- regression results;
- rollback verification.
