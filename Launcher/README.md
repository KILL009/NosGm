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
- the game is launched without administrator elevation.

The private release-signing key is never part of the launcher, repository or web server. Only the public key is pinned in the launcher build.

## Projects

- `src/NosGM.Updater.Core`: manifest validation, signature verification, path sandboxing, planning, streaming downloads, staging and rollback.
- `src/NosGM.ManifestBuilder`: package-free CLI for generating signing keys, building signed manifests and verifying releases.
- `src/NosGM.Launcher`: minimal WPF shell for checking, repairing and launching the client.
- `tests/NosGM.Updater.SelfTest`: package-free synthetic regression suite.

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

## Configure the release channel

`src/NosGM.Launcher/TrustedChannel.cs` intentionally ships disabled. Before publishing a launcher build, set:

- the HTTPS manifest URI;
- the HTTPS content base URI;
- the expected `keyId`;
- the public-key PEM.

The launcher refuses `.invalid`, non-HTTPS and key-mismatched channels.

## Boundaries

This repository does not contain a NosTale executable, client archive, proprietary asset, private signing key, account credential, packet injector, gameplay automation or administrator-elevation mechanism.
