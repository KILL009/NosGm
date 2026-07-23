# NosGM

NosGM is an open-source NosTale server emulator derived from OpenNos.

The project is under active development. Current work includes login, world and channel communication, client and packet compatibility, character and session stability, performance, memory usage, timers, bug fixing, localization and documentation.

> Current target client: **0.9.3.3255**

## Project status

NosGM is experimental software. Systems may be incomplete, unstable or incompatible with some clients. Do not use the project without understanding the security, data-loss and operational risks of running an unfinished server emulator.

## Build requirements

The solution currently targets the .NET Framework and contains legacy-style Visual Studio projects. Use the solution and project files in the repository as the authoritative build configuration.

Before sharing a build, record the exact source commit and preserve the corresponding source code used to produce it.

## Data maintenance tooling

`Tools/NosGM.DataUpdater` is an external .NET 9 utility that downloads or reads NosTale resources, extracts `BCard.dat`, generates multilingual BCard catalogs, reports changes and opens a pull request only when the data changed.

It is intentionally separated from the login, master and world-server runtime. The tool is adapted from `noszanou/BCardGistUpdater` commit `53153c990ae5b65a603d223eeda504df2a67d5fb` and remains under GPL-3.0-only. See its [README](Tools/NosGM.DataUpdater/README.md) and [attribution notice](Tools/NosGM.DataUpdater/NOTICE.md).

Generated catalogs belong under `Data/Generated/BCards`. Proprietary client archives and extracted binary assets must not be committed.

## Time-Space tooling

`Tools/NosGM.TimeSpaceParser` is an external .NET 9 command-line tool that converts operator-supplied Time-Space packet captures into deterministic XML and validation reports for the current NosGM scripted-instance model.

It is adapted from `noszanou/OpennosTimeSpaceParser@36bd96a51c4b4a1e55e8827ca7eb375cc189ad9e`, remains under GPL-3.0-only and preserves the recorded credits to Elendan, SEOVA and OpenNos XML-model contributors. See its [README](Tools/NosGM.TimeSpaceParser/README.md) and [notice](Tools/NosGM.TimeSpaceParser/NOTICE.md).

## Resource exploration tooling

`Tools/NosGM.ResourceExplorer` is an external, package-free .NET 9 command-line tool for read-only inspection, hashing, comparison and sandboxed extraction of supported `.NOS` resource archives.

The first release supports reviewed compressed archive and DAT/LST text-container layouts, but deliberately excludes repacking, patching and source-archive overwrite. It is adapted from `Pumba98/OnexExplorer@eaee2aa9f0e71b9960da586f425f79e628013021` under the Boost Software License 1.0. See its [README](Tools/NosGM.ResourceExplorer/README.md) and [notice](Tools/NosGM.ResourceExplorer/NOTICE.md).

## Packet catalog tooling

`Tools/NosGM.PacketCatalog` is an external .NET 9 source-analysis tool that generates deterministic JSON and Markdown documentation for packet definitions, indexed fields, typed handlers and legacy raw-header handlers without loading or executing server assemblies.

It validates duplicate headers, duplicate indexes, unreachable `SerializeToEnd` fields and handler registration conflicts. The design adapts packet-documentation concepts from `BlowaXD/SaltyEmu@2588cfdc64789a7952c781faaafdf1026ac73e9d`, including the earlier packet-documentator work at `7f849171da82feee1b9fae851a45b3eef9a9cd68`, and remains GPL-3.0-only. See its [README](Tools/NosGM.PacketCatalog/README.md) and [notice](Tools/NosGM.PacketCatalog/NOTICE.md).

## Client enhancement research

`Tools/NosGM.ClientEnhancements` is an optional x86 client-compatibility foundation kept outside the server solution. Its first release provides an exact client identity probe, strict profile validation, safe pattern parsing, memory-range checks and reversible patch infrastructure. It contains no injector, packet injection or active gameplay modification.

The component is adapted from concepts in `ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a`, Copyright (c) 2022 ApourtArtt, under the MIT License. Signature-dependent features remain disabled until the exact target executable is validated. See its [README](Tools/NosGM.ClientEnhancements/README.md) and [notice](Tools/NosGM.ClientEnhancements/NOTICE.md).

## Client theme tooling

`Tools/NosGM.ClientThemeEditor` is a standalone, package-free .NET 9 tool for planning and applying color-only themes to an authorized exact x86 client executable. It validates file name, architecture, version, length, SHA-256, signature count and original bytes before writing.

The default mode creates a separate copy. Guarded in-place mode creates a hash-verified backup, writes through a temporary file, records a restoration manifest and refuses restoration when either file has changed unexpectedly. The first release ships without active signatures for client `0.9.3.3255`.

