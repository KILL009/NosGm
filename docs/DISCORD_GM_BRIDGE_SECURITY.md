# Discord GM Bridge Security

The Discord GM bridge is disabled unless `NOSGM_GM_BRIDGE_ENABLED=true`.

When enabled, the World Server requires **three gates** before any GM command can execute:

1. A gateway HMAC-SHA256 signature authenticates the request transport.
2. A second HMAC-SHA256 signature, made with a different secret, cryptographically binds the Discord actor ID to the exact request body.
3. The authenticated Discord user ID must exist in a server-local allowlist with enough privilege for the requested command.

The two HMAC secrets must be different. Possession of only `NOSGM_GM_BRIDGE_SECRET` is therefore insufficient to forge an authenticated actor, even if an allowlisted Discord user ID is publicly known. Likewise, possession of only the identity secret is insufficient because the gateway signature is checked first.

This does **not** protect against compromise of both secrets or full compromise of the Discord bot host. If either secret may have leaked, rotate it immediately; if the bot host may be compromised, rotate both and investigate the host before re-enabling the bridge.

## Required environment variables

Do not commit real values to Git.

```text
NOSGM_GM_BRIDGE_ENABLED=true
NOSGM_GM_BRIDGE_SECRET=<gateway high-entropy secret, 48-512 characters>
NOSGM_GM_BRIDGE_IDENTITY_SECRET=<different identity high-entropy secret, 48-512 characters>
NOSGM_GM_BRIDGE_PREFIX=http://127.0.0.1:8787/
```

The gateway and identity secrets must be distinct.

At least one of these server-side actor allowlists must also be configured. Values are Discord **user IDs**, not Discord role IDs. Multiple IDs may be separated by commas, semicolons, spaces, or new lines.

```text
NOSGM_GM_BRIDGE_HELPER_IDS=<discord-user-id,...>
NOSGM_GM_BRIDGE_MODERATOR_IDS=<discord-user-id,...>
NOSGM_GM_BRIDGE_ADMIN_IDS=<discord-user-id,...>
NOSGM_GM_BRIDGE_OWNER_IDS=<discord-user-id,...>
```

If the same Discord user ID appears in multiple lists, the highest role wins. If the bridge is enabled with no valid actor allowlist, the World Server refuses to start the bridge.

## Request authentication protocol

The Discord bot must serialize the request body once and sign that **exact raw JSON string**. Do not reformat or reserialize the body after computing either signature.

For a request with:

- `timestamp`: the decimal Unix timestamp sent in `X-NosGM-Timestamp`
- `nonce`: the one-use nonce sent in `X-NosGM-Nonce`
- `discordUserId`: `actor.discordUserId` from the JSON body
- `rawBody`: the exact UTF-8 JSON body sent over HTTP

compute the gateway canonical value as:

```text
<timestamp>\n<nonce>\n<rawBody>
```

Then compute:

```text
X-NosGM-Signature = hex_lower(HMAC_SHA256(NOSGM_GM_BRIDGE_SECRET, gatewayCanonical))
```

Compute the independent actor canonical value as:

```text
nosgm-actor-v1\n<timestamp>\n<nonce>\n<discordUserId>\n<rawBody>
```

Then compute:

```text
X-NosGM-Actor-Signature = hex_lower(HMAC_SHA256(NOSGM_GM_BRIDGE_IDENTITY_SECRET, actorCanonical))
```

The request must include all four headers:

```text
X-NosGM-Timestamp: <unix-seconds>
X-NosGM-Nonce: <16-100 character one-use nonce>
X-NosGM-Signature: <64 lowercase/uppercase hex characters>
X-NosGM-Actor-Signature: <64 lowercase/uppercase hex characters>
```

The World Server verifies the gateway signature before trusting/parsing actor data, verifies the actor signature next, consumes the nonce only after both signatures succeed, then applies the server-local role allowlist. A request is executed only after all of those checks pass.

### Bot-side pseudocode

