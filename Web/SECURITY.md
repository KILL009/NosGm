# Security policy for NosGM Web

## Trust boundary

NosGM Web is a public presentation layer. It must never connect directly to the NosGM game database
or accept a game-account password on behalf of an undocumented legacy API.

## Current guarantees

- no authentication or payment endpoints;
- no session state;
- no forwarded-header trust by default;
- no production service endpoint;
- no private key or signing certificate;
- same-origin content policy and frame denial;
- bounded request body size and public API rate limiting;
- generic error responses outside development.

## Before accounts are added

A separate review must cover password handling, account lockout, MFA, cookie rotation, antiforgery,
audit events, recovery flows, support impersonation, authorization policies and API versioning.

Report security issues privately to the repository owner. Do not publish active credentials or
personal player data in an issue.
