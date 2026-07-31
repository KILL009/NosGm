using System;

namespace NosGm.Communication.Client
{
    public sealed class CommunicationCallbackOperatorCutoverOptions
    {
        public const string PenaltyRefreshArmRequestVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACKS_PENALTY_REFRESH_ARM_REQUEST_ID";
        public const string PenaltyRefreshRollbackVariable =
            "NOSGM_COMMUNICATION_GRPC_CALLBACKS_PENALTY_REFRESH_ROLLBACK_REQUESTED";

        private CommunicationCallbackOperatorCutoverOptions(
            string penaltyRefreshArmRequestId,
            bool penaltyRefreshRollbackRequested)
        {
            PenaltyRefreshArmRequestId =
                penaltyRefreshArmRequestId ?? string.Empty;
            PenaltyRefreshRollbackRequested =
                penaltyRefreshRollbackRequested;
        }

        public string PenaltyRefreshArmRequestId { get; }

        public bool PenaltyRefreshRollbackRequested { get; }

        public bool HasPenaltyRefreshArmRequest =>
            !string.IsNullOrEmpty(PenaltyRefreshArmRequestId);

        public static CommunicationCallbackOperatorCutoverOptions Load(
            Func<string, string> readVariable = null)
        {
            readVariable = readVariable ??
                Environment.GetEnvironmentVariable;
            string armRequestId = ReadOptionalCanonicalGuid(
                readVariable(PenaltyRefreshArmRequestVariable),
                PenaltyRefreshArmRequestVariable);
            bool rollbackRequested = ReadBoolean(
                readVariable(PenaltyRefreshRollbackVariable),
                defaultValue: false,
                PenaltyRefreshRollbackVariable);

            if (rollbackRequested &&
                !string.IsNullOrEmpty(armRequestId))
            {
                throw new InvalidOperationException(
                    PenaltyRefreshArmRequestVariable + " and " +
                    PenaltyRefreshRollbackVariable +
                    " cannot be requested together.");
            }

            return new CommunicationCallbackOperatorCutoverOptions(
                armRequestId,
                rollbackRequested);
        }

        private static string ReadOptionalCanonicalGuid(
            string value,
            string variableName)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            if (!string.Equals(
                    value,
                    value.Trim(),
                    StringComparison.Ordinal) ||
                !CommunicationCallbackKindParityEvidence
                    .IsCanonicalNonEmptyGuid(value))
            {
                throw new InvalidOperationException(
                    variableName +
                    " must be an exact lowercase canonical non-empty GUID.");
            }

            return value;
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
            if (!string.Equals(
                    value,
                    value.Trim(),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    variableName +
                    " must be true or false without surrounding whitespace.");
            }
            if (string.Equals(
                    value,
                    "true",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (string.Equals(
                    value,
                    "false",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw new InvalidOperationException(
                variableName + " must be true or false.");
        }
    }
}
