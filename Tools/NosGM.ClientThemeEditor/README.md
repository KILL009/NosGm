# NosGM.ClientThemeEditor

`NosGM.ClientThemeEditor` is a standalone .NET 9 command-line tool for safely planning and applying color-only client themes to an authorized local x86 NosTale executable.

It is deliberately kept outside `NosGm.sln` and outside the login, master and world-server runtime.

## Safety model

The editor refuses to patch unless all of these values match an exact local profile:

- file name;
- PE architecture;
- file version;
- file length;
- SHA-256;
- signature match count;
- original bytes at every replacement offset.

The first release ships with **no active signature profile for NosTale 0.9.3.3255**. Use `inspect` to create an exact identity profile, then add only separately reviewed patch definitions for that exact executable.

There is no unverified-client bypass.

## Commands

Generate a locked identity profile:

```powershell
dotnet run --project Tools/NosGM.ClientThemeEditor -- `
  inspect `
  --input "C:\NosTale\NostaleClientX.exe" `
  --profile-output ".\profiles\client-local.json"
```

Validate a theme and write a plan without changing a file:

```powershell
dotnet run --project Tools/NosGM.ClientThemeEditor -- `
  plan `
  --input "C:\NosTale\NostaleClientX.exe" `
  --profile ".\profiles\client-local.json" `
  --theme ".\themes\nosgm-blue.json" `
  --report-output ".\plans\nosgm-blue-plan.json"
```

Create a separate patched copy:

```powershell
dotnet run --project Tools/NosGM.ClientThemeEditor -- `
  apply `
  --input "C:\NosTale\NostaleClientX.exe" `
  --profile ".\profiles\client-local.json" `
  --theme ".\themes\nosgm-blue.json" `
  --output "C:\NosTale\NosGM\NostaleClientX.exe"
```

Guarded in-place mode creates a verified backup and manifest before atomic replacement:

```powershell
dotnet run --project Tools/NosGM.ClientThemeEditor -- `
  apply `
  --input "C:\NosTale\NostaleClientX.exe" `
  --profile ".\profiles\client-local.json" `
  --theme ".\themes\nosgm-blue.json" `
  --in-place
```

Restore from the generated backup manifest:

```powershell
dotnet run --project Tools/NosGM.ClientThemeEditor -- `
  restore `
  --manifest "C:\NosTale\NosGM.ThemeBackups\<backup>\manifest.json"
```

Run synthetic regression tests:

```powershell
dotnet run --project Tools/NosGM.ClientThemeEditor -- self-test
```

## Theme colors

Theme values use `#RRGGBB` or `#RRGGBBAA`. A profile decides how each color is encoded in the target executable:

- `RGBA`
- `BGRA`
- `ARGB`
- `ABGR`
- `RGB`
- `BGR`

Suggested patch identifiers are:

- `gm-tag`
- `right-click`
- `object-label-*`
- `weapon-glow-*`

Only identifiers present in an enabled, exact-hash profile can produce changes.

## Patch profiles

A patch definition contains:

```json
{
  "id": "gm-tag",
  "description": "GM tag color",
  "enabled": true,
  "patternHex": "AA BB ?? DD",
  "expectedMatches": 1,
  "valueOffset": 4,
  "expectedOriginalHex": "11 22 33 44",
  "colorEncoding": "RGBA"
}
```

Patterns may contain `??` wildcards, but cannot be all wildcards. The editor refuses ambiguous signatures, out-of-range offsets, unexpected original bytes and overlapping writes.

## What is not included

- no injector or DLL loader;
- no runtime process-memory modification;
- no packet interception or injection;
- no gameplay automation;
- no bundled client executable, memory dump or proprietary asset;
- no copied source from `Pumba98/Nostale-ClientColorizer`, whose reviewed repository did not contain a reuse license;
- no active signature copied from an unverified 2019 client into the 0.9.3.3255 profile.

## Provenance

The GM-tag and right-click color-picker workflow is adapted from `Elendan/Notale-Text-Picker` commit `9eb44d2a0041b49375fabb730121a01acd7bae87`, Copyright (c) 2019 Elendan, under the MIT License. The upstream README also credits Cryless and Fizo55.

The weapon-glow and object-label feature categories were independently described after reviewing `Pumba98/Nostale-ClientColorizer` commit `9d1e61c717b6a49ca221a5f2d855dfa5fa11591c`. No source code, signatures or offsets from that repository are copied because no reuse license was found in the reviewed tree.

See [NOTICE.md](NOTICE.md) and [LICENSE](LICENSE).
