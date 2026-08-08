using System;
using NosGm.Cluster.Contracts.V1;
using WireV1 = global::NosGm.Cluster.Wire.V1;

namespace NosGm.Cluster.Contracts.Configuration.V1
{
    public enum ConfigurationContractValidationError
    {
        None = 0,
        MissingRequest = 1,
        InvalidContext = 2,
        InvalidCallerRole = 3,
        MissingConfiguration = 4,
        InvalidMaxGold = 5,
        InvalidExpBuffTimestamp = 6,
        InvalidGoldBuffTimestamp = 7
    }

    public static class ConfigurationContractLimits
    {
        // DateTime.MinValue and DateTime.MaxValue expressed as Unix
        // milliseconds. Keeping the wire values inside this range guarantees
        // that the legacy ConfigurationObject can be reconstructed safely.
        public const long MinimumDateTimeUnixMilliseconds = -62135596800000L;
        public const long MaximumDateTimeUnixMilliseconds = 253402300799999L;
        public const int MaxRetainedUpdates = 256;
        public const int MaxPendingUpdatesPerSubscriber = 32;
        public const int MaxConcurrentSubscribers = 128;
    }

    public static class ClusterConfigurationContractValidator
    {
        public static ConfigurationContractValidationError Validate(
            WireV1.GetConfigurationRequest request)
        {
            return ValidateRequest(request, request?.Context);
        }

        public static ConfigurationContractValidationError Validate(
            WireV1.UpdateConfigurationRequest request)
        {
            ConfigurationContractValidationError error = ValidateRequest(
                request,
                request?.Context);
            if (error != ConfigurationContractValidationError.None)
            {
                return error;
            }

            return ValidateSnapshot(request.Configuration);
        }

        public static ConfigurationContractValidationError Validate(
            WireV1.SubscribeConfigurationUpdatesRequest request)
        {
            return ValidateRequest(request, request?.Context);
        }

        public static ConfigurationContractValidationError ValidateSnapshot(
            WireV1.ConfigurationSnapshot configuration)
        {
            if (configuration == null)
            {
                return ConfigurationContractValidationError
                    .MissingConfiguration;
            }

            if (configuration.MaxGold <= 0)
            {
                return ConfigurationContractValidationError.InvalidMaxGold;
            }

            if (!IsValidDateTimeUnixMilliseconds(
                    configuration.TimeExpBuffUnixTimeMs))
            {
                return ConfigurationContractValidationError
                    .InvalidExpBuffTimestamp;
            }

            return IsValidDateTimeUnixMilliseconds(
                    configuration.TimeGoldBuffUnixTimeMs)
                ? ConfigurationContractValidationError.None
                : ConfigurationContractValidationError
                    .InvalidGoldBuffTimestamp;
        }

        private static ConfigurationContractValidationError ValidateRequest(
            object request,
            WireV1.RequestContext context)
        {
            if (request == null)
            {
                return ConfigurationContractValidationError.MissingRequest;
            }

            if (context?.Version == null ||
                context.Version.Major > ushort.MaxValue ||
                context.Version.Minor > ushort.MaxValue)
            {
                return ConfigurationContractValidationError.InvalidContext;
            }

            var contractContext = new ClusterRequestContext
            {
                Version = new ClusterContractVersion(
                    (ushort)context.Version.Major,
                    (ushort)context.Version.Minor),
                RequestId = context.RequestId,
                IssuedAtUnixTimeMilliseconds = context.IssuedAtUnixTimeMs,
                DeadlineUnixTimeMilliseconds = context.DeadlineUnixTimeMs,
                CallerRole = (ClusterNodeRole)context.CallerRole,
                RequestedService = (ClusterService)context.RequestedService,
                CallerInstanceId = context.CallerInstanceId
            };

            if (ClusterContractValidator.Validate(contractContext) !=
                    ClusterContractValidationError.None ||
                contractContext.RequestedService !=
                    ClusterService.Configuration)
            {
                return ConfigurationContractValidationError.InvalidContext;
            }

            return contractContext.CallerRole == ClusterNodeRole.World
                ? ConfigurationContractValidationError.None
                : ConfigurationContractValidationError.InvalidCallerRole;
        }

        private static bool IsValidDateTimeUnixMilliseconds(long value)
        {
            return value >=
                       ConfigurationContractLimits
                           .MinimumDateTimeUnixMilliseconds &&
                   value <=
                       ConfigurationContractLimits
                           .MaximumDateTimeUnixMilliseconds;
        }
    }
}
