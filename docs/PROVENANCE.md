# NosGM source provenance register

This register tracks where the code in NosGM came from and which license terms apply. It is a compliance record, not a marketing document.

A component marked **unresolved** must be investigated before a public binary release.

## Repository import history

- `e4fa098aaf3b9df7ddde72ba880823a6633dfde1` created the NosGM repository with only the initial `.gitignore`.
- `22238e8225e82b22f4bd73effb5ba308a352533c` is the first large NosGM commit in which the inherited source tree appears.
- `4186b11909d036633c8727898bba16fcb53f395c` identifies a matching intermediate snapshot in the private `KILL009/NosGuana` repository.
- `2594ec13f4fba5d893b424197878c05f801f68a2` identifies the verified public NQ-Source snapshot in `Price-H16/NQ-Verde` for the included ChickenAPI projects.

The NosGM source import commit identifies the snapshot brought into this repository. ChickenAPI lineage is now verified. The exact Frostvein upstream repository and revision remain unresolved.

## Component register

| Component | Upstream location | Revision imported | License evidence | Status |
|---|---|---|---|---|
| OpenNos | `https://github.com/OpenNos/OpenNos` | Present in NosGM import commit `22238e8225e82b22f4bd73effb5ba308a352533c`; exact upstream base pending | Upstream `LICENSE` contains GPL v2; reviewed file headers state GPL v2 or later | Partially verified |
| Frostvein lineage | Exact upstream repository must be identified | Present in NosGM import commit `22238e8225e82b22f4bd73effb5ba308a352533c`; exact upstream revision pending | Historical source identity and Git history; original license file and notices must be recovered | **Unresolved** |
| ChickenAPI.DAL | `https://github.com/Price-H16/NQ-Verde` | Public snapshot `2594ec13f4fba5d893b424197878c05f801f68a2`; matching intermediate snapshot `KILL009/NosGuana@4186b11909d036633c8727898bba16fcb53f395c` | Project GUID and inspected AssemblyInfo blob match; upstream snapshot contains GNU GPL version 3 | Verified, treat as GPL-3.0-only |
| ChickenAPI.Events | `https://github.com/Price-H16/NQ-Verde` | Public snapshot `2594ec13f4fba5d893b424197878c05f801f68a2`; matching intermediate snapshot `KILL009/NosGuana@4186b11909d036633c8727898bba16fcb53f395c` | Project GUID and assembly metadata match; inspected difference is source-order only; upstream snapshot contains GNU GPL version 3 | Verified, treat as GPL-3.0-only |
| ChickenAPI.Plugins | `https://github.com/Price-H16/NQ-Verde` | Public snapshot `2594ec13f4fba5d893b424197878c05f801f68a2`; matching intermediate snapshot `KILL009/NosGuana@4186b11909d036633c8727898bba16fcb53f395c` | Project GUID and inspected AssemblyInfo blob match; upstream snapshot contains GNU GPL version 3 | Verified, treat as GPL-3.0-only |
| NosGM modifications | `https://github.com/KILL009/NosGm` | Git history after the source import | Copyright held by the respective NosGM contributors | Verified by repository history |

## License structure

NosGM is not accurately described by one historical license file alone:

- the root `LICENSE` preserves the GNU GPL version 2 text inherited with the OpenNos-derived source;
- reviewed OpenNos file notices permit GPL version 2 or any later version;
- the ChickenAPI-derived source is conservatively treated as GPL-3.0-only based on its verified NQ-Source snapshot;
- a complete GPL version 3 license copy is bundled under `LICENSES/GPL-3.0-only/`;
- exact Frostvein licensing still has to be recovered.

If the GPL-3.0-only ChickenAPI source remains linked into and distributed as part of the combined application, the combined distribution must satisfy GPL version 3. This conclusion remains subject to resolving the Frostvein lineage and confirming that no GPL-2.0-only material is present.

## Evidence already preserved

- The root `LICENSE` file contains the GNU General Public License version 2 text.
- Original OpenNos source files reviewed during the audit contain notices referring to the OpenNos `AUTHORS` file and GPL version 2 or later.
- The original OpenNos `AUTHORS.md` contributor list is preserved in the NosGM `AUTHORS.md` file.
- The current solution includes OpenNos-derived project GUIDs, database entities and architecture.
- ChickenAPI project GUIDs and inspected assembly metadata match the verified NQ-Source snapshot.
- `Data/NosGm.ChickenAPI/NOTICE.md` records the ChickenAPI evidence and license treatment.
- File-specific `.license` sidecars restore attribution for indexed inherited files whose project header was renamed during the NosGM identity migration.

## Required investigation

For every unresolved component:

1. Locate the exact public or privately received upstream repository or archive.
2. Record its immutable commit SHA, tag or archive checksum.
3. Preserve its original `LICENSE`, `COPYING`, `NOTICE`, `AUTHORS` and source headers.
4. Compare the imported tree with the current NosGM tree.
5. Identify files copied, modified or removed.
6. Record the applicable license expression, including whether it is `only` or `or later`.
7. Obtain written permission from the copyright holders when the license cannot be determined or is incompatible.
8. Remove or replace code that cannot be lawfully redistributed.

## Release gate

Do not publish new prebuilt NosGM binaries while the Frostvein source component required by the distributed build remains unresolved.

Running the software privately does not complete this provenance record. Distribution includes sending binaries or source archives through Discord, forums, cloud drives, release pages or private messages.

## Updating this file

Every import from another emulator, packet library, snippet, gist or repository must add an entry containing:

- component and file names;
- upstream URL;
- immutable revision;
- copyright holder or contributor information;
- license expression;
- modification date;
- person who performed the import.
