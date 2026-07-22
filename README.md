# NosGM

NosGM is an open-source NosTale server emulator based on the OpenNos ecosystem and later community work.

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

The repository contains the GNU General Public License version 2 text in [LICENSE](LICENSE). Reviewed original OpenNos file notices state GNU GPL version 2 or, at the recipient's option, any later version.

NosGM is a derivative community project. Copyright remains with the respective authors of OpenNos, intermediate OpenNos ecosystem projects, ChickenAPI-related components and NosGM modifications. Renaming namespaces or assemblies does not transfer authorship.

Some inherited component origins are still being reconstructed. See [docs/PROVENANCE.md](docs/PROVENANCE.md). Do not publish new prebuilt binaries until every source component required by the build has a verified origin and compatible license.

When binaries are distributed, recipients must also receive the complete corresponding source code or equivalent GPL-compliant access to the exact source used for that build. Do not impose additional restrictions that prevent recipients from copying, modifying or redistributing GPL-covered code.

## Third-party dependencies

NuGet packages and included source components retain their own licenses and notices. Review [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) before building or distributing the project.

## Legal disclaimer

NosGM is independent and unofficial. It is not affiliated with, authorized by, maintained by, sponsored by or endorsed by Gameforge, Entwell or their affiliates.

NosTale and related names, trademarks, artwork, client data and other proprietary assets belong to their respective owners. This repository does not grant permission to redistribute proprietary client files, leaked code or game assets.

## No warranty

NosGM is provided without warranty, to the extent permitted by applicable law. See [LICENSE](LICENSE) for the complete terms.

## Community

Discord: https://discord.gg/K5j2vxhf7z
