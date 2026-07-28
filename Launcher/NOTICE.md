# NosGM Launcher notice

## Original NosGM work

The launcher, updater core, manifest builder, WPF shell and synthetic tests in this directory were written for NosGM in 2026 and are distributed under the MIT License included beside this notice.

## NosCore.DeveloperTools MIT adaptation

The Steam-compatible modern authentication path adapts narrowly scoped code and binary-layout knowledge from:

- Project: `NosCoreIO/NosCore.DeveloperTools`
- Reviewed revision: `39e2cd2085ff7fc7250966d58893a95262157113`
- Copyright: Copyright (c) 2026 NosCoreIO
- License: MIT License

NosGM adapts only the Delphi Login-address discovery, the equal-length `gf_wrapper.dll` import rewrite, and the NativeAOT x86 `Steam_*` authentication-stub contract. NosGM adds transactional output, ambiguity rejection, one resolved InstallationId, no credential logging, no remote download, and a synthetic PE/patch regression test.

The NosCore packet logger, process injection, packet injection, NosMall tooling, HTTP authentication client, endpoints, binaries and user-interface code are not included. The complete MIT permission notice for the adapted component is preserved in the repository-level `THIRD_PARTY_NOTICES.md` that ships with launcher packages.

## Prior-art review

The update-by-file and repair workflow was independently designed after reviewing:

- Project: `Mati18505/HexTaleLauncher`
- Reviewed revision: `50aa50580aa35a45b156a1899a340a25e50f7fb5`
- Primary author: Mati18505

The reviewed repository's frontend package metadata declared MIT, but no repository-wide license covering all backend sources was found during review. Therefore no HexTaleLauncher source code, binary, artwork, endpoint, signing material or byte-for-byte implementation is copied here.

NosGM independently replaces the reviewed MD5/direct-write design with:

- canonical signed manifests;
- ECDSA P-256 / SHA-256 release signatures;
- SHA-256 file identities;
- strict path confinement and reparse-point rejection;
- streaming downloads with size limits;
- staging before activation;
- rollback storage and local managed-file state;
- deletion limited to previously managed files;
- non-elevated client launch;
- deterministic synthetic regression tests.

No authorship is claimed over HexTaleLauncher. Credit for its prior work remains with Mati18505 and its contributors.

NosTale executables, data, artwork, names and trademarks belong to their respective rights holders. This project does not distribute proprietary client material.
