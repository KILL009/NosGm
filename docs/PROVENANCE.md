# NosGM source provenance register

This register tracks where the code in NosGM came from and which license terms apply. It is a compliance record, not a marketing document.

## Repository import history

- `e4fa098aaf3b9df7ddde72ba880823a6633dfde1` created the NosGM repository with only the initial `.gitignore`.
- `22238e8225e82b22f4bd73effb5ba308a352533c` is the first large NosGM commit in which the OpenNos-derived source tree appears.
- `4186b11909d036633c8727898bba16fcb53f395c` identifies a matching snapshot in the private `KILL009/NosGuana` repository for the included ChickenAPI projects.
- `2594ec13f4fba5d893b424197878c05f801f68a2` identifies the verified public NQ-Source snapshot in `Price-H16/NQ-Verde` for those ChickenAPI projects.

NosGM is an OpenNos-derived emulator. The exact OpenNos base revision can continue to be narrowed through file and commit comparison, but no separate emulator lineage is claimed.

## Component register

| Component | Upstream location | Revision imported | License evidence | Status |
|---|---|---|---|---|
| OpenNos | `https://github.com/OpenNos/OpenNos` | Present in NosGM import commit `22238e8225e82b22f4bd73effb5ba308a352533c`; exact upstream base revision still being matched | Upstream `LICENSE` contains GPL v2; reviewed file headers state GPL v2 or later | Verified project lineage; exact base revision pending |
| ChickenAPI.DAL | `https://github.com/Price-H16/NQ-Verde` | Public snapshot `2594ec13f4fba5d893b424197878c05f801f68a2`; matching snapshot `KILL009/NosGuana@4186b11909d036633c8727898bba16fcb53f395c` | Project GUID and inspected AssemblyInfo blob match; upstream snapshot contains GNU GPL version 3 | Verified, treat as GPL-3.0-only |
| ChickenAPI.Events | `https://github.com/Price-H16/NQ-Verde` | Public snapshot `2594ec13f4fba5d893b424197878c05f801f68a2`; matching snapshot `KILL009/NosGuana@4186b11909d036633c8727898bba16fcb53f395c` | Project GUID and assembly metadata match; inspected difference is source-order only; upstream snapshot contains GNU GPL version 3 | Verified, treat as GPL-3.0-only |
| ChickenAPI.Plugins | `https://github.com/Price-H16/NQ-Verde` | Public snapshot `2594ec13f4fba5d893b424197878c05f801f68a2`; matching snapshot `KILL009/NosGuana@4186b11909d036633c8727898bba16fcb53f395c` | Project GUID and inspected AssemblyInfo blob match; upstream snapshot contains GNU GPL version 3 | Verified, treat as GPL-3.0-only |
| NosGM modifications | `https://github.com/KILL009/NosGm` | Git history after the source import | Copyright held by the respective NosGM contributors | Verified by repository history |

## License structure

NosGM contains these verified license lineages:

- the root `LICENSE` preserves the GNU GPL version 2 text inherited with the OpenNos-derived source;
- reviewed OpenNos file notices permit GPL version 2 or any later version;
- the ChickenAPI-derived source is conservatively treated as GPL-3.0-only based on its verified NQ-Source snapshot;
- a complete GPL version 3 license copy is bundled under `LICENSES/GPL-3.0-only/`.

If the GPL-3.0-only ChickenAPI source remains linked into and distributed as part of the combined application, the combined distribution must satisfy GPL version 3.

## Evidence already preserved

- The root `LICENSE` file contains the GNU General Public License version 2 text.
- Original OpenNos source files reviewed during the audit contain notices referring to the OpenNos `AUTHORS` file and GPL version 2 or later.
- The original OpenNos `AUTHORS.md` contributor list is preserved in the NosGM `AUTHORS.md` file.
- The current solution includes OpenNos-derived project GUIDs, database entities and architecture.
- ChickenAPI project GUIDs and inspected assembly metadata match the verified NQ-Source snapshot.
- `Data/NosGm.ChickenAPI/NOTICE.md` records the ChickenAPI evidence and license treatment.
- File-specific `.license` sidecars restore attribution for inherited OpenNos files whose project header was renamed during the NosGM identity migration.

## Continuing provenance work

To identify the exact OpenNos base revision more precisely:

1. Compare the first NosGM source import against immutable OpenNos commits.
2. Record the closest matching OpenNos commit or tag.
3. Preserve original `LICENSE`, `NOTICE`, `AUTHORS` and file headers.
4. Record later NosGM modifications separately from inherited OpenNos code.

This refinement improves historical precision but does not change the documented fact that NosGM is derived from OpenNos.

## Release requirements

Binary distributions must include the applicable license notices and complete corresponding source for the exact build being distributed.

## Updating this file

Every future import from another library, snippet, gist or repository must add an entry containing:

- component and file names;
- upstream URL;
- immutable revision;
- copyright holder or contributor information;
- license expression;
- modification date;
- person who performed the import.
