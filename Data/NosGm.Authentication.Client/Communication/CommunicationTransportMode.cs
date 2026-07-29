using System;

namespace NosGm.Communication.Client
{
    public enum CommunicationTransportMode
    {
        Scs = 0,
        Grpc = 1
    }

    public static class CommunicationTransportModeParser
    {
        public const string EnvironmentVariableName =
            "NOSGM_COMMUNICATION_TRANSPORT";

        public static CommunicationTransportMode ParseEnvironment()
        {
            return ParseOrDefault(
                Environment.GetEnvironmentVariable(EnvironmentVariableName));
        }

        public static CommunicationTransportMode ParseOrDefault(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, "SCS", StringComparison.OrdinalIgnoreCase))
            {
                return CommunicationTransportMode.Scs;
            }

            if (string.Equals(value, "GRPC", StringComparison.OrdinalIgnoreCase))
            {
                return CommunicationTransportMode.Grpc;
            }

            throw new InvalidOperationException(
                EnvironmentVariableName + " must be SCS or GRPC.");
        }
    }
}
