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
    /// Exports a deliberately small, signed public read model. The Internet-facing portal receives
    /// this file instead of database credentials or direct access to the legacy data layer.
    /// </summary>
    internal static class PublicSnapshotPublisher
    {
        private const int SchemaVersion = 1;
        private static readonly object StartLock = new object();
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatString = "o",
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore
        };
        private static readonly HashSet<string> SupportedLanguages = new HashSet<string>(
            new[] { "es", "en", "de", "fr", "it", "pl", "cs", "ru", "ja", "zh-CN" },
            StringComparer.OrdinalIgnoreCase);
        private static readonly DateTimeOffset UnixEpoch =
            new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static Timer _timer;
        private static byte[] _key;
        private static string _keyId;
        private static string _directory;
        private static string _snapshotPath;
        private static string _newsPath;
        private static int _intervalSeconds;
        private static int _leaderChannel;
        private static int _publishing;
        private static bool _started;
        private static HashSet<long> _excludedCharacterIds;
        private static HashSet<string> _excludedCharacterNames;

        public static void Start()
        {
            lock (StartLock)
            {
                if (_started)
                {
                    return;
                }

                string directory = ReadEnvironment("NOSGM_PUBLIC_SNAPSHOT_DIRECTORY", string.Empty, 4096);
                string encodedKey = ReadEnvironment("NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64", string.Empty, 4096);
                if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(encodedKey))
                {
                    return;
                }

                byte[] key;
                try
                {
                    key = Convert.FromBase64String(encodedKey);
                }
                catch (FormatException)
                {
                    Logger.Warn("[PUBLIC_SNAPSHOT] NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64 is invalid.");
                    return;
                }

                if (key.Length < 32)
                {
                    Logger.Warn("[PUBLIC_SNAPSHOT] Signing key must contain at least 32 bytes.");
                    return;
                }

                try
                {
                    _directory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory));
                    Directory.CreateDirectory(_directory);
                }
                catch (Exception exception)
                {
                    Logger.Warn("[PUBLIC_SNAPSHOT] Directory setup failed: " + exception.Message);
                    return;
                }

                _key = key;
                _keyId = LimitToken(ReadEnvironment("NOSGM_PUBLIC_SNAPSHOT_KEY_ID", "nosgm-live-v1", 64), 64);
                if (string.IsNullOrWhiteSpace(_keyId))
                {
                    _keyId = "nosgm-live-v1";
                }

                _snapshotPath = Path.Combine(_directory, "public-snapshot.json");
                _newsPath = ReadEnvironment(
                    "NOSGM_PUBLIC_NEWS_FILE",
                    Path.Combine(_directory, "public-news.json"),
                    4096);
                _intervalSeconds = ReadInteger("NOSGM_PUBLIC_SNAPSHOT_INTERVAL_SECONDS", 30, 15, 600);
                _leaderChannel = ReadInteger("NOSGM_PUBLIC_SNAPSHOT_LEADER_CHANNEL", 1, 1, 255);
                _excludedCharacterIds = ReadLongSet("NOSGM_PUBLIC_EXCLUDED_CHARACTER_IDS");
                _excludedCharacterNames = ReadStringSet("NOSGM_PUBLIC_EXCLUDED_CHARACTER_NAMES");

                _timer = new Timer(
                    state => PublishSafely(),
                    null,
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(_intervalSeconds));
                _started = true;
                Logger.Info(string.Format(
                    CultureInfo.InvariantCulture,
                    "[PUBLIC_SNAPSHOT] Enabled. Leader channel={0}, interval={1}s.",
                    _leaderChannel,
                    _intervalSeconds));
            }
        }

        private static void PublishSafely()
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
                Interlocked.Exchange(ref _publishing, 0);
            }
        }

        private static void Publish()
        {
            int channelId = ServerManager.Instance.ChannelId;
            if (channelId <= 0)
            {
                return;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            WriteChannelHeartbeat(channelId, now);
            if (channelId != _leaderChannel)
            {
                return;
            }

            List<ServiceRecord> channels = ReadChannelHeartbeats(now);
            bool loginOnline = ProbeTcp(
                ReadEnvironment("NOSGM_PUBLIC_LOGIN_HOST", "127.0.0.1", 255),
                ReadInteger("NOSGM_PUBLIC_LOGIN_PORT", 4000, 1, 65535),
                750);
            bool anyChannelOnline = channels.Any(service =>
                string.Equals(service.Health, "Online", StringComparison.Ordinal));

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
                    Health = anyChannelOnline ? "Online" : channels.Count > 0 ? "Degraded" : "Offline",
                    OnlinePlayers = 0
                }
            };
            services.AddRange(channels.OrderBy(ChannelNumber));

            var payload = new SnapshotPayload
            {
                ServerName = LimitText(ReadEnvironment("NOSGM_PUBLIC_SERVER_NAME", "NosGM", 40), 40),
                ObservedAt = now,
                News = LoadNews(),
                Services = services,
                Rankings = BuildRankings()
            };

            string payloadJson = JsonConvert.SerializeObject(payload, JsonSettings);
            string signature = Sign(payloadJson);
            string envelope = "{\"schemaVersion\":" + SchemaVersion.ToString(CultureInfo.InvariantCulture)
                              + ",\"keyId\":" + JsonConvert.ToString(_keyId)
                              + ",\"payload\":" + payloadJson
                              + ",\"signature\":" + JsonConvert.ToString(signature)
                              + "}";
            WriteAtomic(_snapshotPath, envelope);
        }

        private static void WriteChannelHeartbeat(int channelId, DateTimeOffset observedAt)
        {
            int onlinePlayers = ServerManager.Instance.Sessions.Count();
            var heartbeat = new ChannelHeartbeat
            {
                Id = "channel-" + channelId.ToString(CultureInfo.InvariantCulture),
                Name = "Channel " + channelId.ToString(CultureInfo.InvariantCulture),
                Health = "Online",
                OnlinePlayers = onlinePlayers < 0 ? 0 : onlinePlayers,
                ObservedAt = observedAt
            };

            WriteAtomic(
                Path.Combine(_directory, heartbeat.Id + ".json"),
                JsonConvert.SerializeObject(heartbeat, JsonSettings));
        }

        private static List<ServiceRecord> ReadChannelHeartbeats(DateTimeOffset now)
        {
            var result = new List<ServiceRecord>();
            TimeSpan staleAfter = TimeSpan.FromSeconds(System.Math.Max(45, _intervalSeconds * 3));

            foreach (string path in Directory.EnumerateFiles(_directory, "channel-*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var heartbeat = JsonConvert.DeserializeObject<ChannelHeartbeat>(
                        File.ReadAllText(path, Encoding.UTF8));
                    if (heartbeat == null || !IsChannelId(heartbeat.Id))
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
                        Name = string.IsNullOrWhiteSpace(heartbeat.Name)
                            ? heartbeat.Id
                            : LimitText(heartbeat.Name, 80),
                        Health = fresh ? "Online" : "Offline",
                        OnlinePlayers = fresh && heartbeat.OnlinePlayers > 0
                            ? heartbeat.OnlinePlayers
                            : 0
                    });
                }
                catch (Exception exception) when (
                    exception is IOException
                    || exception is UnauthorizedAccessException
                    || exception is JsonException)
                {
                    Logger.Warn("[PUBLIC_SNAPSHOT] Invalid heartbeat " + Path.GetFileName(path) + ": " + exception.Message);
                }
            }

            return result
                .GroupBy(service => service.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private static Dictionary<string, List<RankingRecord>> BuildRankings()
        {
            List<CharacterDTO> active = DAOFactory.CharacterDAO.LoadAll()
                .Where(IsPublishableCharacter)
                .ToList();
            List<CharacterDTO> reputation = DAOFactory.CharacterDAO.GetTopReputation()
                .Where(IsPublishableCharacter)
                .ToList();

            return new Dictionary<string, List<RankingRecord>>(StringComparer.OrdinalIgnoreCase)
            {
                ["combat"] = active
                    .OrderByDescending(character => character.DuelWon)
                    .ThenByDescending(character => character.TalentWin)
                    .ThenByDescending(character => character.Level)
                    .ThenByDescending(character => character.HeroLevel)
                    .Take(50)
                    .Select((character, index) => ToRanking(
                        character,
                        index + 1,
                        NonNegative(Convert.ToInt64(character.DuelWon)),
                        "duelWins"))
                    .ToList(),
                ["reputation"] = reputation
                    .OrderByDescending(character => character.Reputation)
                    .Take(50)
                    .Select((character, index) => ToRanking(
                        character,
                        index + 1,
                        NonNegative(character.Reputation),
                        "reputation"))
                    .ToList(),
                ["hero"] = active
                    .OrderByDescending(character => character.HeroLevel)
                    .ThenByDescending(character => character.HeroXp)
                    .ThenByDescending(character => character.Level)
                    .Take(50)
                    .Select((character, index) => ToRanking(
                        character,
                        index + 1,
                        NonNegative(character.HeroXp),
                        "heroXp"))
                    .ToList()
            };
        }

        private static bool IsPublishableCharacter(CharacterDTO character)
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
        {
            return new RankingRecord
            {
                Position = position,
                CharacterName = LimitText(character.Name, 32),
                Level = Convert.ToInt32(character.Level),
                HeroLevel = Convert.ToInt32(character.HeroLevel),
                Reputation = NonNegative(character.Reputation),
                Score = NonNegative(score),
                Metric = metric
            };
        }

        private static List<NewsRecord> LoadNews()
        {
            if (string.IsNullOrWhiteSpace(_newsPath) || !File.Exists(_newsPath))
            {
                return new List<NewsRecord>();
            }

            try
            {
                var news = JsonConvert.DeserializeObject<List<NewsRecord>>(
                               File.ReadAllText(_newsPath, Encoding.UTF8))
                           ?? new List<NewsRecord>();

                return news
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
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is JsonException)
            {
                Logger.Warn("[PUBLIC_SNAPSHOT] News file could not be read: " + exception.Message);
                return new List<NewsRecord>();
            }
        }

        private static bool IsValidNews(NewsRecord item)
        {
            return item != null
                   && IsSafeToken(item.Id, 80)
                   && IsSafeToken(item.Slug, 100)
                   && SupportedLanguages.Contains(NormalizeLanguage(item.Language))
                   && !string.IsNullOrWhiteSpace(item.Title)
                   && !string.IsNullOrWhiteSpace(item.Summary)
                   && item.PublishedAt > UnixEpoch;
        }

        private static string Sign(string payloadJson)
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
                    WaitHandle waitHandle = result.AsyncWaitHandle;
                    try
                    {
                        if (!waitHandle.WaitOne(timeoutMilliseconds))
                        {
                            return false;
                        }
                    }
                    finally
                    {
                        waitHandle.Close();
                    }

                    client.EndConnect(result);
                    return client.Connected;
                }
            }
            catch (Exception exception) when (
                exception is SocketException
                || exception is IOException
                || exception is ObjectDisposedException)
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
                    catch (IOException)
                    {
                        File.Delete(destinationPath);
                        File.Move(temporaryPath, destinationPath);
                    }
                    catch (PlatformNotSupportedException)
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
                // A temporary or stale heartbeat can be retried on the next publication cycle.
            }
        }

        private static int ChannelNumber(ServiceRecord service)
        {
            int separator = service.Id.LastIndexOf('-');
            int channel;
            return separator >= 0
                   && int.TryParse(
                       service.Id.Substring(separator + 1),
                       NumberStyles.None,
                       CultureInfo.InvariantCulture,
                       out channel)
                ? channel
                : int.MaxValue;
        }

        private static long NonNegative(long value)
        {
            return value < 0 ? 0 : value;
        }

        private static bool IsChannelId(string value)
        {
            return IsSafeToken(value, 64)
                   && value.StartsWith("channel-", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSafeToken(string value, int maximumLength)
        {
            return !string.IsNullOrWhiteSpace(value)
                   && value.Length <= maximumLength
                   && value.All(character =>
                       char.IsLetterOrDigit(character)
                       || character == '-'
                       || character == '_'
                       || character == '.');
        }

        private static string LimitToken(string value, int maximumLength)
        {
            return new string((value ?? string.Empty)
                .Where(character =>
                    char.IsLetterOrDigit(character)
                    || character == '-'
                    || character == '_'
                    || character == '.')
                .Take(maximumLength)
                .ToArray());
        }

        private static string LimitText(string value, int maximumLength)
        {
            return new string((value ?? string.Empty)
                .Where(character => !char.IsControl(character))
                .Take(maximumLength)
                .ToArray())
                .Trim();
        }

        private static string NormalizeLanguage(string value)
        {
            string language = (value ?? string.Empty).Trim();
            if (string.Equals(language, "zh", StringComparison.OrdinalIgnoreCase)
                || string.Equals(language, "zh-cn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(language, "cn", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-CN";
            }

            if (string.Equals(language, "cz", StringComparison.OrdinalIgnoreCase))
            {
                return "cs";
            }

            if (string.Equals(language, "jp", StringComparison.OrdinalIgnoreCase))
            {
                return "ja";
            }

            return language.ToLowerInvariant();
        }

        private static int ReadInteger(string name, int fallback, int minimum, int maximum)
        {
            int parsed;
            string value = Environment.GetEnvironmentVariable(name);
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return fallback;
            }

            return System.Math.Max(minimum, System.Math.Min(maximum, parsed));
        }

        private static string ReadEnvironment(string name, string fallback, int maximumLength)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                value = fallback;
            }

            value = value.Trim();
            return value.Length <= maximumLength ? value : value.Substring(0, maximumLength);
        }

        private static HashSet<long> ReadLongSet(string name)
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

        private static HashSet<string> ReadStringSet(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            IEnumerable<string> values = string.IsNullOrWhiteSpace(value)
                ? Enumerable.Empty<string>()
                : value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .Where(item => item.Length > 0);
            return new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
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
