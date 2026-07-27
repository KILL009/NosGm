# Modern Login real-client acceptance test

This procedure validates the complete authorized path:

```text
NosGM Launcher
    -> Master LauncherAuthBridge
    -> Login NoS0576 / NoS0577
    -> server and channel selection
    -> one-use World permit
    -> World character entry
```

The automated build proves that the components compile and that their contracts agree. This acceptance test proves that the authorized Windows client performs the same handshake in a real process.

## 1. Start the local stack

The first start requires an elevated PowerShell window for the one-time URL reservation:

```powershell
./scripts/start-modern-login-local.ps1 -ConfigureUrlAcl
```

Later starts use an ordinary PowerShell window:

```powershell
./scripts/start-modern-login-local.ps1 -SkipBuild
```

The startup command leaves the stack running even when readiness finds a launcher or client configuration blocker. Fix the reported item and rerun the readiness command without restarting the server processes.

## 2. Run the readiness inspector

```powershell
./scripts/test-modern-login-readiness.ps1 -RequireLauncher
```

The inspector writes:

```text
artifacts/modern-login-local/readiness.json
```

A clean first test should pass these areas:

- one recorded Master, World and Login process;
- launcher process when `-RequireLauncher` is used;
- Master `4545`, World `1337` and Spanish Login `4005` accepting TCP connections;
- loopback AuthBridge health response reporting ten regions;
- launcher settings containing no credential-shaped properties;
- configured `NostaleClientX.exe` present;
- Spanish selected as region `5`;
- shared Gameforge `InstallationId` present or ready to be created by the launcher.

A missing `InstallationId` before the first press of **Play** is only a warning. The launcher creates it in the current-user Gameforge registry key before starting the client.

## 3. Perform the Spanish client test

1. Select **Español** in NosGM Launcher.
2. Press **Jugar**.
3. Enter the exact NosGM account name and its current password.
4. Confirm that `NostaleClientX.exe` opens.
5. Confirm that the server and channel list appears.
6. Select the channel.
7. Confirm that the character selection screen appears.
8. Enter one character.
9. Confirm that the map loads and the character remains connected.
10. Disconnect normally and repeat once to prove that a new ticket and World permit are issued for the next session.

Do not run the launcher or client as administrator. The named pipe is restricted to the current Windows user, and elevation would create a different security boundary.

## 4. Expected security tests

After one successful login:

1. enter an invalid password and confirm a generic rejection;
2. retry invalid credentials until the configured rate limit returns HTTP `429`;
3. confirm that launcher settings remember only the account name when requested;
4. confirm that no password, authorization code or ticket appears in `settings.json`;
5. stop and restart the stack, then confirm that old one-use tickets cannot be reused.

The local defaults allow ten attempts per account and IP in sixty seconds. Do not perform the rate-limit test against a production endpoint.

## Failure map

| Visible symptom | Most likely stage | What to inspect |
| --- | --- | --- |
| Master port `4545` is closed | Master startup or database initialization | Master console and readiness report |
| Health endpoint fails | Master AuthBridge or URL ACL | `AuthBridge.Health` check and Master log tail |
| Launcher opens but no credential dialog appears | Runtime endpoint was not inherited | Launcher process record and `NOSGM_AUTH_ENDPOINT` startup path |
| Credentials are always rejected | Account casing, password, ban or maintenance | HTTP result and sanitized Master log tail |
| Client does not open | Client path or launcher process creation | `Client.Executable` check and launcher summary |
| Client opens and immediately closes | Named-pipe session or `_TNT_*` environment mismatch | Launcher checks and sanitized evidence bundle |
| Server list is empty | Login region, client version or `NoS0576/NoS0577` parsing | Spanish port `4005`, Login log tail and client version |
| Server list appears but channel entry fails | Login session registration or server-list packet | Login and Master log tails |
| Character screen appears but entry disconnects | One-use World permit, account/session/IP binding | World and Master log tails |
| First entry works but reconnect fails | stale state, reused permit or shutdown residue | stop script, process identity checks and a fresh readiness report |

## 5. Collect a sanitized evidence bundle

When any stage fails, keep the stack running and execute:

```powershell
./scripts/collect-modern-login-diagnostics.ps1
```

The command creates a ZIP under:

```text
artifacts/modern-login-diagnostics/
```

The bundle includes:

- readiness results;
- sanitized PID and port metadata;
- OS, PowerShell, .NET SDK and repository commit versions;
- client presence and file version without the installation path;
- bounded tails of process logs.

Before compression the collector redacts or omits:

- passwords and authorization codes;
- raw `NoS0576`, `NoS0577` and `NsTeST` payloads;
- authentication keys, tokens and secret-looking long values;
- account names, account identifiers and email addresses;
- `InstallationId`, session GUID values and registry contents;
- non-loopback IP addresses;
- Windows profile names and complete local paths.

The collector never reads the environment blocks of running processes.

## 6. Stop cleanly

```powershell
./scripts/stop-modern-login-local.ps1
```

The stop command validates process name and original start time before terminating each recorded PID.

## Result definition

The modern Login acceptance test is successful only when all of these are true:

- readiness has no failed checks;
- the authorized client opens through the launcher;
- the server and channel list appears through Spanish region `5`;
- character selection loads;
- World accepts the character and keeps the session connected;
- a second fresh login succeeds;
- invalid credentials remain generic and rate-limited;
- no credential material is persisted.
