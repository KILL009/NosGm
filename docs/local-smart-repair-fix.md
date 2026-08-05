# Local smart repair activation fix

Source-built launchers intentionally compile with an unconfigured `.invalid` release channel. That security default previously left the diagnostics repair button disabled during the integrated local-stack test.

This follow-up adds a development-only signed loopback channel. The launcher snapshots only its configured executable, creates an ECDSA P-256 key in memory, signs a bounded manifest and stores only the public key. The Development portal serves those files only to loopback clients.

Published launchers are unchanged: a compiled official channel takes precedence, remains HTTPS-only and never reads the local source-build configuration.
