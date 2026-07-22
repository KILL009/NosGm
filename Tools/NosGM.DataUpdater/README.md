# NosGM.DataUpdater

External NosGM maintenance tool that downloads or reads NosTale client resources, extracts `BCard.dat`, generates multilingual JSON catalogs, compares them with the repository and optionally opens a pull request.

This tool is adapted from `noszanou/BCardGistUpdater` commit `53153c990ae5b65a603d223eeda504df2a67d5fb`. Read [NOTICE.md](NOTICE.md) before modifying or redistributing it.

## Why it is separate from the Core

The updater performs downloads, parsing and GitHub writes. None of those responsibilities belong in the login, master or world-server runtime. Keeping the tool under `Tools/` prevents a data refresh failure from affecting gameplay services.

## Requirements

- .NET 9 SDK
- GitHub account
- token capable of reading the `noszanou` GitHub Packages feed
- write access only when `--publish` is used

## Local dry run

Configure the package source:

```powershell
dotnet nuget add source "https://nuget.pkg.github.com/noszanou/index.json" `
  --name noszanou `
  --username YOUR_GITHUB_USER `
  --password YOUR_PACKAGE_TOKEN `
  --store-password-in-clear-text
```

Run without publishing:

```powershell
$env:NOSGM_UPDATER_REPOSITORY_ROOT = (Resolve-Path ../..)
dotnet run --project ./NosGM.DataUpdater.csproj --configuration Release
```

Generated files that differ from the repository are written to the preview directory under the temporary working folder.

## Local resources mode

Place already-extracted resources under the configured working directory and run:

```powershell
$env:NOSGM_UPDATER_WORK_DIRECTORY = "C:\NosGM\DataUpdaterWork"
dotnet run --project ./NosGM.DataUpdater.csproj -- --local-resources
```

The expected source file is resolved through `DatFileFolder` and must contain `BCard.dat`.

## Publish mode

```powershell
$env:GITHUB_TOKEN = "YOUR_FINE_GRAINED_TOKEN"
$env:NOSGM_UPDATER_OWNER = "KILL009"
$env:NOSGM_UPDATER_REPO = "NosGm"
$env:NOSGM_UPDATER_BASE_BRANCH = "main"
dotnet run --project ./NosGM.DataUpdater.csproj -- --publish
```

The publisher compares all generated content before creating a branch. When nothing changed, it creates neither a branch nor a pull request.

Recommended fine-grained permissions for the target repository:

- Contents: read and write
- Pull requests: read and write

Never place tokens in source files, configuration committed to Git or generated JSON.

## Configuration

| Variable | Default | Purpose |
|---|---|---|
| `NOSGM_UPDATER_OWNER` | `KILL009` | Target repository owner |
| `NOSGM_UPDATER_REPO` | `NosGm` | Target repository name |
| `NOSGM_UPDATER_BASE_BRANCH` | `main` | Pull-request base branch |
| `NOSGM_UPDATER_REPOSITORY_ROOT` | `GITHUB_WORKSPACE` or current directory | Local checkout used for comparison |
| `NOSGM_UPDATER_WORK_DIRECTORY` | OS temporary directory | Download, extraction and preview workspace |
| `NOSGM_UPDATER_OUTPUT_ROOT` | `Data/Generated/BCards` | Repository destination for generated files |
| `NOSGM_UPDATER_LANGUAGES` | `ES,EN,DE,FR,IT,PL,CZ,RU,JP,CN` | Requested language codes |
| `NOSGM_UPDATER_PUBLISH` | `false` | Alternative to passing `--publish` |
| `GITHUB_TOKEN` | none | Token used only for publishing |

Unsupported language enum values are reported and skipped instead of silently producing incorrect files.

## Output

For every supported configured language:

```text
Data/Generated/BCards/BCard_ES.json
Data/Generated/BCards/BCard_EN.json
...
```

The updater also creates:

- `manifest.json`, containing source hash and record counts;
- `CHANGE_REPORT.md`, listing added or removed types and subtypes and changed labels.

No generation timestamp is stored, so unchanged game data does not create noisy pull requests.
