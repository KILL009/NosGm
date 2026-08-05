// SPDX-License-Identifier: MIT

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NosGm.Configuration;
using NosGm.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
    }
}

namespace NosGm.GameObject.Plugin.Event
{
    internal static class PublicOperationsModule
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PublicOperationsPublisher.StartFromEnvironment();
        }
    }

    /// <summary>
    /// Publishes a second, bounded public document for launcher operations data.
    /// It shares the existing snapshot HMAC boundary but never exposes internal
    /// event handlers, database objects, accounts, sessions or secrets.
    /// </summary>
    internal static class PublicOperationsPublisher
    {
        private const int SchemaVersion = 1;
        private const int MaximumEvents = 100;
        private const int MaximumRate = 10000;

        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatString = "o",
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Include
        };
        private static readonly DateTimeOffset UnixEpoch =
            new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly object StartLock = new object();

        private static Timer _timer;
        private static byte[] _key;
        private static string _keyId;
        private static string _operationsPath;
        private static string _calendarPath;
        private static int _intervalSeconds;
        private static int _publishing;
        private static bool _started;

        public static void StartFromEnvironment()
        {
            lock (StartLock)
            {
                if (_started)
                {
                    return;
                }

                string directory = ReadEnvironment(
                    "NOSGM_PUBLIC_SNAPSHOT_DIRECTORY",
                    string.Empty,
                    4096);
                string encodedKey = ReadEnvironment(
                    "NOSGM_PUBLIC_SNAPSHOT_KEY_BASE64",
                    string.Empty,
                    4096);
                if (string.IsNullOrWhiteSpace(directory) ||
                    string.IsNullOrWhiteSpace(encodedKey))
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
                    Logger.Warn("[PUBLIC_OPERATIONS] Signing key is invalid Base64.");
                    return;
                }

                if (key.Length < 32)
                {
                    Logger.Warn("[PUBLIC_OPERATIONS] Signing key must contain at least 32 bytes.");
                    Array.Clear(key, 0, key.Length);
                    return;
                }

                string fullDirectory;
                try
                {
                    fullDirectory = Path.GetFullPath(
                        Environment.ExpandEnvironmentVariables(directory));
                    Directory.CreateDirectory(fullDirectory);
                }
                catch (Exception exception) when (
                    exception is IOException or UnauthorizedAccessException or ArgumentException)
                {
                    Logger.Warn("[PUBLIC_OPERATIONS] Directory setup failed: " + exception.Message);
                    Array.Clear(key, 0, key.Length);
                    return;
                }

                _key = key;
                _keyId = LimitToken(ReadEnvironment(
                    "NOSGM_PUBLIC_SNAPSHOT_KEY_ID",
                    "nosgm-live-v1",
                    64), 64);
                if (string.IsNullOrWhiteSpace(_keyId))
                {
                    _keyId = "nosgm-live-v1";
                }

                _operationsPath = Path.Combine(fullDirectory, "public-operations.json");
                _calendarPath = ReadEnvironment(
                    "NOSGM_PUBLIC_EVENTS_FILE",
                    Path.Combine(fullDirectory, "public-events.json"),
                    4096);
                _intervalSeconds = ReadInteger(
                    "NOSGM_PUBLIC_OPERATIONS_INTERVAL_SECONDS",
                    15,
                    10,
                    300);

                _timer = new Timer(
                    state => PublishSafely(),
                    null,
                    TimeSpan.FromSeconds(12),
                    TimeSpan.FromSeconds(_intervalSeconds));
                _started = true;
                Logger.Info(string.Format(
                    CultureInfo.InvariantCulture,
                    "[PUBLIC_OPERATIONS] Enabled. Interval={0}s.",
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
                Logger.Warn("[PUBLIC_OPERATIONS] Publication failed: " + exception.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _publishing, 0);
            }
        }

        private static void Publish()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            OperationsConfiguration configuration = LoadConfiguration();
            var payload = new OperationsPayload
            {
                ObservedAt = now,
                Rates = BuildRates(),
                Maintenance = BuildMaintenance(configuration?.Maintenance, now),
                Events = BuildEvents(configuration?.Events, now)
            };

            string payloadJson = JsonConvert.SerializeObject(payload, JsonSettings);
            string signedText = SchemaVersion.ToString(CultureInfo.InvariantCulture)
                                + "\n" + _keyId
                                + "\n" + payloadJson;
            string signature;
            using (var hmac = new HMACSHA256(_key))
            {
                signature = Convert.ToBase64String(
                    hmac.ComputeHash(Utf8WithoutBom.GetBytes(signedText)));
            }

            string envelope = "{\"schemaVersion\":"
                              + SchemaVersion.ToString(CultureInfo.InvariantCulture)
                              + ",\"keyId\":" + JsonConvert.ToString(_keyId)
                              + ",\"payload\":" + payloadJson
                              + ",\"signature\":" + JsonConvert.ToString(signature)
                              + "}";
            WriteAtomic(_operationsPath, envelope);
        }

        private static List<RateRecord> BuildRates()
        {
            return new List<RateRecord>
            {
                Rate("xp", "EXP", GameConfiguration.XPRate),
                Rate("hero-xp", "Hero EXP", GameConfiguration.HeroXPRate),
                Rate("drop", "Drop", GameConfiguration.DropRate),
                Rate("fairy-xp", "Fairy EXP", GameConfiguration.FairyXPRate),
                Rate("gold", "Gold", GameConfiguration.GoldRate),
                Rate("reputation", "Reputation", GameConfiguration.ReputationRate),
                Rate("job-xp", "Job EXP", GameConfiguration.JobLevelRate)
            };
        }

        private static RateRecord Rate(string id, string name, int value)
        {
            return new RateRecord
            {
                Id = id,
                Name = name,
                Multiplier = Math.Max(0, Math.Min(MaximumRate, value))
            };
        }

        private static MaintenanceRecord BuildMaintenance(
            MaintenanceConfiguration configured,
            DateTimeOffset now)
        {
            bool scheduleValid = configured != null
                                 && configured.StartsAt > UnixEpoch
                                 && configured.EndsAt > configured.StartsAt
                                 && configured.StartsAt <= now.AddDays(90)
                                 && configured.EndsAt >= now.AddDays(-1);
            bool scheduleActive = scheduleValid
                                  && now >= configured.StartsAt
                                  && now < configured.EndsAt;
            bool active = ServerConfiguration.MaintenanceMode || scheduleActive;

            return new MaintenanceRecord
            {
                IsActive = active,
                Title = LimitText(
                    scheduleValid && !string.IsNullOrWhiteSpace(configured.Title)
                        ? configured.Title
                        : active ? "Mantenimiento de NosGM" : string.Empty,
                    100),
                Message = LimitText(
                    scheduleValid && !string.IsNullOrWhiteSpace(configured.Message)
                        ? configured.Message
                        : active ? "El acceso al servidor está temporalmente restringido." : string.Empty,
                    400),
                StartsAt = scheduleValid ? (DateTimeOffset?)configured.StartsAt : null,
                EndsAt = scheduleValid ? (DateTimeOffset?)configured.EndsAt : null
            };
        }

        private static List<EventRecord> BuildEvents(
            List<EventConfiguration> configured,
            DateTimeOffset now)
        {
            if (configured == null)
            {
                return new List<EventRecord>();
            }

            return configured
                .Where(item => IsValidEvent(item, now))
                .OrderBy(item => item.StartsAt)
                .Take(MaximumEvents)
                .Select(item => new EventRecord
                {
                    Id = LimitToken(item.Id, 80),
                    Type = LimitToken(item.Type, 50),
                    Title = LimitText(item.Title, 120),
                    Category = LimitToken(item.Category, 32),
                    StartsAt = item.StartsAt,
                    EndsAt = item.EndsAt,
                    Channel = Math.Max(0, Math.Min(255, item.Channel)),
                    MinimumLevel = Math.Max(0, Math.Min(255, item.MinimumLevel)),
                    MaximumLevel = Math.Max(0, Math.Min(255, item.MaximumLevel)),
                    Details = LimitText(item.Details, 400)
                })
                .ToList();
        }

        private static bool IsValidEvent(EventConfiguration item, DateTimeOffset now)
        {
            return item != null
                   && IsSafeToken(item.Id, 80)
                   && IsSafeToken(item.Type, 50)
                   && IsSafeToken(item.Category, 32)
                   && !string.IsNullOrWhiteSpace(item.Title)
                   && item.StartsAt > UnixEpoch
                   && item.EndsAt > item.StartsAt
                   && item.EndsAt >= now.AddMinutes(-5)
                   && item.StartsAt <= now.AddDays(30)
                   && item.Channel >= 0
                   && item.Channel <= 255
                   && item.MinimumLevel >= 0
                   && item.MinimumLevel <= 255
                   && item.MaximumLevel >= item.MinimumLevel
                   && item.MaximumLevel <= 255;
        }

        private static OperationsConfiguration LoadConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_calendarPath) ||
                !File.Exists(_calendarPath))
            {
                return null;
            }

            try
            {
                var file = new FileInfo(_calendarPath);
                if (file.Length <= 0 || file.Length > 256 * 1024)
                {
                    Logger.Warn("[PUBLIC_OPERATIONS] Calendar file size is invalid.");
                    return null;
                }

                return JsonConvert.DeserializeObject<OperationsConfiguration>(
                    File.ReadAllText(_calendarPath, Encoding.UTF8));
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or JsonException)
            {
                Logger.Warn("[PUBLIC_OPERATIONS] Calendar file could not be read: " + exception.Message);
                return null;
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
                try
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
                catch
                {
                    // A later cycle can clean an abandoned temporary file.
                }
            }
        }

        private static int ReadInteger(
            string name,
            int fallback,
            int minimum,
            int maximum)
        {
            int parsed;
            string value = Environment.GetEnvironmentVariable(name);
            if (!int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out parsed))
            {
                return fallback;
            }

            return Math.Max(minimum, Math.Min(maximum, parsed));
        }

        private static string ReadEnvironment(
            string name,
            string fallback,
            int maximumLength)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                value = fallback;
            }

            value = value.Trim();
            return value.Length <= maximumLength
                ? value
                : value.Substring(0, maximumLength);
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

        private sealed class OperationsPayload
        {
            public DateTimeOffset ObservedAt { get; set; }
            public List<RateRecord> Rates { get; set; }
            public MaintenanceRecord Maintenance { get; set; }
            public List<EventRecord> Events { get; set; }
        }

        private sealed class RateRecord
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Multiplier { get; set; }
        }

        private sealed class MaintenanceRecord
        {
            public bool IsActive { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
            public DateTimeOffset? StartsAt { get; set; }
            public DateTimeOffset? EndsAt { get; set; }
        }

        private sealed class EventRecord
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string Title { get; set; }
            public string Category { get; set; }
            public DateTimeOffset StartsAt { get; set; }
            public DateTimeOffset EndsAt { get; set; }
            public int Channel { get; set; }
            public int MinimumLevel { get; set; }
            public int MaximumLevel { get; set; }
            public string Details { get; set; }
        }

        private sealed class OperationsConfiguration
        {
            public MaintenanceConfiguration Maintenance { get; set; }
            public List<EventConfiguration> Events { get; set; }
        }

        private sealed class MaintenanceConfiguration
        {
            public string Title { get; set; }
            public string Message { get; set; }
            public DateTimeOffset StartsAt { get; set; }
            public DateTimeOffset EndsAt { get; set; }
        }

        private sealed class EventConfiguration
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public string Title { get; set; }
            public string Category { get; set; }
            public DateTimeOffset StartsAt { get; set; }
            public DateTimeOffset EndsAt { get; set; }
            public int Channel { get; set; }
            public int MinimumLevel { get; set; }
            public int MaximumLevel { get; set; }
            public string Details { get; set; }
        }
    }
}
