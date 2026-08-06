# NosGM Companion Mode

NosGM Companion keeps the launcher alive while the authorized game client is running. It preserves launcher-owned Discord Rich Presence, monitors the signed public event calendar and can display Windows tray notifications.

## Player flow

1. Open the launcher.
2. Press `🔔 Alertas`.
3. Enable `Mantener NosGM en la bandeja al iniciar el juego`.
4. Choose event and maintenance alerts.
5. Choose a reminder window of 5, 10, 15, 30 or 60 minutes.
6. Save and press `JUGAR`.
7. NosGM hides to the Windows tray after the game starts.
8. Double-click the tray icon to restore the launcher.
9. Right-click the tray icon to open NosGM, configure alerts or exit completely.

When the game exits, the launcher returns automatically when `Volver a mostrar el launcher` is enabled.

## Public event watch

The companion reuses the existing bounded `LauncherLiveOperationsClient` and reads only:

```text
GET /api/v1/public/operations
GET /api/v1/public/status
```

It can notify about:

- an event entering the configured reminder window;
- an event that has just started;
- upcoming maintenance;
- active maintenance.

Only one highest-priority notification is delivered per polling cycle. Delivered keys are remembered so the same event is not shown repeatedly.

## Local files

Preferences remain in:

```text
%LOCALAPPDATA%\NosGM\Launcher\settings.json
```

Bounded notification state is stored in:

```text
%LOCALAPPDATA%\NosGM\Launcher\event-alert-state.json
```

The alert state contains only:

- public event or maintenance keys;
- the UTC update time;
- an optional mute expiration.

It retains at most 200 keys and expires after 14 days.

## Privacy boundary

Companion Mode never reads or stores:

- account names;
- passwords;
- authorization codes or tickets;
- session identifiers;
- character names, maps, coordinates or inventory;
- chat or packets from the client;
- process environment variables.

The event monitor consumes the same public portal data already shown by the launcher. It does not inject code, read client memory or inspect network traffic.

## Windows tray boundary

The tray icon uses the Windows `Shell_NotifyIconW` API directly. No third-party tray package or native binary is added.

Closing the launcher while the tracked game process is active hides it instead of terminating the companion. `Salir completamente` explicitly bypasses that behavior and removes the tray icon before shutdown.

## Failure behavior

- portal failures never interrupt login or game launch;
- invalid public payloads produce no notification;
- tray notification failure leaves the launcher usable;
- optional alert-history write failures do not reverse a delivered notification;
- launcher shutdown cancels polling and removes the tray icon;
- game process exit handlers are detached before disposal.

## Acceptance test

1. Start the local stack and open `🔔 Alertas`.
2. Press `Probar aviso` and confirm a Windows notification appears.
3. Enable Companion Mode and save.
4. Press `JUGAR` and confirm the launcher hides to the tray.
5. Double-click the tray icon and confirm the launcher returns.
6. Start the game again, right-click the tray icon and open alert settings.
7. Close the game and confirm the launcher restores automatically.
8. Publish an event inside the selected reminder window and confirm one notification appears.
9. Wait through another polling cycle and confirm the same alert is not repeated.
10. Confirm `event-alert-state.json` contains no private account or game data.
11. Start the game, close the launcher window and confirm Companion remains in the tray.
12. Choose `Salir completamente` and confirm the process and tray icon disappear.
