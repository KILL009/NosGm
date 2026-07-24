# NosGM Launcher

`NosGM Launcher` is a standalone Windows launcher and transactional updater for an authorized NosTale client installation.

This foundation is intentionally kept outside `NosGm.sln` and outside the Login, Master and World Server runtime. It is currently staged inside the NosGM repository only because the connected GitHub integration cannot create a new repository. The directory is designed to be extracted later into `KILL009/NosGM.Launcher` without carrying server code with it.

## Security model

A release is accepted only when:

- the JSON manifest uses the supported schema;
- its `keyId` matches the release channel pinned in the launcher;
- its canonical payload has a valid ECDSA P-256 / SHA-256 signature;
- every managed path is relative, normalized and confined to the client root;
- no existing parent path is a symbolic link or Windows reparse point;
- every downloaded file has the declared size and SHA-256;
- all downloads finish in a staging directory before commit;
- replaced or deleted managed files are moved into rollback storage before activation;
- only files recorded in the local managed-install state may be removed;
- one exclusive installation lock prevents concurrent update, import and recovery operations;
- a durable transaction journal recovers or finalizes interrupted commits on the next launch;
- the game is launched without administrator elevation.

The private release-signing key is never part of the launcher, repository or web server. Only the public key is pinned in the launcher build.

## Launcher languages

The WPF interface includes complete built-in navigation, state, confirmation and progress catalogs for:

- Spanish (`es`)
- English (`en`)
- German (`de`)
- French (`fr`)
- Italian (`it`)
- Polish (`pl`)
- Czech (`cz`)
- Russian (`ru`)
- Japanese (`jp`)
- Simplified Chinese (`cn`)

The selected language is saved in the per-user launcher settings and can be changed without restarting. Catalog validation runs before the main window is initialized and refuses incomplete languages. Low-level exception details remain unchanged so diagnostics preserve their original technical meaning.

## Existing installation import

The launcher can explicitly adopt an existing authorized client installation after downloading and verifying the signed release manifest.

Import does not copy, delete or replace client files. It records only existing files whose relative paths appear in the signed manifest. Matching files are accepted as-is; mismatched files are marked as managed so a later **Repair** operation may replace them. Missing files remain untracked until downloaded. Extra player files and paths outside the signed manifest are never adopted.

An installation that already has a non-empty `.nosgm/state.json` cannot be imported again.

## Crash recovery

Each update writes `.nosgm/transactions/<id>/journal.json` before installation files are changed. The journal records the target managed state, planned operations and whether each destination existed before commit.

On startup:

- staging-only and prepared transactions are discarded because no installation change began;
- interrupted commits are rolled back from durable backup files;
- transactions whose target state was already atomically saved are finalized without undoing a completed release;
- malformed journals or missing recovery evidence stop automatic updates instead of guessing.

The local `.nosgm/update.lock` file is opened with exclusive sharing during import, recovery and update operations. Its presence alone does not indicate that the installation is locked; the operating-system file handle is authoritative.

## Projects

- `src/NosGM.Updater.Core`: manifest validation, signature verification, path sandboxing, planning, streaming downloads, staging, rollback, installation locking, import and crash recovery.
- `src/NosGM.ManifestBuilder`: package-free CLI for generating signing keys, building and verifying manifests, calculating public-key fingerprints and generating trusted public channel source.
- `src/NosGM.Launcher`: multilingual WPF shell for importing, checking, repairing and launching the client.
- `tests/NosGM.Updater.SelfTest`: package-free synthetic regression suite, including interrupted-commit recovery.

## Build

```powershell
dotnet restore Launcher/NosGM.Launcher.sln
dotnet build Launcher/NosGM.Launcher.sln --configuration Release --no-restore
dotnet run --project Launcher/tests/NosGM.Updater.SelfTest --configuration Release --no-build
```

## Generate release keys

Run this on a trusted offline or tightly controlled release workstation:

```powershell
dotnet run --project Launcher/src/NosGM.ManifestBuilder -- keygen `
  --private-key .\secrets\nosgm-release-private.pem `
  --public-key .\public\nosgm-release-public.pem
```

The private key path is ignored by Git. Back it up securely and never upload it to the CDN, launcher repository or game server.

## Build a signed manifest

```powershell
dotnet run --project Launcher/src/NosGM.ManifestBuilder -- build `
  --root "C:\NosGM\Release\0.9.3.3255-nosgm.1" `
  --release-id "0.9.3.3255-nosgm.1" `
  --client-version "0.9.3.3255" `
  --minimum-launcher-version "1.0.0" `
  --key-id "nosgm-release-1" `
  --private-key .\secrets\nosgm-release-private.pem `
  --output .\release-manifest.json
```

Manifest URLs are relative to the trusted content base URI compiled into the launcher. The builder refuses reparse points and excludes its own output when that output sits inside the release directory.

## Configure and publish the launcher

Development builds use `TrustedChannel.Placeholder.cs`, which keeps the channel disabled with `.invalid` URLs. Release builds must generate `TrustedChannel.Generated.cs` from a clean HTTPS channel and the public half of the P-256 release key. The generated source is ignored by Git and replaces the placeholder only during publication.

Use the guarded publishing pipeline:

```powershell
./Launcher/scripts/publish-launcher.ps1 `
  -Version "1.0.0" `
  -ManifestUri "https://updates.example.org/nosgm/release-manifest.json" `
  -ContentBaseUri "https://updates.example.org/nosgm/content/" `
  -KeyId "nosgm-release-1" `
  -PublicKeyPath "C:\NosGM\public\nosgm-release-public.pem" `
  -OutputDirectory "C:\NosGM\releases\launcher-1.0.0-win-x64"
```

The resulting self-contained package includes `release-info.json`, SHA-256 metadata for every delivered file, the exact Git source commit and required license and attribution documents. See [`RELEASING.md`](RELEASING.md) for the complete trust boundary, verification and optional Authenticode process.

## Boundaries

This repository does not contain a NosTale executable, client archive, proprietary asset, private signing key, account credential, packet injector, gameplay automation or administrator-elevation mechanism.
