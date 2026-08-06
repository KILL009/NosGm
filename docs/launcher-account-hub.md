# NosGM launcher account hub

The launcher account hub is the first slice of the account and community phase. It lets a player choose which account name is prepared before pressing Play without creating a persistent authenticated session.

## Player flow

A `Mi cuenta` button appears in the launcher title bar.

The window shows:

- the currently prepared account name;
- whether modern authentication or classic mode is configured;
- up to five recently authenticated account names;
- actions to select a recent account, forget it or enter another account next time.

Selecting an account only pre-fills the existing login dialog. The password is still requested when the player presses Play.

## Storage boundary

The launcher settings may contain:

- the current account name;
- at most five recent account names.

The account hub never stores:

- passwords;
- authorization codes;
- access tickets;
- cookies;
- session identifiers;
- process environment variables;
- authentication responses.

Account names are normalized, bounded to 255 characters, rejected when they contain control characters and deduplicated case-insensitively.

## Authentication lifecycle

The existing modern authentication flow remains authoritative:

1. The login dialog asks for the selected account name and password.
2. `LauncherAuthenticationClient` requests a short-lived one-use authorization code.
3. The game receives the code through the existing Gameforge pipe or Steam-compatible stub.
4. A successful launch raises the existing `GameLaunched` event.
5. The account hub adds only the returned canonical account name to the bounded history.
6. The existing launcher settings write persists the account name history.

There is no reusable launcher login session to terminate. `Usar otra cuenta` clears the prepared account name, while `Olvidar seleccionada` also removes that name from local history.

## Failure isolation

Account history is a convenience feature. It cannot block:

- launcher startup;
- update or repair;
- authentication;
- game launch;
- Discord Rich Presence;
- diagnostics.

The account window does not call the authentication service and does not launch the game.

## Acceptance test

1. Start the launcher with modern authentication enabled.
2. Confirm the title bar shows `Mi cuenta` when no account is prepared.
3. Press Play, authenticate successfully and close the game.
4. Confirm the title bar now shows the canonical account name.
5. Open `Mi cuenta` and confirm the name appears once in recent accounts.
6. Select `Usar otra cuenta` and confirm the next login dialog starts empty.
7. Authenticate with a second account and confirm both names appear, newest first.
8. Use `Olvidar seleccionada` and confirm the selected name disappears.
9. Inspect `%LOCALAPPDATA%\NosGM\Launcher\settings.json` and confirm it contains account names only, never a password or authorization ticket.
10. Repeat successful logins and confirm the history never exceeds five unique names.
