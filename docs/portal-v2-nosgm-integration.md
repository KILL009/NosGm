# NosGM Portal 2.0 integration

This document describes the Portal 2.0 bridge for the current `NosGm.*` source tree. It replaces the legacy `Frostvein.*` integration shipped in the original Portal 2.0 package.

## What is integrated

- `MallPacketHandler` creates a 90-second HMAC-SHA256 game-login ticket when the client opens NosMall.
- `ShopDeliveryWorker` claims committed purchases through `shop.ProcessNextDelivery` and immediately adds the resulting parcel to an online character's mailbox.
- `PortalBridgeWorker` publishes live character locations and executes only the allowlisted structured actions `announce`, `kick`, and `teleport`.
- `PortalWorldEntryPoint` runs the existing `Program.Main` first and starts the SQL workers only after the World Server has received a positive channel id.
- The integration uses `Directory.Build.targets` so the large legacy `.csproj` files do not need installer-generated edits.

## Security model

Secrets and direct SQL connection strings are read only from the World Server process environment. They are not stored in `app.config` or committed to Git.

Do not use `sa`. Create the least-privilege logins from the Portal 2.0 SQL package. The workers reject an explicit `sa` SQL login.

`NOSGM_SHOP_URL` must be HTTPS. Plain HTTP is accepted only for exact `localhost` or `127.0.0.1` development URLs.

The value of `NOSGM_SHOP_TICKET_SECRET` in the World Server must be the same secret used by the Portal backend to validate game tickets (`STORE_TICKET_SECRET` in the Portal 2.0 package).

## World Server environment

| Variable | Required | Purpose |
| --- | --- | --- |
| `NOSGM_SHOP_ENABLED` | for delivery | Set to `true` to start the purchase delivery worker. |
| `NOSGM_SHOP_URL` | for mall button | Public Portal/NosMall base URL. HTTPS outside loopback. |
| `NOSGM_SHOP_TICKET_SECRET` | for mall button | High-entropy HMAC secret, 32 to 512 characters. |
| `NOSGM_SHOP_SQL_CONNECTION_STRING` | for delivery | Direct least-privilege SQL Server connection string for `shop.ProcessNextDelivery`. |
| `NOSGM_SHOP_SYSTEM_SENDER_ID` | for delivery | Existing service character id used as parcel sender. |
| `NOSGM_SHOP_POLL_MS` | no | Delivery polling delay, default `2000`, allowed `500` to `30000`. |
| `NOSGM_PORTAL_BRIDGE_ENABLED` | for GM bridge | Set to `true` on each World channel that should serve Portal commands. |
| `NOSGM_PORTAL_SQL_CONNECTION_STRING` | for GM bridge | Direct least-privilege SQL Server connection string for the `portal.*` procedures. |

Example for a local development process:

```powershell
$env:NOSGM_SHOP_ENABLED = 'true'
$env:NOSGM_SHOP_URL = 'http://127.0.0.1:3000'
$env:NOSGM_SHOP_TICKET_SECRET = '<generate-a-random-secret>'
$env:NOSGM_SHOP_SQL_CONNECTION_STRING = '<least-privilege-shop-connection>'
$env:NOSGM_SHOP_SYSTEM_SENDER_ID = '12345'
$env:NOSGM_PORTAL_BRIDGE_ENABLED = 'true'
$env:NOSGM_PORTAL_SQL_CONNECTION_STRING = '<least-privilege-portal-bridge-connection>'
```

Never commit real values from the placeholders above.

## Database order

Test against a database copy first. Apply the Portal 2.0 scripts in their numeric order and run `006_verify.sql` before enabling either World worker. The integration expects at least:

- `shop.ProcessNextDelivery`
- `portal.LivePositionBatch`
- `portal.UpsertLiveCharacterStateBatch`
- `portal.ClaimNextGmCommand`
- `portal.CompleteGmCommand`

NosGM's existing `Database/Migrations/20260720_MailDeliveryOperation.sql` should also be applied so parcel claims keep the current item-delivery audit infrastructure available.

## Build

Build the current World project, not a `Frostvein.World` project:

```powershell
msbuild .\Data\NosGm.Program\NosGm.World\NosGm.World.csproj /m /p:Configuration=Release /p:Platform=x64
```

The current target is .NET Framework 4.8.1.

## Smoke test

1. Start Portal 2.0 against a staging database and confirm its health checks.
2. Start Master/Login/World with the environment variables above.
3. Confirm the World log contains `NosMall delivery worker started.` and/or `NosGM Portal bridge started` for the enabled components.
4. Log in with a test account and press the NosMall button. The URL must contain a short-lived signed ticket and the Portal must consume it once.
5. Make a zero-risk test purchase and verify exactly one `dbo.Mail` row and one `shop.DeliveryReceipt` are created.
6. Claim the parcel, relog, and verify the item is not duplicated.
7. From the Portal GM panel, test `announce`, then `teleport` on a staging character, then `kick`.
8. Restart the World Server and confirm pending deliveries resume without duplicate mail.

## Legacy installer replacement

Use:

```powershell
.\scripts\portal-v2\install-shop.ps1 -NosGmRoot C:\path\to\NosGm
.\scripts\portal-v2\install-portal-bridge.ps1 -NosGmRoot C:\path\to\NosGm
```

These scripts target `NosGm.Handler` and `NosGm.World`, never write SQL credentials into `app.config`, and create timestamped backups of the build-target files they change.
