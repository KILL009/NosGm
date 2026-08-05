# Local smart repair security boundary

The local channel exists only for source-built development launchers that still use the unconfigured placeholder. It is loopback-only, Development-only and signed with an ephemeral ECDSA P-256 key whose private half is never persisted.

Published launchers continue to use their compiled HTTPS channel and ignore the local configuration.
