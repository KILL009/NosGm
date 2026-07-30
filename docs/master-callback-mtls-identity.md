# Master callback mTLS identity

The legacy Master process currently performs two unrelated jobs while the transport migration is in progress:

1. the launcher AuthBridge issues Gameforge authentication tickets;
2. the cluster Master will publish typed communication callbacks.

Those jobs must not share one client certificate. A compromised callback publisher must not gain ticket-issuer authority, and the AuthBridge must not gain callback-publisher authority.

## Certificate roles

Local certificate bundles contain four independent client identities:

- `AuthBridge`
- `Login`
- `World`
- `Master`

The authentication runtime accepts the Master fingerprint through:

```text
NOSGM_AUTH_GRPC_MASTER_CERT_SHA256
```

The fingerprint participates in the same cross-role reuse rejection as the existing three roles. Reusing one certificate for Master and another role prevents runtime startup.

## Separate process variables

The AuthBridge continues to use the existing authentication variables:

```text
NOSGM_AUTH_GRPC_CLIENT_CERT_PATH
NOSGM_AUTH_GRPC_CLIENT_CERT_PASSWORD
NOSGM_AUTH_GRPC_CALLER_INSTANCE_ID
```

The future communication callback publisher loads a different namespace:

```text
NOSGM_COMMUNICATION_GRPC_URL
NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PATH
NOSGM_COMMUNICATION_GRPC_MASTER_CERT_PASSWORD
NOSGM_COMMUNICATION_GRPC_TRUSTED_ROOT_CERT_PATH
NOSGM_COMMUNICATION_GRPC_MASTER_INSTANCE_ID
NOSGM_COMMUNICATION_GRPC_DEADLINE_MILLISECONDS
NOSGM_COMMUNICATION_GRPC_WIRE_MODE
```

`MasterCommunicationGrpcIdentityOptions` maps only that namespace to `ClusterNodeRole.Master`. It never reads the AuthBridge certificate variables.

## Compatibility boundary

The Master fingerprint is optional while an installation uses only the existing authentication gRPC service. The callback runtime must require a non-empty Master allow-list before `PublishCommunicationCallback` is activated. This avoids breaking authentication-only deployments before the callback slice is connected, while still making the security boundary explicit and testable now.

## Local rotation

Existing local bundles contain only three client certificates and must be replaced before callback acceptance:

```powershell
./scripts/new-local-authentication-certificates.ps1 `
  -OutputDirectory ./artifacts/authentication-grpc-local-new `
  -TrustRootCertificate
```

After verifying the new bundle, stop the local stack and replace the old bundle deliberately. Private keys remain in the current-user-only directory and passwords remain in the DPAPI-protected credential file.

## Acceptance proof

The live acceptance test presents the new Master certificate to the real loopback Kestrel endpoint. TLS must succeed, but an attempted AuthBridge-only RPC must return `PermissionDenied`. This proves that the certificate is trusted as Master without inheriting AuthBridge authority.
