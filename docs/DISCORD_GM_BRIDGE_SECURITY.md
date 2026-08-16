# Discord GM Bridge Security

The Discord GM bridge is disabled unless `NOSGM_GM_BRIDGE_ENABLED=true`.

When enabled, the World Server now requires **two independent checks** before any command can execute:

1. The request must have a valid HMAC-SHA256 signature, timestamp, and one-use nonce.
2. The Discord user ID carried by the signed request must exist in a server-local allowlist with enough privilege for the requested command.

The second check is intentionally independent from Discord role claims sent by the bot. A stolen bridge HMAC secret therefore does not automatically grant GM privileges unless the attacker also uses an allowlisted Discord user ID. Rotate the HMAC secret immediately if exposure is suspected.

## Required environment variables

Do not commit real values to Git.

```text
NOSGM_GM_BRIDGE_ENABLED=true
NOSGM_GM_BRIDGE_SECRET=<high-entropy secret, 48-512 characters>
NOSGM_GM_BRIDGE_PREFIX=http://127.0.0.1:8787/
```

At least one of these server-side actor allowlists must also be configured. Values are Discord **user IDs**, not Discord role IDs. Multiple IDs may be separated by commas, semicolons, spaces, or new lines.

```text
NOSGM_GM_BRIDGE_HELPER_IDS=<discord-user-id,...>
NOSGM_GM_BRIDGE_MODERATOR_IDS=<discord-user-id,...>
NOSGM_GM_BRIDGE_ADMIN_IDS=<discord-user-id,...>
NOSGM_GM_BRIDGE_OWNER_IDS=<discord-user-id,...>
```

If the same Discord user ID appears in multiple lists, the highest role wins. If the bridge is enabled with no valid actor allowlist, the World Server refuses to start the bridge.

## Command permissions

| Minimum role | Commands |
| --- | --- |
| Helper | `status`, `players`, `player`, `server`, `whisper`, `link-challenge` |
| Moderator | Helper commands plus `position`, `history`, `unstuck`, `kick`, `teleport`, `mute`, `unmute` |
| Admin | Moderator commands plus `inventory`, `announce`, `ban`, `unban` |
| Owner | All allowlisted commands. `give-item` and `shutdown` still remain disabled in the bridge implementation. |

Unknown commands and unknown Discord user IDs are denied by default.

## Secret rotation

Normal rotation is safest when the bot and World Server can switch to the new key together: replace `NOSGM_GM_BRIDGE_SECRET` and restart the components.

For a short zero-downtime migration, the World Server can temporarily accept one previous secret:

```text
NOSGM_GM_BRIDGE_SECRET=<new-secret>
NOSGM_GM_BRIDGE_PREVIOUS_SECRET=<old-secret>
NOSGM_GM_BRIDGE_PREVIOUS_SECRET_EXPIRES_UNIX=<future-unix-time>
```

The previous key expiry must be in the future and no more than **3600 seconds** from World Server startup. Remove both `NOSGM_GM_BRIDGE_PREVIOUS_SECRET` and `NOSGM_GM_BRIDGE_PREVIOUS_SECRET_EXPIRES_UNIX` after the bot has switched to the new key.

Never reuse an exposed secret.

## Network boundary

The bridge only accepts an exact HTTP listener rooted at `127.0.0.1` or `localhost`. Do not forward port `8787` to the public Internet. If the Discord bot runs on another host, use a private authenticated transport such as a VPN/private network and keep the World listener local behind a narrowly scoped proxy.

## Audit trail

Bridge decisions are written to `logs/discord-gm-audit.jsonl`. Entries include the Discord user ID, resolved server-side role, command, result, and arguments. Penalty attribution uses the allowlisted Discord user ID rather than the display tag.

Treat this log as sensitive administrative data.

## CI protections

`validate-discord-gm-bridge-security.yml` verifies the bridge's deny-by-default and authorization contracts on every pull request to `main`.

`secret-guard.yml` also rejects several high-confidence credential, exfiltration, and download/execute patterns before merge.
