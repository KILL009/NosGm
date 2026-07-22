# NosGM.PacketCatalog attribution notice

`NosGM.PacketCatalog` is an external maintenance and documentation tool for NosGM. It is not part of the Login, Master or World Server runtime.

## Upstream inspiration

- Project: `BlowaXD/SaltyEmu`
- Reviewed repository revision: `2588cfdc64789a7952c781faaafdf1026ac73e9d`
- Packet-documentator introduction: `7f849171da82feee1b9fae851a45b3eef9a9cd68`
- Primary author recorded by the upstream project: Blowa
- Additional contributors: Kraken, Zanou, Kiritsu, GodnessCookie, Quarry, Clavs, SylEze and contributors recorded in the upstream Git history
- Upstream license: GNU General Public License version 3

The SaltyEmu packet documentator introduced packet and property-description attributes and an unfinished reflection-based documentation command. NosGM does not claim that unfinished code or its architecture as original NosGM work.

## NosGM implementation

NosGM contributors implemented a new source-based tool with:

- Roslyn syntax parsing instead of runtime assembly loading;
- deterministic JSON and Markdown output;
- typed and raw handler cross-references;
- source path and line reporting;
- duplicate header and handler detection;
- `PacketIndex` structural validation;
- conservative packet-direction inference;
- strict mode and stable exit codes;
- synthetic, non-proprietary regression tests;
- CI, attribution and safety checks;
- complete separation from the server runtime.

Copyright in SaltyEmu remains with Blowa and the respective SaltyEmu contributors for the portions they authored. Copyright in the new NosGM implementation remains with the respective NosGM contributors.

## License

This tool is distributed under `GPL-3.0-only`. The complete GNU GPL version 3 text is stored under `LICENSES/GPL-3.0-only/`.

## Third-party dependency

`Microsoft.CodeAnalysis.CSharp` 5.6.0 is maintained by Microsoft and the Roslyn contributors and is licensed under MIT. The required notice is preserved in `THIRD_PARTY_LICENSES.md`.