```text
rawBody = serializeJsonOnce(request)
timestamp = currentUnixSeconds()
nonce = cryptographicallyRandomNonce()

gatewayCanonical = timestamp + "\n" + nonce + "\n" + rawBody
actorCanonical = "nosgm-actor-v1\n" + timestamp + "\n" + nonce + "\n" + request.actor.discordUserId + "\n" + rawBody

gatewaySignature = hmacSha256Hex(GATEWAY_SECRET, gatewayCanonical)
actorSignature = hmacSha256Hex(IDENTITY_SECRET, actorCanonical)

POST /v1/commands
  X-NosGM-Timestamp: timestamp
  X-NosGM-Nonce: nonce
  X-NosGM-Signature: gatewaySignature
  X-NosGM-Actor-Signature: actorSignature
  body: rawBody
```

The bot source is not part of this repository, so the bot must be updated separately before this hardened bridge can be used. Until then, leave the bridge disabled. There is intentionally no compatibility bypass for single-signature requests.

## Command permissions

| Minimum role | Commands |
| --- | --- |
| Helper | `status`, `players`, `player`, `server`, `whisper`, `link-challenge` |
| Moderator | Helper commands plus `position`, `history`, `unstuck`, `kick`, `teleport`, `mute`, `unmute` |
| Admin | Moderator commands plus `inventory`, `announce`, `ban`, `unban` |
| Owner | All allowlisted commands. `give-item` and `shutdown` still remain disabled in the bridge implementation. |

Unknown commands and unknown Discord user IDs are denied by default.

## Secret rotation

Normal rotation is safest when the bot and World Server can switch both keys together: replace `NOSGM_GM_BRIDGE_SECRET` and `NOSGM_GM_BRIDGE_IDENTITY_SECRET`, update the bot, and restart the components.

For a short zero-downtime migration, the World Server can temporarily accept one complete previous key generation:

```text
NOSGM_GM_BRIDGE_SECRET=<new-gateway-secret>
NOSGM_GM_BRIDGE_IDENTITY_SECRET=<new-identity-secret>
NOSGM_GM_BRIDGE_PREVIOUS_SECRET=<old-gateway-secret>
NOSGM_GM_BRIDGE_PREVIOUS_IDENTITY_SECRET=<old-identity-secret>
NOSGM_GM_BRIDGE_PREVIOUS_SECRET_EXPIRES_UNIX=<future-unix-time>
```

Both previous secrets must be configured together, all four configured secrets must be different, and the previous generation may remain valid for at most **3600 seconds** from World Server startup. A request authenticated with the previous gateway key must also use the previous identity key; current and previous generations cannot be mixed.

Remove `NOSGM_GM_BRIDGE_PREVIOUS_SECRET`, `NOSGM_GM_BRIDGE_PREVIOUS_IDENTITY_SECRET`, and `NOSGM_GM_BRIDGE_PREVIOUS_SECRET_EXPIRES_UNIX` after the bot has switched to the new key generation.

Never reuse an exposed secret.

## Network boundary

The bridge only accepts an exact HTTP listener rooted at `127.0.0.1` or `localhost`. Do not forward port `8787` to the public Internet. If the Discord bot runs on another host, use a private authenticated transport such as a VPN/private network and keep the World listener local behind a narrowly scoped proxy.

## Audit trail

Bridge decisions are written to `logs/discord-gm-audit.jsonl`. Entries include the Discord user ID, resolved server-side role, command, result, and arguments after full dual-signature authentication. Authentication failures do not trust request actor/argument data for audit attribution. Penalty attribution uses the authenticated, allowlisted Discord user ID rather than the display tag.

Treat this log as sensitive administrative data.

## CI protections

`validate-discord-gm-bridge-security.yml` verifies dual-signature ordering, actor binding, deny-by-default authorization, role tiers, bounded rotation, disabled destructive commands, and the absence of obvious shell/download primitives on every pull request to `main`.

`secret-guard.yml` also rejects several high-confidence credential, exfiltration, and download/execute patterns before merge.
