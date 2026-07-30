using System;
using System.Globalization;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackMirrorOptions
    {
        public const string EnabledVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_ENABLED";
        public const string QueueCapacityVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_QUEUE_CAPACITY";
        public const string StopTimeoutVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACK_MIRROR_STOP_TIMEOUT_MILLISECONDS";

        public const int DefaultQueueCapacity = 4096;
        public const int MinimumQueueCapacity = 64;
        public const int MaximumQueueCapacity = 16384;
        public const int DefaultStopTimeoutMilliseconds = 5000;
        public const int MinimumStopTimeoutMilliseconds = 1000;
        public const int MaximumStopTimeoutMilliseconds = 30000;

        private CommunicationCallbackMirrorOptions(
            bool enabled,
            int queueCapacity,
            int stopTimeoutMilliseconds)
        {
            Enabled = enabled;
            QueueCapacity = queueCapacity;
            StopTimeoutMilliseconds = stopTimeoutMilliseconds;
        }

        public bool Enabled { get; }

        public int QueueCapacity { get; }

        public int StopTimeoutMilliseconds { get; }

        public static CommunicationCallbackMirrorOptions Load(
            Func<string, string> readVariable = null)
        {
            readVariable = readVariable ?? Environment.GetEnvironmentVariable;
            bool enabled = ReadBoolean(
                readVariable(EnabledVariable),
                false,
                EnabledVariable);
            int queueCapacity = ReadInteger(
                readVariable(QueueCapacityVariable),
                DefaultQueueCapacity,
                MinimumQueueCapacity,
                MaximumQueueCapacity,
                QueueCapacityVariable);
            int stopTimeout = ReadInteger(
                readVariable(StopTimeoutVariable),
                DefaultStopTimeoutMilliseconds,
                MinimumStopTimeoutMilliseconds,
                MaximumStopTimeoutMilliseconds,
                StopTimeoutVariable);
            return new CommunicationCallbackMirrorOptions(
                enabled,
                queueCapacity,
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
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
                !bool.TryParse(value, out bool parsed))
            {
                throw new InvalidOperationException(
                    variableName +
                    " must be true or false without surrounding whitespace.");
            }
            return parsed;
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
                    variableName +
                    " must be an integer between " +
                    minimum +
                    " and " +
                    maximum +
                    ".");
            }
            return parsed;
        }
    }
}
