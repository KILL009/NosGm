# NosGM launcher smart repair

The diagnostics center can verify and repair the installed client without replacing the whole folder.

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

A published launcher uses only the release channel and public key compiled into that build. A source-built launcher normally contains the disabled `.invalid` placeholder. During the integrated local stack, NosGM now creates a development-only signed channel so the repair workflow can be tested without weakening the published launcher.

## Source-built local channel

The local development channel is created only when all of these conditions are true:

- the launcher still contains the `UNCONFIGURED` placeholder;
- the configured portal is loopback HTTP;
- the authorized game executable exists;
- the portal runs with `ASPNETCORE_ENVIRONMENT=Development`.

On the first eligible startup, the launcher:

1. copies the configured game executable into a private per-user repair snapshot;
2. creates an ECDSA P-256 signing key;
3. signs a one-file development manifest;
4. stores only the public key and signed manifest;
5. serves the manifest and content through the loopback portal.

The private key exists only in memory and is never written to disk. The signed local snapshot is reused on following source-built runs instead of silently replacing its trust root.

Local files are stored under:

```text
%LOCALAPPDATA%\NosGM\Launcher\local-repair-channel
```

The portal exposes them only in Development and only when both ends of the connection are loopback:

```text
http://127.0.0.1:<portal-port>/local-update/release-manifest.json
http://127.0.0.1:<portal-port>/local-update/content/<managed-file>
```

Requests cannot use path traversal, reparse points, redirects, cookies or remote addresses. Responses are marked `no-store,private`.

This local channel is a smoke-test and recovery source for the configured executable. It is not a replacement for the official production CDN, offline signing key or complete client release manifest.

A published launcher ignores the local configuration because its compiled official channel takes precedence and remains HTTPS-only.

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

Smart repair does not introduce an arbitrary download mechanism. It uses `LauncherController.CheckAndApplyAsync` with the already hardened components:

- trusted channel configuration;
- remote HTTPS and certificate revocation checks;
- loopback-only HTTP for the source-built development channel;
- signed manifest validation;
- bounded manifest parsing;
- safe managed paths;
- SHA-256 file verification;
- install lock;
- transaction staging;
- rollback and startup recovery.

The UI never downloads an arbitrary URL supplied by a diagnostic result. Environment variables cannot replace the trusted update root.

## Acceptance test

1. Start the complete local stack from the smart-repair branch without `-SkipBuild`.
2. Wait a few seconds for the launcher to prepare the local signed channel.
3. Open Diagnostics and confirm the repair button is enabled.
4. Press repair with a healthy client and confirm `Todos los archivos ya estaban correctos`.
5. Close the launcher and make a backup copy of the configured game executable.
6. Move that executable out of the client folder.
7. Restart the local stack and press repair again.
8. Confirm only the configured executable is restored and diagnostics rerun.
9. Confirm the history line displays the last repair without a path or account name.
10. Restore the backup only if the test did not finish successfully.
