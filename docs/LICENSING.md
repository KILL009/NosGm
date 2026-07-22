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
