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
10. Return to character selection and enter another character at least three more times without closing the client.
11. Disconnect normally, launch again and confirm that a new authorization ticket and active session are created.

Do not run the launcher or client as administrator. The named pipe is restricted to the current Windows user, and elevation would create a different security boundary.

## 4. Expected security tests

After one successful login:

1. enter an invalid password and confirm a generic rejection;
2. retry invalid credentials until the configured rate limit returns HTTP `429`;
3. confirm that launcher settings remember only the account name when requested;
4. confirm that no password, authorization code or ticket appears in `settings.json`;
5. stop and restart the stack, then confirm that the old active-session authorization cannot recreate a disconnected Master session.

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
| Server list appears but channel entry fails | Initial World frame, Login session registration or World permit | `[WORLD_HANDSHAKE]` and `[WORLD_ENTRY]` milestones in the World log tail |
| Character screen appears but entry disconnects | One-use World permit, account/session/IP binding | World and Master log tails |
| First entries work but a later reselection fails | stale Master registration, expired active-session lease or permit issuance | Login entry counter, Master account/session tuple and authentication log tail |

The World log emits only bounded metadata and stable reason codes; it never emits the raw handshake or entry packet. Follow one `ClientId` from `TCP_CONNECTED` through these milestones:

| Last milestone or code | Meaning |
| --- | --- |
| `INITIAL_FRAME_BUFFERED` | World received bytes but has not found the initial `0x0E` terminator |
| `INITIAL_FRAME_SPLIT` | The custom parameter and any coalesced encrypted tail were separated |
| `FRAME_TOO_SHORT` | A one- or two-byte transport remainder reached the legacy ingress filter |
| `SESSION_ESTABLISHED` | The initial World custom parameter produced a valid session identifier |
| `ENTRY_PACKET_WAIT_STARTED` | World is waiting for the two encrypted entry-packet parts |
| `ENTRY_PACKET_ASSEMBLED` | All entry parts reached the handler |
| `LOGIN_NOT_PERMITTED` | Master has no matching account/session registration; if Login records different SessionIDs for consecutive `Entry=` values, the deployed binaries predate the stable-session fix |
| `GAMEFORGE_AUTH_SERVICE_UNAVAILABLE` | World could not authenticate its permit-consumer role |
| `GAMEFORGE_WORLD_PERMIT_INVALID` | The one-use permit was missing, expired, already consumed or did not match |
| `GAMEFORGE_WORLD_PERMIT_ACCEPTED` | The passwordless World permit was consumed successfully |
| `CHARACTER_LIST_SENT` | World completed entry and returned the character list |

The dedicated `nosgm-world-handshake.log` uses immediate flush so the collector
can read these events while World is still running. Its first record must be
`DIAGNOSTICS_READY Revision=20260728.4`; if that record is absent, the deployed
`NosGm.Core.dll` does not contain this diagnostic revision.

For one launcher authorization, Login must log a growing entry counter with the
same `SessionID`:

```text
Auth=NoS0577 Entry=1
Auth=NoS0577 Entry=2
Auth=NoS0577 Entry=3
Auth=NoS0577 Entry=4
Auth=NoS0577 Entry=5
```

Entry one owns the Master account/session registration and converts the short
launcher authorization into a bounded 24-hour active-session lease. Every later
entry must find that exact account/session tuple already registered; otherwise
Login rejects the stale continuation instead of recreating a disconnected
session. Each accepted entry issues its own temporary, IP-bound, one-use World
permit because returning to character selection creates a new World connection
after the previous permit has already been consumed. Different SessionIDs across
those lines indicate that Master, Login or `NosGm.Master.Library.dll` was not
rebuilt or copied consistently.

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
- repository dirty state and SHA-256 fingerprints for the launched executable plus the fixed NosGM diagnostic module allowlist;
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
- at least five modern Login entries reuse one SessionID;
- each entry receives a fresh one-use World permit;
- stopping the session prevents the old authorization from recreating it;
- a new launcher login succeeds;
- invalid credentials remain generic and rate-limited;
- no credential material is persisted.
