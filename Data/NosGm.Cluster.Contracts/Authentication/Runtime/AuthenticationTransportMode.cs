using System;

namespace NosGm.Cluster.Contracts.Authentication.Runtime
{
    public enum AuthenticationTransportMode
    {
        Scs = 0,
        Grpc = 1
    }

    public static class AuthenticationTransportModeParser
    {
        public static AuthenticationTransportMode ParseOrDefault(
            string configuredValue)
        {
            if (string.IsNullOrEmpty(configuredValue))
            {
                return AuthenticationTransportMode.Scs;
            }

            if (string.Equals(
                    configuredValue,
                    "SCS",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticationTransportMode.Scs;
            }

            if (string.Equals(
                    configuredValue,
                    "GRPC",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticationTransportMode.Grpc;
            }

            throw new InvalidOperationException(
                "Authentication transport must be exactly SCS or GRPC.");
        }
    }
}
