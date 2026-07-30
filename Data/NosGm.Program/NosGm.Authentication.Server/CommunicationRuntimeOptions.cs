using System.Globalization;
using Microsoft.Extensions.Configuration;
using NosGm.Cluster.Contracts.Communication.V1;

namespace NosGm.Authentication.Server;

public sealed class CommunicationRuntimeOptions
{
    public const string MaximumAccountsVariable =
        "NOSGM_COMMUNICATION_MAX_ACCOUNTS";
    public const string MaximumWorldsVariable =
        "NOSGM_COMMUNICATION_MAX_WORLDS";
    public const string MaximumCallbackSubscribersVariable =
        "NOSGM_COMMUNICATION_MAX_CALLBACK_SUBSCRIBERS";
    public const string SessionTtlVariable =
        "NOSGM_COMMUNICATION_SESSION_TTL_SECONDS";
    public const string GlacernonPortVariable =
        "NOSGM_COMMUNICATION_GLACERNON_PORT";

    public const int DefaultMaximumAccounts = 100000;
    public const int DefaultMaximumWorlds = 1024;
    public const int DefaultMaximumCallbackSubscribers = 2048;
    public const int DefaultSessionTtlSeconds = 300;
    public const int DefaultGlacernonPort = 5100;
    public const int MaximumChannelsPerGroup = 30;
    public const int GlacernonChannelId = 51;
    public const int MaximumCallbackSubscriberCapacity = 8192;

    private CommunicationRuntimeOptions(
        int maximumAccounts,
        int maximumWorlds,
        int maximumCallbackSubscribers,
        int sessionTtlSeconds,
        int glacernonPort)
    {
        MaximumAccounts = maximumAccounts;
        MaximumWorlds = maximumWorlds;
        MaximumCallbackSubscribers = maximumCallbackSubscribers;
        SessionTtlSeconds = sessionTtlSeconds;
        GlacernonPort = glacernonPort;
    }

    public int MaximumAccounts { get; }

    public int MaximumWorlds { get; }

    public int MaximumCallbackSubscribers { get; }

    public int SessionTtlSeconds { get; }

    public int GlacernonPort { get; }

    public static CommunicationRuntimeOptions Load(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new CommunicationRuntimeOptions(
            ReadInteger(
                configuration[MaximumAccountsVariable],
                DefaultMaximumAccounts,
                1,
                1000000,
                MaximumAccountsVariable),
            ReadInteger(
                configuration[MaximumWorldsVariable],
                DefaultMaximumWorlds,
                1,
                CommunicationContractLimits.MaxWorldsPerResponse,
                MaximumWorldsVariable),
            ReadInteger(
                configuration[MaximumCallbackSubscribersVariable],
                DefaultMaximumCallbackSubscribers,
                1,
                MaximumCallbackSubscriberCapacity,
                MaximumCallbackSubscribersVariable),
            ReadInteger(
                configuration[SessionTtlVariable],
                DefaultSessionTtlSeconds,
                30,
                3600,
                SessionTtlVariable),
            ReadInteger(
                configuration[GlacernonPortVariable],
                DefaultGlacernonPort,
                1,
                (int)CommunicationContractLimits.MaxEndpointPort,
                GlacernonPortVariable));
    }

    private static int ReadInteger(
        string value,
        int defaultValue,
        int minimum,
        int maximum,
        string variableName)
    {
        if (value == null)
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
