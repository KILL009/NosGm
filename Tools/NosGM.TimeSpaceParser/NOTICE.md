# NosGM.TimeSpaceParser attribution notice

`NosGM.TimeSpaceParser` is an external maintenance tool for the NosGM project. It is not part of the NosGM game-server runtime.

## Upstream lineage

The NosGM adaptation is derived from and substantially inspired by:

- Project: `noszanou/OpennosTimeSpaceParser`
- Reviewed upstream commit: `36bd96a51c4b4a1e55e8827ca7eb375cc189ad9e`
- Adapter and maintainer: `noszanou`
- Earlier project: `Elendan/TimeSpace-Generator`
- Earlier contributor identified by the upstream project: `SEOVA`
- XML contract lineage: OpenNos XML model, including the source published in `MasterDomino/OfficialOpenNos`
- Upstream license: GNU General Public License version 3

The intermediate repository does not identify an immutable Elendan base commit or the exact SEOVA snapshot. NosGM therefore records those contributors and repositories without inventing a more precise historical claim.

## NosGM modifications

NosGM contributors added or changed:

- a package-free .NET 10 command-line application;
- explicit `parse`, `batch`, `validate` and `self-test` commands;
- configurable input and output paths;
- deterministic XML and reports without generation timestamps;
- source SHA-256 reporting;
- structural validation before and after XML creation;
- strict mode and overwrite protection;
- destination-map resolution using captured `gp` and `at` identifiers before positional fallback;
- correction of the end-portal type-5 control-flow case;
- removal of arbitrary default gold and reputation rewards;
- warnings for unsupported lines and inferred values;
- synthetic, non-proprietary regression data;
- CI, documentation, safety and attribution checks;
- separation from the NosGM server Core and solution.

Copyright in the original work remains with Elendan, SEOVA, noszanou, OpenNos contributors and other upstream contributors for the portions they authored. Copyright in later modifications remains with the respective NosGM contributors.

## License

This tool and its adapted source files are distributed under `GPL-3.0-only`. The repository copy of the complete GNU GPL version 3 text is stored under `LICENSES/GPL-3.0-only/`.

Generated XML may contain factual data derived from a packet capture and may be affected by rights in the source game data. This tool grants no permission to redistribute proprietary NosTale client files, server files, artwork, text or captures.
