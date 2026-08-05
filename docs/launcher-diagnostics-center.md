# NosGM launcher diagnostics center

The launcher footer now opens a native diagnostics center instead of sending every player directly to a generic support page.

The diagnostics center is designed for installed player launchers. It does not depend on the NosGM source repository, developer PowerShell scripts or administrator privileges.

## Checks

The first diagnostics slice checks:

- installation folder existence and reparse-point safety;
- authorized game executable existence, size and file version;
- write permission required by transactional updates;
- free disk space;
- modern authentication configuration and HTTPS/loopback policy;
- official Discord Rich Presence Application ID;
- public portal availability and valid bounded JSON;
- Master TCP reachability on port `4545`;
- World TCP reachability on port `1337`;
- Spanish Login TCP reachability on port `4005`.

A portal or TCP warning does not prevent the launcher from opening the game. The center explains which component failed and gives one suggested action.

## Support bundle

The player can export a ZIP containing:

```text
launcher-diagnostics.json
launcher-diagnostics.txt
settings-summary.json
client-fingerprint.json
privacy.txt
```

The client fingerprint contains the authorized executable file name, length, version, last-write time and SHA-256 hash. This allows support to distinguish a damaged or unexpected client without uploading the executable itself.

The bundle never includes the saved account name, passwords, authorization codes, tickets, Discord secrets, full process environment blocks, chat messages, exact game coordinates or complete launcher settings.

Windows profile paths are replaced with:

```text
C:\Users\<user>
```

The safe settings summary contains only language, sanitized installation path, executable name, authentication enabled state, selected transport, public server address, public portal address, Rich Presence preference and close-after-launch preference.

## Network safety

Portal diagnostics use only:

```text
GET /api/v1/public/status
```

The diagnostics center does not call the ticket endpoint and does not submit account credentials.

The HTTP client:

- refuses redirects;
- stores no cookies;
- validates certificate revocation;
- limits the response to 256 KiB;
- uses a short timeout;
- accepts remote HTTPS or local loopback HTTP according to normal launcher settings validation.

TCP checks open short-lived connections only to the already configured Master, World and Login ports.

## Player workflow

1. Open the NosGM launcher.
2. Press `Diagnóstico` in the footer.
3. Wait for the checks to finish.
4. Read the suggested action beside warnings or failures.
5. Press `Exportar ZIP para soporte` when evidence is needed.
6. Choose where to save the ZIP.
7. Review or send that ZIP to authorized NosGM support.

## Relationship to developer diagnostics

The repository also contains `collect-modern-login-diagnostics.ps1`, which collects sanitized server-process evidence for local developers. The native launcher diagnostics center is intentionally separate and smaller:

- player launcher diagnostics inspect the installed client and public connectivity;
- developer diagnostics inspect the locally orchestrated Master, World, Login and portal processes;
- neither collector includes credentials or raw modern-login packets.

Future slices can add DirectX/runtime inspection, updater transaction history, one-click repair recommendations and an optional support-case upload after a separately authenticated and consented flow exists.
