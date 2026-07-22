# NosGM

NosGM is an open-source NosTale server emulator derived from OpenNos.

The project is under active development. Current work includes login, world and channel communication, client and packet compatibility, character and session stability, performance, memory usage, timers, bug fixing, localization and documentation.

> Current target client: **0.9.3.3255**

## Project status

NosGM is experimental software. Systems may be incomplete, unstable or incompatible with some clients. Do not use the project without understanding the security, data-loss and operational risks of running an unfinished server emulator.

## Build requirements

The solution currently targets the .NET Framework and contains legacy-style Visual Studio projects. Use the solution and project files in the repository as the authoritative build configuration.

Before sharing a build, record the exact source commit and preserve the corresponding source code used to produce it.

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

NosGM contains code from these verified GPL lineages:

- the root [LICENSE](LICENSE) preserves the GNU General Public License version 2 text inherited with the OpenNos-derived source;
- reviewed original OpenNos file notices permit GNU GPL version 2 or, at the recipient's option, any later version;
- the included ChickenAPI source is derived from the verified `Price-H16/NQ-Verde` snapshot `2594ec13f4fba5d893b424197878c05f801f68a2` and is conservatively treated as GPL-3.0-only;
- the complete GNU GPL version 3 text is bundled under [LICENSES/GPL-3.0-only](LICENSES/GPL-3.0-only/README.md).

NosGM is an OpenNos-derived community project. Copyright remains with the respective authors of OpenNos, NQ-Source and ChickenAPI, and with NosGM contributors for their own modifications. Renaming namespaces or assemblies does not transfer authorship.

The exact OpenNos base commit is still being narrowed through source comparison, but the project lineage is OpenNos directly. See [docs/PROVENANCE.md](docs/PROVENANCE.md).

If ChickenAPI remains linked into and distributed as part of the combined application, the distribution must satisfy GPL version 3.

When binaries are distributed, recipients must also receive the complete corresponding source code or equivalent GPL-compliant access to the exact source used for that build. Do not impose additional restrictions that prevent recipients from copying, modifying or redistributing GPL-covered code.

## Third-party dependencies

NuGet packages and included source components retain their own licenses and notices. Review [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) before building or distributing the project.

## Legal disclaimer

NosGM is independent and unofficial. It is not affiliated with, authorized by, maintained by, sponsored by or endorsed by Gameforge, Entwell or their affiliates.

NosTale and related names, trademarks, artwork, client data and other proprietary assets belong to their respective owners. This repository does not grant permission to redistribute proprietary client files, leaked code or game assets.

## No warranty

NosGM is provided without warranty, to the extent permitted by applicable law. See the applicable GPL texts and [NOTICE.md](NOTICE.md) for details.

## Community

Discord: https://discord.gg/K5j2vxhf7z
