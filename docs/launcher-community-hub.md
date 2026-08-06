# NosGM launcher community hub

The launcher Community Hub groups public server information in one native window:

- localized news;
- combat, reputation and hero rankings;
- active maintenance;
- active and upcoming calendar events;
- current public player count and server health;
- fixed links to the configured NosGM portal, news page and rankings page.

The former unconfigured `Foro` footer button becomes `🏆 Comunidad`.

## Public data boundary

The window performs only bounded `GET` requests under:

```text
/api/v1/public/status
/api/v1/public/operations
/api/v1/public/news
/api/v1/public/rankings/combat
/api/v1/public/rankings/reputation
/api/v1/public/rankings/hero
```

Remote deployments require HTTPS. Loopback HTTP remains available for the integrated development stack. Redirects and cookies are disabled, certificate revocation is checked and every response is limited to 256 KiB.

The Community Hub never reads or stores:

- account names;
- passwords;
- authorization codes;
- login tickets;
- session identifiers;
- character coordinates;
- inventory data;
- private messages;
- process environment variables.

It does not authenticate and cannot start the game.

## Cache

A bounded public-data cache is stored at:

```text
%LOCALAPPDATA%\NosGM\Launcher\community-cache.json
```

The cache:

- contains only the same public news, rankings, events and status shown in the window;
- is limited to 1 MiB;
- expires after two days;
- is validated before display;
- is replaced atomically;
- is visibly labeled `Datos en caché` when used.

A cache write failure never hides valid live data.

## Link safety

The portal, news and rankings buttons construct fixed relative paths under the already validated `PortalBaseUri`. Before Windows opens a link, the launcher verifies that scheme, host and port remain identical to the configured portal origin.

## Acceptance test

1. Start the integrated local stack from the community branch without `-SkipBuild`.
2. Confirm the footer contains `🏆 Comunidad` instead of the empty forum action.
3. Open the window and confirm the server name, health and player count appear.
4. Confirm News displays the signed public news or the empty-state message.
5. Switch between Combat, Reputation and Hero rankings.
6. Confirm Events displays active maintenance and upcoming calendar entries when configured.
7. Stop the portal, reopen the window and confirm a validated cache is labeled `Datos en caché`.
8. Confirm portal links remain under the configured portal origin.
9. Inspect `community-cache.json` and confirm it contains no account, password, ticket or session data.
