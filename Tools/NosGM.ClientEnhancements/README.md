# NosGM.ClientEnhancements

`NosGM.ClientEnhancements` is a conservative x86 client-compatibility foundation for NosGM. It is kept outside `NosGm.sln` and outside the login, master and world-server runtime.

The first release deliberately favors a locked door over a dramatic client crash. It identifies the exact executable, validates memory ranges and provides reversible patch infrastructure, but it ships without active client signatures or gameplay modifications.

## Current capabilities

- `NosGM.ClientProbe.exe` reads an authorized local client executable.
- The probe reports PE architecture, file version and SHA-256.
- The probe writes a complete local profile with all signature-dependent features disabled.
- `NosGM.ClientEnhancements.dll` exposes explicit initialize, shutdown, status and API-version functions.
- Initialization succeeds only when architecture, file version and SHA-256 match the profile exactly.
- A JSON status report is written beside the profile.
- Pattern parsing supports hexadecimal bytes and `??` wildcards.
- Patch transactions require expected bytes, validate executable memory and restore original bytes.

## Not included

- no injector or automatic DLL loader;
- no packet interception, packet injection or arbitrary server/client packet bridge;
- no automated movement, combat or farming;
- no bundled NosTale executable, memory dump, packet capture or proprietary asset;
- no active FPS, resolution, cooldown, Discord or render patch before exact signatures are validated.

## Build on Windows

Requirements:

- Visual Studio 2022 with Desktop development with C++;
- CMake 3.22 or newer;
- an x86 build configuration.

```powershell
cd Tools\NosGM.ClientEnhancements

cmake -S . -B build -A Win32 `
  -DNOSGM_CLIENT_ENHANCEMENTS_BUILD_TESTS=ON

cmake --build build --config Release

ctest --test-dir build -C Release --output-on-failure
```

Expected binaries:

```text
build\Release\NosGM.ClientEnhancements.dll
build\Release\NosGM.ClientProbe.exe
build\Release\NosGM.ClientEnhancements.Tests.exe
```

## Generate an exact client profile

Use only an executable you are legally allowed to inspect and run:

```powershell
.\build\Release\NosGM.ClientProbe.exe `
  --input "C:\NosTale\NostaleClientX.exe" `
  --profile-output ".\profiles\client-local.ini"
```

The probe refuses non-x86 executables. The generated profile records the exact version and SHA-256 and leaves every signature-dependent feature disabled.

## Module API

The DLL does not initialize itself from `DllMain`. An authorized loader may call these exports:

```cpp
unsigned int __stdcall NosGM_GetApiVersion();
int __stdcall NosGM_Initialize(const wchar_t* profilePath);
int __stdcall NosGM_Shutdown();
const wchar_t* __stdcall NosGM_GetLastStatus();
```

No loader is distributed by this repository. This prevents the server project from becoming an injector distribution point.

## Profile rules

The module requires:

```text
profile_name
expected_file_version
expected_sha256
target_architecture=x86
```

Unknown keys, malformed booleans, incomplete SHA-256 values and non-x86 profiles are rejected. There is no `allow_unverified_client` escape hatch.

## Planned features

These are represented as disabled profile flags and require a separately reviewed signature profile before implementation:

- NosGM Discord Rich Presence;
- numeric cooldown labels;
- configurable FPS limits;
- additional validated resolutions;
- render reduction while minimized.

Each future feature must fail closed when a signature is missing, verify the original bytes, restore every change on shutdown and have a regression test for the exact target executable.

## Attribution

This component is adapted from ideas and client-structure research in `ImNotAVirus/NostaleWidget` at commit `fc1b6dda5d797efc24a053180d30702f8dad162a`, copyright 2022 ApourtArtt, under the MIT License. See [NOTICE.md](NOTICE.md) and [LICENSE](LICENSE).
