using Microsoft.Extensions.Configuration;

namespace NosGm.Authentication.Server;

public sealed class ConfigurationRuntimeControlOptions
{
    public const string EnabledVariable =
        "NOSGM_CONFIGURATION_GRPC_RUNTIME_CONTROL_ENABLED";

    public ConfigurationRuntimeControlOptions(bool enabled)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }

    public static ConfigurationRuntimeControlOptions Load(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string value = configuration[EnabledVariable];
        if (value == null)
        {
            return new ConfigurationRuntimeControlOptions(false);
        }
        if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
        {
            return new ConfigurationRuntimeControlOptions(true);
        }
        if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
        {
            return new ConfigurationRuntimeControlOptions(false);
        }

        throw new InvalidOperationException(
            EnabledVariable + " must be true or false.");
    }
}
