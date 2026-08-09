using System;
using System.Globalization;

namespace NosGm.Communication.Client
{
    public enum CommunicationCallbackActivationMode
    {
        Disabled = 0,
        Shadow = 1,
        PenaltyRefreshCutover = 2
    }

    public sealed class CommunicationCallbackActivationOptions
    {
        public const string EnabledVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACKS_ENABLED";
        public const string ApplyVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACKS_APPLY_ENABLED";
        public const string StopTimeoutVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACKS_STOP_TIMEOUT_MILLISECONDS";

        public const int DefaultStopTimeoutMilliseconds = 5000;
        public const int MinimumStopTimeoutMilliseconds = 1000;
        public const int MaximumStopTimeoutMilliseconds = 30000;

        private CommunicationCallbackActivationOptions(
            CommunicationCallbackActivationMode mode,
            int stopTimeoutMilliseconds)
        {
            Mode = mode;
            StopTimeoutMilliseconds = stopTimeoutMilliseconds;
        }

        public CommunicationCallbackActivationMode Mode { get; }

        public int StopTimeoutMilliseconds { get; }

        public bool IsEnabled =>
            Mode != CommunicationCallbackActivationMode.Disabled;

        public bool IsApplyEnabled =>
            Mode ==
                CommunicationCallbackActivationMode.PenaltyRefreshCutover;

        public static CommunicationCallbackActivationOptions Load(
            Func<string, string> readVariable = null)
        {
            bool usesProcessEnvironment = readVariable == null;
            readVariable = readVariable ?? Environment.GetEnvironmentVariable;
            bool enabled = ReadBoolean(
                readVariable(EnabledVariable),
                defaultValue: false,
                EnabledVariable);
            bool apply = ReadBoolean(
                readVariable(ApplyVariable),
                defaultValue: false,
                ApplyVariable);
            int stopTimeout = ReadInteger(
                readVariable(StopTimeoutVariable),
                DefaultStopTimeoutMilliseconds,
                MinimumStopTimeoutMilliseconds,
                MaximumStopTimeoutMilliseconds,
                StopTimeoutVariable);

            if (apply && !enabled)
            {
                throw new InvalidOperationException(
                    ApplyVariable + " requires " + EnabledVariable + "=true.");
            }

            // The normal local stack already scopes distinct authentication
            // gRPC certificates to Login and World. In explicit shadow mode we
            // may bridge those process-local identities into the callback
            // namespace before the subscriber options are loaded. Tests that
            // provide an isolated variable reader never mutate process state.
            if (enabled &&
                usesProcessEnvironment &&
                CommunicationCallbackExistingIdentityFallback.IsEnabled())
            {
                CommunicationCallbackExistingIdentityFallback
                    .PrepareSubscriberEnvironment();
            }

            CommunicationCallbackActivationMode mode =
                !enabled
                    ? CommunicationCallbackActivationMode.Disabled
                    : apply
                        ? CommunicationCallbackActivationMode
                            .PenaltyRefreshCutover
                        : CommunicationCallbackActivationMode.Shadow;

            // Production gRPC callback application remains blocked for every
            // callback kind except the operator-qualified PenaltyRefresh slice.
            return new CommunicationCallbackActivationOptions(
                mode,
                stopTimeout);
        }

        private static bool ReadBoolean(
            string value,
            bool defaultValue,
            string variableName)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    variableName + " must be true or false without surrounding whitespace.");
            }
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw new InvalidOperationException(
                variableName + " must be true or false.");
        }

        private static int ReadInteger(
            string value,
            int defaultValue,
            int minimum,
            int maximum,
            string variableName)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            if (!int.TryParse(
                    value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int parsed) ||
                parsed < minimum ||
                parsed > maximum)
            {
                throw new InvalidOperationException(
                    variableName + " must be an integer between " +
                    minimum + " and " + maximum + ".");
            }
            return parsed;
        }
    }
}
