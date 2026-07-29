# NosGM.DataUpdater

External NosGM maintenance tool that reads `BCard.dat`, generates multilingual JSON catalogs, compares them with the repository and optionally opens a pull request.

This tool is adapted from `noszanou/BCardGistUpdater` commit `53153c990ae5b65a603d223eeda504df2a67d5fb`. Read [NOTICE.md](NOTICE.md) before modifying or redistributing it.

## Why it is separate from the Core

The updater performs resource acquisition, parsing and GitHub writes. None of those responsibilities belong in the login, master or world-server runtime. Keeping the tool under `Tools/` prevents a data refresh failure from affecting gameplay services.

## Two operating modes

### Package-free local mode

The default build has no dependency on private GitHub Packages. It accepts:

- a local `BCard.dat` file;
- one JSON translation map per language.

This core can be restored and compiled by anyone with the .NET 10 SDK.

### Optional automatic-download adapter

The `Za.NosGame.Fetcher` and `Za.NosGame.RessourceLoader` integration can download and extract current resources, but those packages are hosted in the `noszanou` GitHub Packages feed and require authorized `read:packages` access.

Enable that adapter only when restoring, building and running:

```powershell
dotnet restore ./NosGM.DataUpdater.csproj -p:EnableNosGamePackages=true
dotnet build ./NosGM.DataUpdater.csproj -c Release --no-restore -p:EnableNosGamePackages=true
dotnet run --project ./NosGM.DataUpdater.csproj -c Release --no-build -p:EnableNosGamePackages=true -- --download-resources
```

A normal GitHub repository token from another owner may receive `403 Forbidden`. The scheduled workflow therefore requires a dedicated `NOSGAME_PACKAGE_TOKEN` secret that is actually authorized to read those packages.

## Requirements

- .NET 10 SDK
- `BCard.dat`
- translation maps for local mode
- GitHub write access only when `--publish` is used
- authorized package token only when `--download-resources` is used

## Local translation-map format

Create files such as:

```text
C:\NosGM\DataUpdaterWork\input\translations\BCard_ES.json
C:\NosGM\DataUpdaterWork\input\translations\BCard_EN.json
```

Each file is a JSON object mapping the identifiers found in `BCard.dat` to translated text:

```json
{
  "BCARD_NAME_1": "Ataque especial",
  "BCARD_SUBTYPE_11": "No es posible atacar",
  "BCARD_DESCRIPTION_11": "Cancela el ataque"
}
```

Only languages with an available translation map are generated. Missing identifiers fall back to the original identifier instead of inventing text.

## Local dry run

```powershell
$env:NOSGM_UPDATER_REPOSITORY_ROOT = (Resolve-Path ../..)
$env:NOSGM_UPDATER_WORK_DIRECTORY = "C:\NosGM\DataUpdaterWork"
$env:NOSGM_UPDATER_BCARD_FILE = "C:\NosGM\DataUpdaterWork\input\BCard.dat"
$env:NOSGM_UPDATER_TRANSLATION_DIRECTORY = "C:\NosGM\DataUpdaterWork\input\translations"
dotnet run --project ./NosGM.DataUpdater.csproj --configuration Release
```

Generated files that differ from the repository are written under:

```text
C:\NosGM\DataUpdaterWork\preview
```

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

Never place tokens in source files, committed configuration or generated JSON.

## Configuration

| Variable | Default | Purpose |
|---|---|---|
| `NOSGM_UPDATER_OWNER` | `KILL009` | Target repository owner |
| `NOSGM_UPDATER_REPO` | `NosGm` | Target repository name |
| `NOSGM_UPDATER_BASE_BRANCH` | `main` | Pull-request base branch |
| `NOSGM_UPDATER_REPOSITORY_ROOT` | `GITHUB_WORKSPACE` or current directory | Local checkout used for comparison |
| `NOSGM_UPDATER_WORK_DIRECTORY` | OS temporary directory | Input, download and preview workspace |
| `NOSGM_UPDATER_BCARD_FILE` | `<work>/input/BCard.dat` | Local source file |
| `NOSGM_UPDATER_TRANSLATION_DIRECTORY` | `<work>/input/translations` | Local JSON translation maps |
| `NOSGM_UPDATER_OUTPUT_ROOT` | `Data/Generated/BCards` | Repository destination for generated files |
| `NOSGM_UPDATER_LANGUAGES` | `ES,EN,DE,FR,IT,PL,CZ,RU,JP,CN` | Requested language codes |
| `NOSGM_UPDATER_DOWNLOAD_RESOURCES` | `false` | Enables the optional package adapter when compiled |
| `NOSGM_UPDATER_PUBLISH` | `false` | Alternative to passing `--publish` |
| `GITHUB_TOKEN` | none | Token used only for publishing |

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
