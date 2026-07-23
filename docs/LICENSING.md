# NosGM licensing and attribution policy

This policy applies to maintainers and contributors working on NosGM.

## Core rules

1. Preserve upstream copyright, author, license and warranty notices.
2. Never replace an upstream author's name with `NosGM Team` or another project name.
3. NosGM may claim copyright only over modifications and new work authored by its contributors.
4. Renaming a namespace, class, assembly, executable, product or database does not change authorship.
5. Modified inherited files must retain upstream attribution and should identify NosGM modifications and their date.
6. Do not import code without recording its immutable source revision and license in `docs/PROVENANCE.md`.
7. Do not add proprietary NosTale client files, leaked source, artwork, data or other assets unless redistribution rights are documented.
8. Do not impose extra restrictions on GPL-covered code, including bans on redistribution, modification or use by other servers.
9. Binary releases must include the complete corresponding source or equivalent GPL-compliant access to it.
10. Keep `AUTHORS.md`, `NOTICE.md` and `THIRD_PARTY_NOTICES.md` in release archives.

## Source file header for inherited GPL code

Use the following form when an inherited file lacks an adequate notice or when restoring a removed notice. Adapt years and project names only when supported by evidence.

```text
/*
 * This file is derived from the OpenNos Emulator Project.
 * See AUTHORS.md and NOTICE.md for attribution and provenance.
 *
 * Copyright (C) original authors and contributors
 * Modifications Copyright (C) 2026 NosGM contributors
 *
 * Modified by the NosGM project on YYYY-MM-DD.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms applicable to the original source of this file.
 *
 * This program is distributed without any warranty; without even the
 * implied warranty of merchantability or fitness for a particular purpose.
 * See LICENSE and NOTICE.md for details.
 */
```

Do not insert a specific SPDX identifier until the original license version for that component has been verified.

## Adapted external tools

Tools adapted from another repository must remain outside the server runtime unless there is a documented architectural reason to combine them.

Every adapted tool must include:

- upstream repository and immutable commit;
- original author and contributor credit;
- applicable SPDX license identifier in adapted source files;
- a local `NOTICE.md` describing copied concepts and NosGM modifications;
- configuration instead of credentials or third-party destinations embedded in code;
- automated checks preventing attribution removal.

`Tools/NosGM.DataUpdater` follows this policy for its adaptation of `noszanou/BCardGistUpdater@53153c990ae5b65a603d223eeda504df2a67d5fb`.

`Tools/NosGM.TimeSpaceParser` follows this policy for its adaptation of `noszanou/OpennosTimeSpaceParser@36bd96a51c4b4a1e55e8827ca7eb375cc189ad9e`, while preserving the upstream credits to Elendan, SEOVA and OpenNos XML-model contributors.

`Tools/NosGM.PacketCatalog` follows this policy for its GPL-3.0-only adaptation of packet-documentation concepts from `BlowaXD/SaltyEmu@2588cfdc64789a7952c781faaafdf1026ac73e9d`, including the packet-documentator introduced at `7f849171da82feee1b9fae851a45b3eef9a9cd68`, while preserving credit to Blowa and the recorded SaltyEmu contributors.

`Tools/NosGM.ClientEnhancements` follows this policy for its MIT-licensed adaptation of `ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a`, preserving Copyright (c) 2022 ApourtArtt and the reviewed references to DitzProject/ClientModdingAPI and ApourtArtt/DelphiClassInfo.

`Tools/NosGM.ResourceExplorer` follows this policy for its BSL-1.0 adaptation of `Pumba98/OnexExplorer@eaee2aa9f0e71b9960da586f425f79e628013021`, preserving credit to Pumba98, OnexExplorer contributors and their respective upstream contributors.

`Tools/NosGM.ClientThemeEditor` follows this policy for its MIT-licensed adaptation of `Elendan/Notale-Text-Picker@9eb44d2a0041b49375fabb730121a01acd7bae87`, preserving Copyright (c) 2019 Elendan and the upstream credits to Cryless and Fizo55. `Pumba98/Nostale-ClientColorizer@9d1e61c717b6a49ca221a5f2d855dfa5fa11591c` is recorded only as prior art because no reuse license was found; no source code, signature or offset is imported from it.

Packet source-analysis tooling has additional safeguards:

- parse source syntax without loading or executing server assemblies;
- never compile packet source as part of catalog generation;
- do not connect to clients, databases, brokers or game servers;
- do not modify packet source files;
- produce deterministic reports without timestamps;
- record diagnostics with stable codes and source locations;
- keep generated catalogs and artifacts outside the server runtime;
- use only synthetic, non-proprietary source fixtures in tests.

Client resource tooling has additional safeguards:

- source archives are opened read-only;
- parsing enforces count, offset, size, name and decompression limits;
- extraction is confined to an explicitly selected output directory;
- no source archive overwrite, repacking or patch application in the initial release;
- no proprietary archive, extracted resource or client asset in the repository;
- write support requires byte-identical round-trip tests against authorized samples before activation.

Client-side memory research has additional safeguards:

- no injector, automatic loader or server-side DLL deployment;
- exact executable version, architecture and SHA-256 validation before activation;
- no unverified-client override;
- no packet injection, movement, combat or farming automation;
- original-byte validation before every write;
- reversible patches and complete restoration on shutdown;
- no proprietary executable, memory dump, capture or asset in the repository;
- signatures and active features require tests for the exact supported client revision.

Client executable theme tooling has additional safeguards:

- no active signature profile is distributed until measured and tested against the exact authorized target executable;
- file name, PE architecture, file version, file length and SHA-256 must all match;
- every pattern declares an exact expected match count and cannot consist entirely of wildcards;
- every write verifies the expected original bytes and rejects overlapping ranges;
- the default mode writes a separate copy rather than replacing the input;
- in-place mode creates and hashes a backup before temporary-file replacement;
- restoration requires both the current patched hash and backup hash to match the manifest;
- no administrator requirement, process-memory access, injector, packet behavior or gameplay automation;
- no proprietary executable, modified client, memory dump or client asset is committed;
- unlicensed prior art may inform feature names only, never copied implementation, signature or offset data.

## New files written entirely for NosGM

For a new file containing only original NosGM work, use:

```text
/*
 * Copyright (C) 2026 NosGM contributors
 *
 * This file is part of NosGM. See LICENSE, AUTHORS.md and NOTICE.md.
 */
```

If the new file incorporates copied or adapted code, it is not entirely original and must preserve the upstream notices instead.

## Assembly metadata

Assembly metadata may use the NosGM product name, but historical copyright dates must not be reassigned.

Preferred form:

```csharp
[assembly: AssemblyCompany("NosGM contributors")]
[assembly: AssemblyCopyright(
    "Portions Copyright © OpenNos and other upstream contributors; " +
    "modifications Copyright © 2026 NosGM contributors")]
```

## Pull request checklist

A pull request importing or adapting external code must answer:

- Where did the code come from?
- What exact commit, tag or archive checksum was used?
- What license applies?
- Were all original notices preserved?
- Which files were modified?
- Is the license compatible with the rest of the distributed build?
- Does `THIRD_PARTY_NOTICES.md` need an update?

A missing answer blocks merge until provenance is established.
