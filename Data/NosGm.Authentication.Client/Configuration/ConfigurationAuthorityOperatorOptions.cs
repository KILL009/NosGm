using System;

namespace NosGm.Authentication.Client.Configuration
{
    public sealed class ConfigurationAuthorityOperatorOptions
    {
        public const string ArmRequestVariable =
            "NOSGM_CONFIGURATION_GRPC_AUTHORITY_ARM_REQUEST_ID";
        public const string RollbackRequestVariable =
            "NOSGM_CONFIGURATION_GRPC_AUTHORITY_ROLLBACK_REQUESTED";
        public const string EffectRoutingVariable =
            "NOSGM_CONFIGURATION_GRPC_AUTHORITY_EFFECTS_ENABLED";

        private ConfigurationAuthorityOperatorOptions(
            string armRequestId,
            bool rollbackRequested,
            bool effectRoutingRequested)
        {
            ArmRequestId = armRequestId ?? string.Empty;
            RollbackRequested = rollbackRequested;
            EffectRoutingRequested = effectRoutingRequested;
        }

        public string ArmRequestId { get; }

        public bool RollbackRequested { get; }

        public bool EffectRoutingRequested { get; }

        public bool HasArmRequest =>
            !string.IsNullOrEmpty(ArmRequestId);

        public static ConfigurationAuthorityOperatorOptions Load(
            Func<string, string> readVariable = null)
        {
            readVariable = readVariable ??
                Environment.GetEnvironmentVariable;
            string armRequestId = ReadOptionalCanonicalGuid(
                readVariable(ArmRequestVariable),
                ArmRequestVariable);
            bool rollbackRequested = ReadBoolean(
                readVariable(RollbackRequestVariable),
                defaultValue: false,
                RollbackRequestVariable);
            bool effectRoutingRequested = ReadBoolean(
                readVariable(EffectRoutingVariable),
                defaultValue: false,
                EffectRoutingVariable);

            if (rollbackRequested &&
                (!string.IsNullOrEmpty(armRequestId) ||
                 effectRoutingRequested))
            {
                throw new InvalidOperationException(
                    ArmRequestVariable + " and " +
                    EffectRoutingVariable + " cannot be combined with " +
                    RollbackRequestVariable + ".");
            }
            if (effectRoutingRequested &&
                string.IsNullOrEmpty(armRequestId))
            {
                throw new InvalidOperationException(
                    EffectRoutingVariable +
                    " requires an explicit " + ArmRequestVariable + ".");
            }

            return new ConfigurationAuthorityOperatorOptions(
                armRequestId,
                rollbackRequested,
                effectRoutingRequested);
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
                !ConfigurationAuthorityGate.IsCanonicalNonEmptyGuid(value))
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
