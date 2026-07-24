# Releasing NosGM Launcher

This document describes the release boundary for the standalone NosGM Launcher. It does not authorize distribution of NosTale client files or assets.

## Trust separation

Use two separate locations:

1. **Offline signing location**: stores the ECDSA P-256 private release key and signs client release manifests.
2. **Launcher build location**: receives only the corresponding public key and creates the launcher package.

The private key must never be copied into GitHub Actions, the launcher source tree, the CDN, the game server or a player installation.

## Prepare the public channel

The content server must expose:

- one clean HTTPS manifest URL;
- one clean HTTPS content base URL ending in `/`;
- no redirects for manifest or content requests;
- files at the relative URLs declared by the signed manifest.

Generate the public channel source through `NosGM.ManifestBuilder`; do not edit generated C# by hand:

```powershell
dotnet run --project Launcher/src/NosGM.ManifestBuilder -- channel `
  --manifest-uri "https://updates.example.org/nosgm/release-manifest.json" `
  --content-base-uri "https://updates.example.org/nosgm/content/" `
  --key-id "nosgm-release-1" `
  --public-key "C:\NosGM\public\nosgm-release-public.pem" `
  --output "Launcher/src/NosGM.Launcher/TrustedChannel.Generated.cs"
```

`TrustedChannel.Generated.cs` is intentionally ignored by Git. Normal development builds continue using the disabled `.invalid` placeholder.

## Build a portable package

The publishing script requires a clean tracked Git tree and records the exact source commit:

```powershell
./Launcher/scripts/publish-launcher.ps1 `
  -Version "1.0.0" `
  -ManifestUri "https://updates.example.org/nosgm/release-manifest.json" `
  -ContentBaseUri "https://updates.example.org/nosgm/content/" `
  -KeyId "nosgm-release-1" `
  -PublicKeyPath "C:\NosGM\public\nosgm-release-public.pem" `
  -OutputDirectory "C:\NosGM\releases\launcher-1.0.0-win-x64" `
  -Runtime win-x64
```

The script:

- generates the public channel source;
- publishes a self-contained single-file Windows launcher;
- sets deterministic and continuous-integration build properties;
- copies required license, author and notice documents;
- records the exact source commit and public-key fingerprint;
- creates `release-info.json` with the size and SHA-256 of every delivered file;
- verifies the package before moving it to the requested destination;
- removes generated channel source in a `finally` block.

The process is reproducible from the recorded commit, public channel values, public key, .NET 9 SDK and runtime identifier. Platform signing can add certificate-dependent bytes afterward and is therefore a separate release step.

## Verify and optionally sign

Verify an unsigned package:

```powershell
./Launcher/scripts/verify-launcher-package.ps1 `
  -PackageDirectory "C:\NosGM\releases\launcher-1.0.0-win-x64"
```

Authenticode signing must occur in a protected signing environment using a certificate whose private key is not stored in the repository. After signing, regenerate `release-info.json` for the changed executable or place the signature step before final metadata generation in the protected release pipeline. Then verify with:

```powershell
./Launcher/scripts/verify-launcher-package.ps1 `
  -PackageDirectory "C:\NosGM\releases\launcher-1.0.0-win-x64" `
  -RequireAuthenticode
```

Do not publish an Authenticode-signed executable with stale SHA-256 metadata.

## Release checklist

- All pull-request checks are green.
- The source commit is reviewed and tagged.
- The public key fingerprint matches the offline release record.
- The manifest `keyId` matches the launcher channel `keyId`.
- The CDN serves the manifest and content directly over HTTPS without redirects.
- `verify-launcher-package.ps1` passes.
- The package contains no private key, client executable, `.NOS` archive, `.pak` file or account credential.
- The package includes all license, author and third-party notice files.
- The exact source used for the binary remains publicly available.
