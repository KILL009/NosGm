# NosGM.ResourceExplorer attribution notice

`NosGM.ResourceExplorer` is an optional read-only maintenance tool. It is not part of the NosGM login, master or world-server runtime.

## Upstream source

- Repository: `Pumba98/OnexExplorer`
- Reviewed commit: `eaee2aa9f0e71b9960da586f425f79e628013021`
- Reviewed version: `v0.7.1`
- License: Boost Software License 1.0

The reviewed application identifies itself as a fork maintained by Pumba98. The inspected snapshot does not provide an immutable identifier for the earlier source of that fork, so NosGM preserves credit to Pumba98, OnexExplorer contributors and their respective upstream contributors without inventing a revision.

## Adapted portions

The NosGM tool adapts documented archive layouts, compressed-entry handling and DAT/LST text decoding behavior. The Qt user interface, image editor, model editor, patch writer and repacking code were not copied.

## NosGM safety changes

- package-free .NET 10 command-line implementation;
- read-only access to source archives;
- strict bounds for counts, offsets, sizes, names and decompression;
- SHA-256 for source files and decoded entries;
- extraction paths confined to a selected output directory;
- no archive overwrite, patching, repacking or client modification;
- deterministic reports without timestamps;
- synthetic regression fixtures only.

Modifications Copyright (c) 2026 NosGM contributors.