The GM-tag and right-click workflow is adapted from `Elendan/Notale-Text-Picker@9eb44d2a0041b49375fabb730121a01acd7bae87` under the MIT License, preserving credit to Elendan, Cryless and Fizo55. `Pumba98/Nostale-ClientColorizer@9d1e61c717b6a49ca221a5f2d855dfa5fa11591c` is recorded only as independently reviewed prior art for object-label and weapon-glow categories; no code, signatures or offsets are copied because no reuse license was found. See the tool [README](Tools/NosGM.ClientThemeEditor/README.md) and [notice](Tools/NosGM.ClientThemeEditor/NOTICE.md).

## Contributing

Developers, testers, packet researchers, bug hunters, technical writers and experienced NosTale players are welcome.

Contributors must read:

- [Licensing and attribution policy](docs/LICENSING.md)
- [Source provenance register](docs/PROVENANCE.md)
- [Authors and contributors](AUTHORS.md)
- [Copyright and provenance notice](NOTICE.md)
- [Third-party notices](THIRD_PARTY_NOTICES.md)

When importing or adapting external code, include the upstream URL, immutable commit or archive checksum, applicable license and preserved copyright notices.

## License and attribution

NosGM contains code from multiple verified license lineages:

- the root [LICENSE](LICENSE) preserves the GNU General Public License version 2 text inherited with the OpenNos-derived source;
- reviewed original OpenNos file notices permit GNU GPL version 2 or, at the recipient's option, any later version;
- the included ChickenAPI source is derived from the verified `Price-H16/NQ-Verde` snapshot `2594ec13f4fba5d893b424197878c05f801f68a2` and is conservatively treated as GPL-3.0-only;
- `Tools/NosGM.DataUpdater` is adapted from `noszanou/BCardGistUpdater@53153c990ae5b65a603d223eeda504df2a67d5fb` and is GPL-3.0-only;
- `Tools/NosGM.TimeSpaceParser` is adapted from `noszanou/OpennosTimeSpaceParser@36bd96a51c4b4a1e55e8827ca7eb375cc189ad9e` and is GPL-3.0-only;
- `Tools/NosGM.PacketCatalog` adapts packet-documentation concepts from `BlowaXD/SaltyEmu@2588cfdc64789a7952c781faaafdf1026ac73e9d` and is GPL-3.0-only;
- `Tools/NosGM.ClientEnhancements` preserves the MIT License from `ImNotAVirus/NostaleWidget@fc1b6dda5d797efc24a053180d30702f8dad162a`;
- `Tools/NosGM.ResourceExplorer` preserves the Boost Software License 1.0 from `Pumba98/OnexExplorer@eaee2aa9f0e71b9960da586f425f79e628013021`;
- `Tools/NosGM.ClientThemeEditor` preserves the MIT License from `Elendan/Notale-Text-Picker@9eb44d2a0041b49375fabb730121a01acd7bae87`;
- the complete GNU GPL version 3 text is bundled under [LICENSES/GPL-3.0-only](LICENSES/GPL-3.0-only/README.md).

NosGM is an OpenNos-derived community project. Copyright remains with the respective authors of OpenNos, NQ-Source, ChickenAPI and the adapted external tools, and with NosGM contributors for their own modifications. Renaming namespaces or assemblies does not transfer authorship.

The exact OpenNos base commit is still being narrowed through source comparison, but the project lineage is OpenNos directly. See [docs/PROVENANCE.md](docs/PROVENANCE.md).

If GPL-3.0-only components remain linked into or are distributed with a combined application, the applicable distribution must satisfy GPL version 3. MIT and Boost-covered portions must retain their required notices.

When binaries are distributed, recipients must also receive the complete corresponding source code or equivalent GPL-compliant access to the exact source used to produce it. Do not impose additional restrictions that prevent recipients from copying, modifying or redistributing GPL-covered code.

## Third-party dependencies

NuGet packages and included source components retain their own licenses and notices. Review [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) before building or distributing the project.

## Legal disclaimer

NosGM is independent and unofficial. It is not affiliated with, authorized by, maintained by, sponsored by or endorsed by Gameforge, Entwell or their affiliates.

NosTale and related names, trademarks, artwork, client data and other proprietary assets belong to their respective owners. This repository does not grant permission to redistribute proprietary client files, leaked code or game assets.

## No warranty

NosGM is provided without warranty, to the extent permitted by applicable law. See the applicable license texts and [NOTICE.md](NOTICE.md) for details.

## Community

Discord: https://discord.gg/K5j2vxhf7z
