# Configuration gRPC final validation

This file records the final validation entrypoints for the Configuration authority cutover. It deliberately does not restore any shadow, parity, rollback, SCS fallback, acceptance-pulse or LiveEffects migration path.

## Static and self-test entrypoints

Run from the repository root in Windows PowerShell:

```powershell
./scripts/verify-configuration-grpc-authority-final.ps1
./scripts/verify-configuration-runtime-controller.ps1
./scripts/verify-scs-transport-contracts.ps1
./scripts/verify-dotnet10-foundation.ps1
```

The final authority verifier checks that Configuration-only SCS contracts and implementations are absent, the migration map declares gRPC authority with no fallback, Master is seed-only, World remains the sole subscriber, startup identity wiring is role-separated, and gameplay effects publish authoritative state first.

The runtime-controller verifier checks one-time Master seeding, exact generation compare-and-swap restarts, explicit old-stream termination and isolation from the communication callback runtime.

The SCS inventory verifier now covers only the remaining non-Configuration legacy services.

## Windows runtime acceptance

Use the normal local stack scripts after the static/build checks pass. The Authentication/Configuration gRPC host must start before Master, Master must confirm or seed the Configuration snapshot before World starts, and the state file must report Configuration authority `gRPC`, fallback `null`, and subscriber role `World`.

Previously accepted `LiveEffects` evidence belongs to the migration phase and must not be repeated as part of this final cutover validation.
