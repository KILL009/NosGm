# NosGM.PacketCatalog

`NosGM.PacketCatalog` is a standalone .NET 9 maintenance tool that parses the NosGM C# source tree and produces a deterministic packet catalog without loading or executing the legacy server assemblies.

It is intentionally outside `NosGm.sln`, Login, Master and World Server.

## What it catalogs

- `PacketDefinition` classes;
- `PacketHeader` aliases and handler metadata;
- `PacketIndex` fields and serialization options;
- typed packet handlers discovered from method parameters;
- legacy raw handlers declared with `PacketAttribute`;
- source file and line references;
- inferred packet direction with explicit evidence;
- duplicate headers, duplicate indexes, unreachable fields and other structural diagnostics.

## Build

```powershell
dotnet restore Tools/NosGM.PacketCatalog/NosGM.PacketCatalog.csproj
dotnet build Tools/NosGM.PacketCatalog/NosGM.PacketCatalog.csproj -c Release --no-restore
```

## Generate the catalog

From the repository root:

```powershell
dotnet run --project Tools/NosGM.PacketCatalog/NosGM.PacketCatalog.csproj --configuration Release --no-build -- \
  generate --root . --output-directory artifacts/packet-catalog
```

Outputs:

```text
artifacts/packet-catalog/packet-catalog.json
artifacts/packet-catalog/diagnostics.json
artifacts/packet-catalog/PACKETS.md
```

No timestamp is emitted, so unchanged source produces unchanged output.

## Validate only

```powershell
dotnet run --project Tools/NosGM.PacketCatalog/NosGM.PacketCatalog.csproj --configuration Release --no-build -- \
  validate --root . --report artifacts/packet-catalog-validation.json
```

Exit codes:

- `0`: no errors;
- `1`: structural errors were found, or warnings were found with `--strict`;
- `2`: command-line, file-system or permission failure.

## Synthetic self-test

```powershell
dotnet run --project Tools/NosGM.PacketCatalog/NosGM.PacketCatalog.csproj --configuration Release --no-build -- self-test
```

The self-test creates temporary, synthetic packet source files. It does not use proprietary client data.

## Design and safety

- Uses Roslyn syntax parsing rather than regex-only parsing.
- Does not compile, load or execute NosGM server assemblies.
- Does not connect to a client, database, network service or game server.
- Does not modify packet source files.
- Does not generate or send gameplay packets.
- Excludes `.git`, build outputs, packages and generated artifact directories.
- Records every finding with a stable diagnostic code and source location.

Direction is deliberately conservative. A packet consumed by a typed handler is classified as client-to-server. Unhandled packets use source-path evidence and remain explicitly uncertain when that evidence is insufficient.

## Provenance and license

The packet-documentation concept is adapted from `BlowaXD/SaltyEmu` at commit `2588cfdc64789a7952c781faaafdf1026ac73e9d`, including the earlier packet-documentator work introduced at commit `7f849171da82feee1b9fae851a45b3eef9a9cd68`.

The tool is distributed under `GPL-3.0-only`. See `NOTICE.md` and the complete repository license copy under `LICENSES/GPL-3.0-only/`.

The `Microsoft.CodeAnalysis.CSharp` dependency is licensed under MIT. Its notice is preserved in `THIRD_PARTY_LICENSES.md`.
