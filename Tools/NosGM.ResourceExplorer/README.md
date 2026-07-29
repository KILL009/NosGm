# NosGM.ResourceExplorer

A conservative, package-free .NET 10 command-line tool for inspecting and safely extracting supported NosTale `.NOS` resource archives. It remains outside `NosGm.sln` and outside the server runtime.

## Supported in the first release

- compressed archives beginning with `NT Data`, `32GBS V1.0` or `ITEMS V1.0`;
- headerless text containers using the DAT/LST layouts reviewed in OnexExplorer;
- entry listing with SHA-256, sizes, offsets and encoding hints;
- sandboxed extraction;
- archive comparison;
- deterministic JSON reports;
- synthetic self-test.

The first release does not repack, patch, edit or overwrite source archives. Image and model decoding are intentionally deferred until representative authorized samples and stronger format-specific tests are available.

## Build

```powershell
cd Tools\NosGM.ResourceExplorer
dotnet restore
dotnet build -c Release --no-restore
dotnet run -c Release --no-build -- self-test
```

## Inspect

```powershell
dotnet run -c Release -- inspect `
  --input "C:\NosTale\NStcData.NOS" `
  --report ".\reports\NStcData.json"
```

## Extract safely

```powershell
dotnet run -c Release -- extract `
  --input "C:\NosTale\NStcData.NOS" `
  --output-directory ".\extracted\NStcData"
```

Existing extracted files are not overwritten unless `--force` is supplied. The original `.NOS` file is never modified.

## Compare versions

```powershell
dotnet run -c Release -- compare `
  --left "C:\ClientOld\NStcData.NOS" `
  --right "C:\ClientNew\NStcData.NOS" `
  --report ".\reports\NStcData-diff.json"
```

Exit code `1` means supported entries differ; malformed or unsupported input returns `2`.

## Text encoding hints

The tool preserves decoded bytes and reports a hint rather than silently transcoding. It recognizes common ES, EN, DE, FR, IT, PL, CZ, RU, JP and CN filename markers. Japanese and Chinese hints remain explicitly client-specific until verified samples establish the exact encoding.

## Attribution

Adapted from `Pumba98/OnexExplorer` at commit `eaee2aa9f0e71b9960da586f425f79e628013021` under the Boost Software License 1.0. See `NOTICE.md` and `LICENSE`.
