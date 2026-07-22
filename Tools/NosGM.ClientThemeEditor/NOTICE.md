# NosGM.ClientThemeEditor notice

## MIT-licensed upstream

The GM-tag and right-click color-editing workflow is adapted from:

- Project: `Elendan/Notale-Text-Picker`
- Reviewed commit: `9eb44d2a0041b49375fabb730121a01acd7bae87`
- Copyright: Copyright (c) 2019 Elendan
- License: MIT License

The upstream README credits Cryless for publishing a GM-tag color method and Fizo55 for an earlier console tool. Those credits are preserved here.

NosGM modifications include:

- migration to a package-free .NET 9 command-line tool;
- exact PE architecture, version, size and SHA-256 gates;
- strict JSON profiles and themes;
- bounded wildcard pattern matching;
- exact match-count enforcement;
- expected-original-byte verification;
- overlap detection;
- copy-first output mode;
- verified backup, manifest and restore flow for guarded in-place mode;
- temporary-file writes with atomic replacement;
- deterministic synthetic regression tests;
- removal of administrator requirements and unrestricted direct executable replacement.

## Independently implemented feature categories

`Pumba98/Nostale-ClientColorizer` at commit
`9d1e61c717b6a49ca221a5f2d855dfa5fa11591c`
was reviewed as prior art for object-label and weapon-glow color categories.

No license granting reuse was found in the reviewed repository tree. Therefore:

- no ClientColorizer source code is copied;
- no ClientColorizer pattern, signature or offset is distributed;
- no authorship over that work is claimed;
- future implementations must use independently measured signatures for an authorized exact client.

## NosGM modifications

Modifications Copyright (c) 2026 NosGM contributors.

NosTale executables, assets, names and trademarks belong to their respective rights holders. This repository does not distribute a client executable or modified proprietary binary.
