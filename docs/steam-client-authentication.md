# Steam client authentication for NosGM

NosGM can use an authorized NosTale installation managed by Steam without authenticating against Steam or Gameforge services. The launcher prepares a separate local executable and supplies the short-lived NosGM authorization code through a small NativeAOT x86 compatibility DLL.

## Trust boundary

The launcher never modifies or redistributes the original Steam client. It creates these two files beside the user's existing installation:

```text
NostaleClientX_NosGM.exe
noscore_gf.dll
```

`NostaleClientX_NosGM.exe` is derived locally from the user's own `NostaleClientX.exe`. It must not be committed, uploaded or distributed. `noscore_gf.dll` is built from the MIT-licensed source under `Launcher/src/NosGM.SteamAuthStub` and is embedded in the launcher package.

Passwords are sent only to the configured NosGM AuthBridge. The password, authorization code and InstallationId are not written to launcher settings. The one-use authorization code is inherited only by the child process through `_NC_AUTH_CODE`.

## Automatic transport selection

The default launcher setting is:

```json
"AuthenticationTransport": "auto"
```

`auto` selects `steam-stub` when the installation contains `steam_api.dll`, `steam_api64.dll`, or is under a `steamapps/common` path. Other installations continue using `gameforge-pipe`.

The process-scoped overrides are:

```powershell
$env:NOSGM_LOGIN_TRANSPORT = "steam-stub"   # auto | steam-stub | gameforge-pipe
$env:NOSGM_LOGIN_ADDRESS = "127.0.0.1"       # IPv4 only, maximum 15 characters
```

The local startup workflow already works with the defaults, so these overrides are normally unnecessary.

## What the launcher patches

The preparation is transactional and refuses ambiguous binaries. It performs only two binary changes in the copied executable:

1. Rewrites the Delphi constant AnsiString containing the Login server IPv4 address.
2. Rewrites the equal-length import name `gf_wrapper.dll` to `noscore_gf.dll`.

The launcher does not patch gameplay, packet handlers, anti-cheat logic, rendering, updater files or the original executable. A temporary file is fully written and flushed before replacing a previous NosGM copy.

## InstallationId synchronization

The authorization ticket is bound to one InstallationId. The launcher resolves one GUID and synchronizes it to the current-user TNT location and the 32-bit Gameforge/Steam registry location before requesting the ticket.

The machine-wide 32-bit registry location may require one elevated launcher run. The local development script is normally started from an Administrator Developer PowerShell, which satisfies this requirement. Later launches do not need to rewrite the value when it already matches.

## Build requirements

Running a published NosGM Launcher does not require an installed SDK. Building the launcher from source requires:

- .NET 9 SDK for the WPF launcher;
- .NET 10 SDK for NativeAOT win-x86;
- Visual Studio Build Tools with the native C++ toolchain required by NativeAOT on Windows.

Build and test:

```powershell
dotnet restore .\Launcher\NosGM.Launcher.sln
dotnet build .\Launcher\NosGM.Launcher.sln --configuration Release

dotnet run `
  --project .\Launcher\tests\NosGM.SteamClient.SelfTest\NosGM.SteamClient.SelfTest.csproj `
  --configuration Release
```

The self-test creates a synthetic client and verifies that:

- the original SHA-256 remains unchanged;
- the Login address and wrapper import change only in the copy;
- the embedded stub is an x86 PE image;
- ambiguous IP constants are rejected;
- rejected preparation leaves no output files.

## Local acceptance test

From an elevated Developer PowerShell:

```powershell
.\scripts\stop-modern-login-local.ps1

git checkout main
git pull

dotnet build .\Launcher\NosGM.Launcher.sln --configuration Release
.\scripts\start-modern-login-local.ps1 -SkipBuild
```

In NosGM Launcher:

1. Select the Steam NosTale directory.
2. Keep `NostaleClientX.exe` as the configured source executable.
3. Select Spanish.
4. Press **Play** and enter a valid NosGM account.

The launcher detects Steam, prepares the two local files, requests a one-use ticket, starts `NostaleClientX_NosGM.exe gf 5`, and passes `_NC_AUTH_CODE`, `_NC_INSTALLATION_ID` and the selected Steam language only to that child process.

## Remove the prepared Steam copy

Stop the game and delete only the generated files:

```powershell
$client = "E:\steam\steamapps\common\NosTale"
Remove-Item (Join-Path $client "NostaleClientX_NosGM.exe") -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $client "noscore_gf.dll") -Force -ErrorAction SilentlyContinue
```

Do not delete or rename the original `NostaleClientX.exe`. Pressing **Play** later recreates the NosGM copy from the current Steam version.

## Failure map

| Symptom | Likely stage |
|---|---|
| NativeAOT build fails | .NET 10 or Visual C++ build tools are missing |
| Client preparation reports ambiguous IP constants | Client layout changed; do not force the patch |
| Access denied while synchronizing InstallationId | Run the launcher once elevated |
| Patched process exits immediately | Stub export/import compatibility or client update |
| Client opens but Login rejects it | NoS0577 token, InstallationId, region, version or MD5 validation |
| Server list appears but World rejects entry | Login-to-World permit flow |

Collect sanitized diagnostics after a failed attempt:

```powershell
.\scripts\collect-modern-login-diagnostics.ps1
```

The diagnostics collector must never include the patched executable, raw ticket, password, account name, InstallationId or client packet body.

## Attribution

The Login-address discovery, equal-length wrapper import rewrite and `Steam_*` compatibility-stub contract are adapted from `NosCoreIO/NosCore.DeveloperTools` revision `39e2cd2085ff7fc7250966d58893a95262157113` under the MIT License. Attribution and the complete permission notice are preserved in `Launcher/NOTICE.md` and `THIRD_PARTY_NOTICES.md`.
