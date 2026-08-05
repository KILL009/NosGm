# NosGM launcher smart repair

The diagnostics center can now verify and repair the installed client without replacing the whole folder.

## Player flow

1. Open the launcher.
2. Press `Diagnóstico`.
3. Press `Verificar y reparar`.
4. Review the confirmation message.
5. Allow the signed verification and transactional repair to finish.
6. The diagnostics center automatically runs its checks again.

## What repair does

Smart repair delegates to the existing signed updater pipeline:

```text
recover interrupted transaction
    -> download signed release manifest
    -> verify ECDSA signature and trusted key
    -> calculate local file hashes
    -> plan only missing or mismatching managed files
    -> download and verify each replacement
    -> stage changes
    -> commit transaction
    -> rollback automatically if commit fails
```

It does not delete the complete NosTale installation and does not reinstall files that already match the trusted manifest.

A launcher build without a configured trusted release channel cannot use smart repair. The button remains disabled in that build.

## Confirmation and cancellation

The launcher asks for explicit confirmation before modifying managed files.

Closing the diagnostics window cancels pending scanning or downloads. The updater's existing transaction journal and recovery system prevents a half-applied client from being treated as complete.

## Repair history

A local bounded history is stored at:

```text
%LOCALAPPDATA%\NosGM\Launcher\repair-history.json
```

Only the latest 25 entries are retained. Each entry may contain:

- UTC time;
- result: up to date, repaired or failed;
- release identifier;
- downloaded file count;
- deleted managed file count;
- downloaded byte count;
- ignored delete count;
- exception type when an attempt fails.

The history does not contain the account name, password, authorization ticket, Discord secret, installation path, file names or process environment variables.

History writes are atomic and optional. A history write failure never changes the result of a successful client repair.

## Security boundary

Smart repair does not introduce a new download mechanism. It uses `LauncherController.CheckAndApplyAsync` with the already hardened components:

- trusted channel configuration;
- HTTPS and certificate revocation checks;
- signed manifest validation;
- bounded manifest parsing;
- safe managed paths;
- SHA-256 file verification;
- install lock;
- transaction staging;
- rollback and startup recovery.

The UI never downloads an arbitrary URL supplied by a diagnostic result.

## Acceptance test

1. Start the launcher from the smart-repair branch.
2. Open Diagnostics and confirm the repair button is enabled.
3. Press repair with a healthy client and confirm `Todos los archivos ya estaban correctos`.
4. Move one non-critical managed test file out of the client folder.
5. Press repair again.
6. Confirm only the missing file is downloaded and the diagnostics rerun.
7. Confirm the history line displays the last repair without a path or account name.
8. Close the launcher during a staged test download and restart it.
9. Confirm normal transaction recovery runs and the client is not left half-applied.
