using System;

namespace NosGm.Master.Library.Client
{
    internal sealed class ConfigurationGrpcShadowOptions
    {
        internal const string EnabledVariable =
            "NOSGM_CONFIGURATION_GRPC_SHADOW_ENABLED";
        internal const string TimeoutVariable =
            "NOSGM_CONFIGURATION_GRPC_SHADOW_TIMEOUT_MS";

        private const int DefaultTimeoutMilliseconds = 1500;
        private const int MinimumTimeoutMilliseconds = 100;
        private const int MaximumTimeoutMilliseconds = 10000;

        public bool Enabled { get; private set; }

        public int TimeoutMilliseconds { get; private set; }

        internal static ConfigurationGrpcShadowOptions Load(
            Func<string, string> readVariable = null)
        {
            readVariable = readVariable ?? Environment.GetEnvironmentVariable;
            string enabledValue = readVariable(EnabledVariable);
            bool enabled = false;
            if (!string.IsNullOrWhiteSpace(enabledValue) &&
                !bool.TryParse(enabledValue, out enabled))
            {
                throw new InvalidOperationException(
                    EnabledVariable + " must be true or false.");
            }

            int timeout = DefaultTimeoutMilliseconds;
            string timeoutValue = readVariable(TimeoutVariable);
            if (!string.IsNullOrWhiteSpace(timeoutValue) &&
                (!int.TryParse(timeoutValue, out timeout) ||
                 timeout < MinimumTimeoutMilliseconds ||
                 timeout > MaximumTimeoutMilliseconds))
            {
                throw new InvalidOperationException(
                    TimeoutVariable + " must be between " +
                    MinimumTimeoutMilliseconds + " and " +
                    MaximumTimeoutMilliseconds + " milliseconds.");
            }

            return new ConfigurationGrpcShadowOptions
            {
                Enabled = enabled,
                TimeoutMilliseconds = timeout
            };
        }
    }
}
