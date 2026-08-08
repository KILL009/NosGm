using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Authentication.Client.Configuration
{
    public enum ConfigurationTransportResultCode
    {
        Unspecified = 0,
        Success = 1,
        InvalidRequest = 2,
        Unauthorized = 3,
        Conflict = 4,
        Unavailable = 5
    }

    public sealed class ConfigurationTransportSnapshot
    {
        public long MaxGold { get; set; }

        public long TimeExpBuffUnixTimeMilliseconds { get; set; }

        public long TimeGoldBuffUnixTimeMilliseconds { get; set; }
    }

    public sealed class ConfigurationTransportResult
    {
        public ConfigurationTransportResultCode Result { get; set; }

        public ConfigurationTransportSnapshot Configuration { get; set; }

        public ulong Generation { get; set; }

        public string RuntimeGenerationId { get; set; }
    }

    public sealed class ConfigurationTransportUpdate
    {
        public ConfigurationTransportSnapshot Configuration { get; set; }

        public ulong Generation { get; set; }

        public string RuntimeGenerationId { get; set; }

        public bool Replayed { get; set; }

        public bool RecoveredFromSnapshot { get; set; }
    }

    public interface IClusterConfigurationUpdateHandler
    {
        Task ObserveAsync(
            ConfigurationTransportUpdate update,
            CancellationToken cancellationToken);
    }

    public interface IClusterConfigurationTransport
    {
        Task<ConfigurationTransportResult> GetAsync(
            CancellationToken cancellationToken);

        Task<ConfigurationTransportResult> UpdateAsync(
            ConfigurationTransportSnapshot configuration,
            CancellationToken cancellationToken);
    }

    public interface IClusterConfigurationUpdateStreamTransport
    {
        Task SubscribeUpdatesAsync(
            string runtimeGenerationId,
            ulong resumeAfterGeneration,
            IClusterConfigurationUpdateHandler handler,
            CancellationToken cancellationToken);
    }
}
