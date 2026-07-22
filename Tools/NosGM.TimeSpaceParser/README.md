# NosGM.TimeSpaceParser

`NosGM.TimeSpaceParser` is a standalone .NET 9 maintenance tool that converts a NosTale Time-Space packet capture into XML shaped for the `NosGm.XMLModel` scripted-instance loader.

It is deliberately kept outside the login, master and world-server runtime. A malformed capture or parser regression must never prevent the game services from starting.

## Provenance and license

This tool is adapted from `noszanou/OpennosTimeSpaceParser` at commit `36bd96a51c4b4a1e55e8827ca7eb375cc189ad9e`, which identifies earlier work by Elendan, the contributor known as SEOVA and the OpenNos XML model. Read [NOTICE.md](NOTICE.md) before modifying or redistributing the tool.

The adapted tool is distributed under `GPL-3.0-only`.

## Requirements

- .NET 9 SDK
- a plain-text packet capture supplied by the operator
- no proprietary client archive is bundled or downloaded
- no database or running NosGM server is required

## Build and self-test

```powershell
dotnet restore .\NosGM.TimeSpaceParser.csproj
dotnet build .\NosGM.TimeSpaceParser.csproj -c Release --no-restore
dotnet run --project .\NosGM.TimeSpaceParser.csproj -c Release --no-build -- self-test
```

The self-test uses only `Samples/packet.sample.txt`, a synthetic capture created for NosGM. It contains no extracted client data.

## Parse one capture

```powershell
dotnet run --project .\NosGM.TimeSpaceParser.csproj -- parse `
  --input "C:\Captures\packet.txt" `
  --output "C:\Captures\TimeSpace_001.xml" `
  --strict
```

Optional review overrides:

```powershell
--name "Time-Space name"
--label "Description"
--lives 1
--gold 0
--reputation 0
--force
```

`--strict` prevents XML creation when any warning remains. Without strict mode, structural errors still block output, while warnings are written to the reports for manual review.

## Validate existing XML

```powershell
dotnet run --project .\NosGM.TimeSpaceParser.csproj -- validate `
  --input "C:\Captures\TimeSpace_001.xml" `
  --strict
```

This creates `TimeSpace_001.xml.validation.json`.

## Batch mode

```powershell
dotnet run --project .\NosGM.TimeSpaceParser.csproj -- batch `
  --input-directory "C:\Captures" `
  --output-directory "C:\TimeSpaces" `
  --pattern "*.txt" `
  --strict
```

## Generated files

For `TimeSpace_001.xml`, the parser also writes:

```text
TimeSpace_001.report.json
TimeSpace_001.report.md
```

Reports contain a SHA-256 of the source capture, packet and entity counts, ignored lines and every warning or error. They contain no generation timestamp, so identical inputs produce stable review files.

## Packet coverage

The initial NosGM adaptation recognizes:

- `rbr`
- `at`
- `rsfn` and `rsfm`
- `walk`
- `in`
- `su`
- `gp`
- `msg`
- `npc_req`
- `evnt`
- `out`
- `preq`
- `eff`
- `mapclear` and `mapclean`
- selected server packets preserved through `SendPacket`: `sinfo`, `minfo`, `msgi`

Unsupported lines are not silently discarded. They appear in the generated report.

## Important review rules

The parser is an assistant, not an oracle. Before importing generated XML:

1. Review inferred portal destinations.
2. Confirm map VNums and coordinates.
3. Confirm monster, NPC, object and dialog VNums.
4. Confirm lives, rewards, gold and reputation.
5. Confirm clocks and end conditions.
6. Test the Time-Space on a development server before production use.

The parser uses the destination runtime map from `gp` when it matches a captured `at` room. Only unresolved destinations fall back to a documented positional heuristic, and those results are marked as warnings.

## Safety

- The tool does not connect to the game client or server.
- It does not capture network traffic.
- It does not write to the NosGM database.
- It does not overwrite output unless `--force` is supplied.
- It never runs as part of the game-server solution.
- Real captures and proprietary client files must not be committed to the repository.
