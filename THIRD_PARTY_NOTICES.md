# Third-party notices

NosGM includes or references third-party software. Each component remains subject to its own copyright and license terms.

This document records direct dependencies found during the compliance review. It must be updated whenever a dependency is added, removed or upgraded. Transitive dependencies must also be reviewed before distributing binaries.

## Source components

| Component | Relationship | License status |
|---|---|---|
| OpenNos | Upstream source and project lineage | Original reviewed headers state GPL-2.0-or-later |
| ChickenAPI.DAL | Source project derived from `Price-H16/NQ-Verde@2594ec13f4fba5d893b424197878c05f801f68a2` | Treat as GPL-3.0-only; exact project GUID and inspected AssemblyInfo blob match |
| ChickenAPI.Events | Source project derived from `Price-H16/NQ-Verde@2594ec13f4fba5d893b424197878c05f801f68a2` | Treat as GPL-3.0-only; project GUID and metadata match |
| ChickenAPI.Plugins | Source project derived from `Price-H16/NQ-Verde@2594ec13f4fba5d893b424197878c05f801f68a2` | Treat as GPL-3.0-only; exact project GUID and inspected AssemblyInfo blob match |
| BCardGistUpdater | `Tools/NosGM.DataUpdater` adapts parsing, resource setup and GitHub update concepts from `noszanou/BCardGistUpdater@53153c990ae5b65a603d223eeda504df2a67d5fb` | GPL-3.0-only; attribution preserved in `Tools/NosGM.DataUpdater/NOTICE.md` |
| NosGM | Current modifications and new work | Copyright remains with NosGM contributors; upstream rights are preserved |

The ChickenAPI evidence and matching `KILL009/NosGuana` snapshot are described in `Data/NosGm.ChickenAPI/NOTICE.md` and `docs/PROVENANCE.md`. The BCardGistUpdater adaptation is described in `Tools/NosGM.DataUpdater/NOTICE.md`. A complete GNU GPL version 3 license copy is bundled under `LICENSES/GPL-3.0-only/`.

## Direct NuGet dependencies observed

The following entries were observed in inspected project files. License expressions are taken from the corresponding package metadata where confirmed.

| Package | Observed version | License or review status |
|---|---:|---|
| FluentValidation | 11.8.1 | Apache-2.0 |
| Google.Cloud.Translation.V2 | 3.5.0 | Apache-2.0 |
| log4net | 3.3.2 | Apache-2.0 |
| MediatR | 12.2.0 | Apache-2.0 |
| MongoDB.Driver | 3.10.0 | Apache-2.0 |
| Newtonsoft.Json | 13.0.4 | MIT |
| System.Reactive | 6.1.0 | Verify and preserve package license text in release output |
| WindowsFirewallHelper | 2.2.0.86 | Package uses a linked license; verify the upstream license text before release |
| Autofac | versions vary by project | Verify package metadata for every resolved version |
| System.ValueTuple | versions vary by project | Verify package metadata for every resolved version |
| Za.NosGame.Fetcher | 1.0.21 | Used only by `Tools/NosGM.DataUpdater`; verify package metadata and preserve required notices before redistributing the tool |
| Za.NosGame.RessourceLoader | 1.0.21 | Used only by `Tools/NosGM.DataUpdater`; verify package metadata and preserve required notices before redistributing the tool |

Package references do not transfer ownership to NosGM. A release must include all notices and license texts required by the resolved packages.

## Compatibility note

OpenNos files reviewed during this audit permit GPL version 2 or later, while the verified ChickenAPI snapshot and the BCardGistUpdater-derived tool are treated as GPL-3.0-only. GPLv3 obligations apply to the corresponding covered components and any combined distribution governed by those terms.

## Release requirements

Before publishing any executable, DLL, ZIP, installer, container or prebuilt package:

1. Restore the exact dependency graph used for the build.
2. Export all direct and transitive package names and versions.
3. Confirm each package license from its package metadata and upstream source.
4. Review compatibility with the license governing the combined distribution.
5. Include required license and notice files in the distributed archive.
6. Record the exact NosGM source commit used to produce the binaries.
7. Provide the complete corresponding source under the applicable GPL terms.

## Proprietary assets

NosTale client files, artwork, maps, text, packets captured from proprietary services and other game assets are not licensed by this repository unless explicitly stated. Do not add proprietary client distributions, leaked server code or assets without a documented legal right to redistribute them.
