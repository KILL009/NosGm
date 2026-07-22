# NosGM source provenance register

This register tracks where the code in NosGM came from and which license terms apply. It is a compliance record, not a marketing document.

A component marked **unresolved** must be investigated before a public binary release.

## Component register

| Component | Upstream location | Revision imported | License evidence | Status |
|---|---|---|---|---|
| OpenNos | `https://github.com/OpenNos/OpenNos` | Exact base revision to be identified from Git history | Upstream `LICENSE` contains GPL v2; reviewed file headers state GPL v2 or later | Partially verified |
| Frostvein lineage | Exact upstream repository must be identified | Pending | Historical source identity and Git history; original license file and notices must be recovered | **Unresolved** |
| ChickenAPI.DAL | Exact upstream repository must be identified | Pending | Included as source project; upstream license and revision must be confirmed | **Unresolved** |
| ChickenAPI.Events | Exact upstream repository must be identified | Pending | Included as source project; upstream license and revision must be confirmed | **Unresolved** |
| ChickenAPI.Plugins | Exact upstream repository must be identified | Pending | Included as source project; upstream license and revision must be confirmed | **Unresolved** |
| NosGM modifications | `https://github.com/KILL009/NosGm` | Git history | Copyright held by the respective NosGM contributors | Verified by repository history |

## Evidence already preserved

- The root `LICENSE` file contains the GNU General Public License version 2 text.
- Original OpenNos source files reviewed during the audit contain notices referring to the OpenNos `AUTHORS` file and GPL version 2 or later.
- The original OpenNos `AUTHORS.md` contributor list is preserved in the NosGM `AUTHORS.md` file.
- The current solution includes OpenNos-derived project GUIDs, database entities and architecture, as well as ChickenAPI-named source projects.

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

Do not publish new prebuilt NosGM binaries while any source component required by the distributed build remains unresolved.

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
