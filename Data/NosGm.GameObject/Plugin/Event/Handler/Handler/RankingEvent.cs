using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace NosGm.GameObject.Plugin.Event.Handler
{
    public static class RankingEvent
    {
        public static void Load()
        {
            ServerManager.Instance.TopComplimented = DAOFactory.CharacterDAO.GetTopCompliment();
            ServerManager.Instance.TopPoints = DAOFactory.CharacterDAO.GetTopPoints();
            ServerManager.Instance.TopReputation = DAOFactory.CharacterDAO.GetTopReputation();
            ServerManager.Instance.TopDuel = DAOFactory.CharacterDAO.GetTopDuel();
            ServerManager.Instance.TopMonster = DAOFactory.CharacterDAO.GetTopMonster();
            PublicSnapshotPublisher.Start();
        }
    }

    /// <summary>
    /// Publishes a narrow, signed read model for the public portal. The portal never receives a
    /// game-database connection string and only sees fields explicitly copied into this snapshot.
    /// Set NOSGM_PUBLIC_SNAPSHOT_DIRECTORY and NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64 to enable it.
    /// </summary>
    internal sealed class PublicSnapshotPublisher
    {
        private const int SchemaVersion = 1;
        private static readonly Lazy<PublicSnapshotPublisher> LazyInstance =
            new Lazy<PublicSnapshotPublisher>(() => new PublicSnapshotPublisher());
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static readonly HashSet<string> SupportedLanguages = new HashSet<string>(
            new[] { "es", "en", "de", "fr", "it", "pl", "cs", "ru", "ja", "zh-CN" },
            StringComparer.OrdinalIgnoreCase);

        private readonly object _startLock = new object();
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatString = "o",
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        private Timer _timer;
        private string _directory;
        private string _snapshotPath;
        private string _newsPath;
        private string _keyId;
        private byte[] _key;
        private int _intervalSeconds;
        private int _leaderChannel;
        private int _publishing;
        private bool _started;
        private HashSet<long> _excludedCharacterIds;
        private HashSet<string> _excludedCharacterNames;

        public static void Start()
        {
            LazyInstance.Value.StartCore();
        }

        private void StartCore()
        {
            lock (_startLock)
            {
                if (_started)
                {
                    return;
                }

                string configuredDirectory = Environment.GetEnvironmentVariable("NOSGM_PUBLIC_SNAPSHOT_DIRECTORY");
                string configuredKey = Environment.GetEnvironmentVariable("NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64");
                if (string.IsNullOrWhiteSpace(configuredDirectory) || string.IsNullOrWhiteSpace(configuredKey))
                {
                    return;
                }

                byte[] decodedKey;
                try
                {
                    decodedKey = Convert.FromBase64String(configuredKey.Trim());
                }
                catch (FormatException)
                {
                    Logger.Warn("[PUBLIC_SNAPSHOT] NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64 is not valid Base64.");
                    return;
                }

                if (decodedKey.Length < 32)
                {
                    Logger.Warn("[PUBLIC_SNAPSHOT] Signing key must contain at least 32 bytes.");
                    return;
                }

                try
                {
                    _directory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredDirectory.Trim()));
                    Directory.CreateDirectory(_directory);
                }
                catch (Exception exception)
                {
                    Logger.Warn("[PUBLIC_SNAPSHOT] Snapshot directory could not be prepared: " + exception.Message);
                    return;
                }

                _key = decodedKey;
                _keyId = ReadTextEnvironment("NOSGM_PUBLIC_SNAPSHOT_KEY_ID", "nosgm-live-v1", 64);
                _snapshotPath = Path.Combine(_directory, "public-snapshot.json");
                _newsPath = ReadTextEnvironment(
                    "NOSGM_PUBLIC_NEWS_FILE",
                    Path.Combine(_directory, "public-news.json"),
                    4096);
                _intervalSeconds = ReadIntegerEnvironment("NOSGM_PUBLIC_SNAPSHOT_INTERVAL_SECONDS", 30, 15, 600);
                _leaderChannel = ReadIntegerEnvironment("NOSGM_PUBLIC_SNAPSHOT_LEADER_CHANNEL", 1, 1, 255);
                _excludedCharacterIds = ReadLongSetEnvironment("NOSGM_PUBLIC_EXCLUDED_CHARACTER_IDS");
                _excludedCharacterNames = ReadStringSetEnvironment("NOSGM_PUBLIC_EXCLUDED_CHARACTER_NAMES");

                _timer = new Timer(
                    _ => PublishSafely(),
                    null,
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(_intervalSeconds));
                _started = true;
                Logger.Info($"[PUBLIC_SNAPSHOT] Enabled. Leader channel={_leaderChannel}, interval={_intervalSeconds}s.");
            }
        }

        private void PublishSafely()
        {
            if (Interlocked.Exchange(ref _publishing, 1) != 0)
            {
                return;
            }

            try
            {
                Publish();
            }
            catch (Exception exception)
            {
                Logger.Warn("[PUBLIC_SNAPSHOT] Publication failed: " + exception.Message);
            }
            finally
            {
                Volatile.Write(ref _publishing, 0);
            }
        }

        private void Publish()
        {
            int channelId = ServerManager.Instance.ChannelId;
            if (channelId <= 0)
            {
                return;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            WriteHeartbeat(channelId, now);
            if (channelId != _leaderChannel)
            {
                return;
            }

            List<ServiceRecord> channels = ReadChannelHeartbeats(now);
            bool loginOnline = ProbeTcp(
                ReadTextEnvironment("NOSGM_PUBLIC_LOGIN_HOST", "127.0.0.1", 255),
                ReadIntegerEnvironment("NOSGM_PUBLIC_LOGIN_PORT", 4000, 1, 65535),
                750);
            bool anyChannelOnline = channels.Any(channel =>
                string.Equals(channel.Health, "Online", StringComparison.Ordinal));
            bool anyKnownChannel = channels.Count > 0;

            var services = new List<ServiceRecord>
            {
                new ServiceRecord
                {
                    Id = "login",
                    Name = "Login",
                    Health = loginOnline ? "Online" : "Offline",
                    OnlinePlayers = 0
                },
                new ServiceRecord
                {
                    Id = "world",
                    Name = "World",
                    Health = anyChannelOnline ? "Online" : anyKnownChannel ? "Degraded" : "Offline",
                    OnlinePlayers = 0
                }
            };
            services.AddRange(channels.OrderBy(ChannelSortKey));

            var payload = new SnapshotPayload
            {
                ServerName = ReadTextEnvironment("NOSGM_PUBLIC_SERVER_NAME", "NosGM", 40),
                ObservedAt = now,
                News = LoadNews(),
                Services = services,
                Rankings = BuildRankings()
            };

            string payloadJson = JsonConvert.SerializeObject(payload, _jsonSettings);
            string signature = ComputeSignature(payloadJson);
            string envelope = "{\"schemaVersion\":" + SchemaVersion.ToString(CultureInfo.InvariantCulture)
                              + ",\"keyId\":" + JsonConvert.ToString(_keyId)
                              + ",\"payload\":" + payloadJson
                              + ",\"signature\":" + JsonConvert.ToString(signature)
                              + "}";
            WriteAtomic(_snapshotPath, envelope);
        }

        private void WriteHeartbeat(int channelId, DateTimeOffset observedAt)
        {
            int onlinePlayers = ServerManager.Instance.Sessions.Count();
            var heartbeat = new ChannelHeartbeat
            {
                Id = "channel-" + channelId.ToString(CultureInfo.InvariantCulture),
                Name = "Channel " + channelId.ToString(CultureInfo.InvariantCulture),
                Health = "Online",
                OnlinePlayers = Math.Max(0, onlinePlayers),
                ObservedAt = observedAt
            };
            string path = Path.Combine(
                _directory,
                "channel-" + channelId.ToString(CultureInfo.InvariantCulture) + ".json");
            WriteAtomic(path, JsonConvert.SerializeObject(heartbeat, _jsonSettings));
        }

        private List<ServiceRecord> ReadChannelHeartbeats(DateTimeOffset now)
        {
            var result = new List<ServiceRecord>();
            TimeSpan staleAfter = TimeSpan.FromSeconds(Math.Max(45, _intervalSeconds * 3));
            foreach (string path in Directory.EnumerateFiles(_directory, "channel-*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var heartbeat = JsonConvert.DeserializeObject<ChannelHeartbeat>(File.ReadAllText(path, Encoding.UTF8));
                    if (heartbeat == null || !IsSafeServiceId(heartbeat.Id))
                    {
                        continue;
                    }

                    TimeSpan age = now - heartbeat.ObservedAt;
                    if (age > TimeSpan.FromDays(1))
                    {
                        TryDelete(path);
                        continue;
                    }

                    bool fresh = age >= TimeSpan.Zero && age <= staleAfter;
                    result.Add(new ServiceRecord
                    {
                        Id = heartbeat.Id,
                        Name = string.IsNullOrWhiteSpace(heartbeat.Name) ? heartbeat.Id : LimitText(heartbeat.Name, 80),
                        Health = fresh ? "Online" : "Offline",
                        OnlinePlayers = fresh ? Math.Max(0, heartbeat.OnlinePlayers) : 0
                    });
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException)
                {
                    Logger.Warn("[PUBLIC_SNAPSHOT] Invalid channel heartbeat " + Path.GetFileName(path) + ": " + exception.Message);
                }
            }

            return result
                .GroupBy(service => service.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private Dictionary<string, List<RankingRecord>> BuildRankings()
        {
            List<CharacterDTO> activeCharacters = DAOFactory.CharacterDAO.LoadAll()
                .Where(IsPublishableCharacter)
                .ToList();

            List<CharacterDTO> reputationCharacters = DAOFactory.CharacterDAO.GetTopReputation()
                .Where(IsPublishableCharacter)
                .ToList();

            return new Dictionary<string, List<RankingRecord>>(StringComparer.OrdinalIgnoreCase)
            {
                ["combat"] = activeCharacters
                    .OrderByDescending(character => character.DuelWon)
                    .ThenByDescending(character => character.TalentWin)
                    .ThenByDescending(character => character.Level)
                    .ThenByDescending(character => character.HeroLevel)
                    .Take(50)
                    .Select((character, index) => ToRanking(
                        character,
                        index + 1,
                        Math.Max(0, character.DuelWon),
                        "duelWins"))
                    .ToList(),
                ["reputation"] = reputationCharacters
                    .OrderByDescending(character => character.Reputation)
                    .Take(50)
                    .Select((character, index) => ToRanking(
                        character,
                        index + 1,
                        Math.Max(0L, character.Reputation),
                        "reputation"))
                    .ToList(),
                ["hero"] = activeCharacters
                    .OrderByDescending(character => character.HeroLevel)
                    .ThenByDescending(character => character.HeroXp)
                    .ThenByDescending(character => character.Level)
                    .Take(50)
                    .Select((character, index) => ToRanking(
                        character,
                        index + 1,
                        Math.Max(0L, character.HeroXp),
                        "heroXp"))
                    .ToList()
            };
        }

        private bool IsPublishableCharacter(CharacterDTO character)
        {
            if (character == null
                || character.State != CharacterState.Active
                || string.IsNullOrWhiteSpace(character.Name)
                || character.Name.StartsWith("[DELETED]", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !_excludedCharacterIds.Contains(character.CharacterId)
                   && !_excludedCharacterNames.Contains(character.Name);
        }

        private static RankingRecord ToRanking(CharacterDTO character, int position, long score, string metric)
            => new RankingRecord
            {
                Position = position,
                CharacterName = SanitizeCharacterName(character.Name),
                Level = Math.Max(0, character.Level),
                HeroLevel = Math.Max(0, character.HeroLevel),
                Reputation = Math.Max(0L, character.Reputation),
                Score = score,
                Metric = metric
            };

        private List<NewsRecord> LoadNews()
        {
            if (string.IsNullOrWhiteSpace(_newsPath) || !File.Exists(_newsPath))
            {
                return new List<NewsRecord>();
            }

            try
            {
                var items = JsonConvert.DeserializeObject<List<NewsRecord>>(File.ReadAllText(_newsPath, Encoding.UTF8))
                            ?? new List<NewsRecord>();
                return items
                    .Where(IsValidNews)
                    .OrderByDescending(item => item.PublishedAt)
                    .Take(200)
                    .Select(item => new NewsRecord
                    {
                        Id = LimitToken(item.Id, 80),
                        Slug = LimitToken(item.Slug, 100),
                        Title = LimitText(item.Title, 160),
                        Summary = LimitText(item.Summary, 600),
                        PublishedAt = item.PublishedAt,
                        Language = NormalizeLanguage(item.Language)
                    })
                    .ToList();
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is JsonException)
            {
                Logger.Warn("[PUBLIC_SNAPSHOT] News file could not be read: " + exception.Message);
                return new List<NewsRecord>();
            }
        }

        private static bool IsValidNews(NewsRecord item)
            => item != null
               && IsSafeToken(item.Id, 80)
               && IsSafeToken(item.Slug, 100)
               && SupportedLanguages.Contains(NormalizeLanguage(item.Language))
               && !string.IsNullOrWhiteSpace(item.Title)
               && !string.IsNullOrWhiteSpace(item.Summary)
               && item.PublishedAt > DateTimeOffset.UnixEpoch;

        private string ComputeSignature(string payloadJson)
        {
            string signedText = SchemaVersion.ToString(CultureInfo.InvariantCulture)
                                + "\n" + _keyId
                                + "\n" + payloadJson;
            using (var hmac = new HMACSHA256(_key))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Utf8WithoutBom.GetBytes(signedText)));
            }
        }

        private static bool ProbeTcp(string host, int port, int timeoutMilliseconds)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    IAsyncResult result = client.BeginConnect(host, port, null, null);
                    using (result.AsyncWaitHandle)
                    {
                        if (!result.AsyncWaitHandle.WaitOne(timeoutMilliseconds))
                        {
                            return false;
                        }
                    }

                    client.EndConnect(result);
                    return client.Connected;
                }
            }
            catch (Exception exception) when (exception is SocketException || exception is IOException || exception is ObjectDisposedException)
            {
                return false;
            }
        }

        private static void WriteAtomic(string destinationPath, string content)
        {
            string temporaryPath = destinationPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporaryPath, content, Utf8WithoutBom);
            try
            {
                if (File.Exists(destinationPath))
                {
                    try
                    {
                        File.Replace(temporaryPath, destinationPath, null, true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Delete(destinationPath);
                        File.Move(temporaryPath, destinationPath);
                    }
                    catch (IOException)
                    {
                        File.Delete(destinationPath);
                        File.Move(temporaryPath, destinationPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, destinationPath);
                }
            }
            finally
            {
                TryDelete(temporaryPath);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // A stale auxiliary file is harmless and will be retried later.
            }
        }

        private static int ChannelSortKey(ServiceRecord service)
        {
            int separator = service.Id.LastIndexOf('-');
            int channel;
            return separator >= 0
                   && int.TryParse(service.Id.Substring(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out channel)
                ? channel
                : int.MaxValue;
        }

        private static bool IsSafeServiceId(string value)
            => IsSafeToken(value, 64) && value.StartsWith("channel-", StringComparison.OrdinalIgnoreCase);

        private static bool IsSafeToken(string value, int maximumLength)
            => !string.IsNullOrWhiteSpace(value)
               && value.Length <= maximumLength
               && value.All(character => char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.');

        private static string LimitToken(string value, int maximumLength)
            => new string((value ?? string.Empty)
                .Where(character => char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.')
                .Take(maximumLength)
                .ToArray());

        private static string LimitText(string value, int maximumLength)
            => new string((value ?? string.Empty)
                .Where(character => !char.IsControl(character))
                .Take(maximumLength)
                .ToArray())
                .Trim();

        private static string SanitizeCharacterName(string value)
        {
            string result = LimitText(value, 32);
            return string.IsNullOrWhiteSpace(result) ? "Unknown" : result;
        }

        private static string NormalizeLanguage(string value)
        {
            if (string.Equals(value, "zh", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "zh-cn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "cn", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-CN";
            }

            if (string.Equals(value, "cz", StringComparison.OrdinalIgnoreCase))
            {
                return "cs";
            }

            if (string.Equals(value, "jp", StringComparison.OrdinalIgnoreCase))
            {
                return "ja";
            }

            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static int ReadIntegerEnvironment(string name, int fallback, int minimum, int maximum)
        {
            int parsed;
            string value = Environment.GetEnvironmentVariable(name);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? Math.Max(minimum, Math.Min(maximum, parsed))
                : fallback;
        }

        private static string ReadTextEnvironment(string name, string fallback, int maximumLength)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return LimitText(string.IsNullOrWhiteSpace(value) ? fallback : value, maximumLength);
        }

        private static HashSet<long> ReadLongSetEnvironment(string name)
        {
            var result = new HashSet<long>();
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return result;
            }

            foreach (string token in value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                long parsed;
                if (long.TryParse(token.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    result.Add(parsed);
                }
            }

            return result;
        }

        private static HashSet<string> ReadStringSetEnvironment(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return new HashSet<string>(
                string.IsNullOrWhiteSpace(value)
                    ? Enumerable.Empty<string>()
                    : value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(item => item.Trim())
                        .Where(item => item.Length > 0),
                StringComparer.OrdinalIgnoreCase);
        }

        private sealed class SnapshotPayload
        {
            public string ServerName { get; set; }

            public DateTimeOffset ObservedAt { get; set; }

            public List<NewsRecord> News { get; set; }

            public List<ServiceRecord> Services { get; set; }

            public Dictionary<string, List<RankingRecord>> Rankings { get; set; }
        }

        private sealed class ChannelHeartbeat
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public string Health { get; set; }

            public int OnlinePlayers { get; set; }

            public DateTimeOffset ObservedAt { get; set; }
        }

        private sealed class ServiceRecord
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public string Health { get; set; }

            public int OnlinePlayers { get; set; }
        }

        private sealed class NewsRecord
        {
            public string Id { get; set; }

            public string Slug { get; set; }

            public string Title { get; set; }

            public string Summary { get; set; }

            public DateTimeOffset PublishedAt { get; set; }

            public string Language { get; set; }
        }

        private sealed class RankingRecord
        {
            public int Position { get; set; }

            public string CharacterName { get; set; }

            public int Level { get; set; }

            public int HeroLevel { get; set; }

            public long Reputation { get; set; }

            public long Score { get; set; }

            public string Metric { get; set; }
        }
    }
}
