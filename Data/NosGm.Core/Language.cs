using NosGm.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;

namespace NosGm.Core
{
    /// <summary>
    /// Resolves server messages for a specific player culture.
    /// The neutral resource file is English and satellite resources override translated keys.
    /// </summary>
    public sealed class Language
    {
        private static readonly string[] SupportedCultures =
        {
            "en", "es", "de", "fr", "it", "pl", "cs", "ru", "ja", "zh"
        };

        private static readonly Lazy<Language> LazyInstance =
            new Lazy<Language>(() => new Language());

        private readonly AsyncLocal<string> _ambientCulture = new AsyncLocal<string>();

        private readonly ConcurrentDictionary<string, string> _language =
            new ConcurrentDictionary<string, string>();

        private readonly ConcurrentDictionary<string, byte> _missingLanguage =
            new ConcurrentDictionary<string, byte>();

        private readonly string _defaultCultureName;
        private readonly ResourceManager _manager;
        private readonly object _streamWriterLock = new object();
        private readonly StreamWriter _streamWriter;

        private Language()
        {
            _defaultCultureName = NormalizeKnownCulture(ServerConfiguration.Language) ?? "en";

            try
            {
                _streamWriter = new StreamWriter("MissingLanguage.txt", true)
                {
                    AutoFlush = true
                };
            }
            catch
            {
                // A read-only working directory must not prevent the server from starting.
            }

            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                _manager = new ResourceManager(
                    entryAssembly.GetName().Name + ".Resource.LocalizedResources",
                    entryAssembly);
            }
        }

        public static Language Instance => LazyInstance.Value;

        public string DefaultCultureName => _defaultCultureName;

        public string SupportedCultureList => string.Join(", ", SupportedCultures);

        public string GetMessageFromKey(string key)
        {
            return GetMessageFromKey(key, _ambientCulture.Value ?? _defaultCultureName);
        }

        /// <summary>
        /// Makes legacy GetMessageFromKey(key) calls use the current player's
        /// culture for the lifetime of a packet handler invocation.
        /// </summary>
        public IDisposable UseCulture(string cultureName)
        {
            var previousCulture = _ambientCulture.Value;
            _ambientCulture.Value = NormalizeCulture(cultureName);
            return new CultureScope(this, previousCulture);
        }

        public string GetMessageFromKey(string key, string cultureName)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var normalizedCulture = NormalizeCulture(cultureName);
            var cacheKey = normalizedCulture + "|" + key;

            return _language.GetOrAdd(cacheKey, ignored =>
            {
                string value = null;

                try
                {
                    value = _manager?.GetString(key, CultureInfo.GetCultureInfo(normalizedCulture));
                    if (string.IsNullOrEmpty(value) && normalizedCulture != _defaultCultureName)
                    {
                        value = _manager?.GetString(key, CultureInfo.GetCultureInfo(_defaultCultureName));
                    }
                }
                catch (MissingManifestResourceException)
                {
                    // Missing resources are reported below without crashing the World Server.
                }

                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }

                LogMissing(normalizedCulture, key);
                return key + " ";
            });
        }

        public string NormalizeCulture(string cultureName)
        {
            return NormalizeKnownCulture(cultureName) ?? _defaultCultureName;
        }

        public bool TryNormalizeCulture(string cultureName, out string normalizedCulture)
        {
            normalizedCulture = NormalizeKnownCulture(cultureName);
            return normalizedCulture != null;
        }

        private static string NormalizeKnownCulture(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                return null;
            }

            var candidate = cultureName.Trim().Replace('_', '-').ToLowerInvariant();

            if (candidate == "uk" || candidate == "gb" || candidate == "english" ||
                candidate.StartsWith("en-"))
            {
                candidate = "en";
            }
            else if (candidate == "spanish" || candidate == "español" ||
                     candidate == "espanol" || candidate.StartsWith("es-"))
            {
                candidate = "es";
            }
            else if (candidate == "german" || candidate == "deutsch" ||
                     candidate.StartsWith("de-"))
            {
                candidate = "de";
            }
            else if (candidate == "french" || candidate == "français" ||
                     candidate == "francais" || candidate.StartsWith("fr-"))
            {
                candidate = "fr";
            }
            else if (candidate == "italian" || candidate == "italiano" ||
                     candidate.StartsWith("it-"))
            {
                candidate = "it";
            }
            else if (candidate == "polish" || candidate == "polski" ||
                     candidate.StartsWith("pl-"))
            {
                candidate = "pl";
            }
            else if (candidate == "cz" || candidate == "czech" ||
                     candidate == "čeština" || candidate == "cestina" ||
                     candidate.StartsWith("cs-") || candidate.StartsWith("cz-"))
            {
                candidate = "cs";
            }
            else if (candidate == "russian" || candidate == "русский" ||
                     candidate.StartsWith("ru-"))
            {
                candidate = "ru";
            }
            else if (candidate == "jp" || candidate == "japanese" ||
                     candidate == "日本語" || candidate.StartsWith("ja-") ||
                     candidate.StartsWith("jp-"))
            {
                candidate = "ja";
            }
            else if (candidate == "cn" || candidate == "chinese" ||
                     candidate == "中文" || candidate.StartsWith("zh-") ||
                     candidate.StartsWith("cn-"))
            {
                candidate = "zh";
            }

            foreach (var supportedCulture in SupportedCultures)
            {
                if (candidate == supportedCulture)
                {
                    return supportedCulture;
                }
            }

            return null;
        }

        private void LogMissing(string cultureName, string key)
        {
            var missingKey = cultureName + "|" + key;
            if (!_missingLanguage.TryAdd(missingKey, 0) || _streamWriter == null)
            {
                return;
            }

            lock (_streamWriterLock)
            {
                _streamWriter.WriteLine(missingKey);
            }
        }

        private sealed class CultureScope : IDisposable
        {
            private readonly Language _owner;
            private readonly string _previousCulture;
            private bool _disposed;

            public CultureScope(Language owner, string previousCulture)
            {
                _owner = owner;
                _previousCulture = previousCulture;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _owner._ambientCulture.Value = _previousCulture;
                _disposed = true;
            }
        }
    }

    public sealed class ClientLanguageProfile
    {
        public ClientLanguageProfile(
            byte regionType,
            int loginPort,
            string protocolPrefix,
            string clientFileSuffix,
            string serverCulture)
        {
            RegionType = regionType;
            LoginPort = loginPort;
            ProtocolPrefix = protocolPrefix;
            ClientFileSuffix = clientFileSuffix;
            ServerCulture = serverCulture;
        }

        public byte RegionType { get; }

        public int LoginPort { get; }

        public string ProtocolPrefix { get; }

        public string ClientFileSuffix { get; }

        public string ServerCulture { get; }
    }

    /// <summary>
    /// Maps each supported client language to its Login port, protocol account prefix,
    /// NSlangData suffix and server culture. The accepted local Login port is authoritative.
    /// World endpoint ports listed later in NsTeST are independent channel ports.
    /// </summary>
    public static class ClientRegionMap
    {
        private static readonly ClientLanguageProfile[] Profiles =
        {
            new ClientLanguageProfile(0, 4000, "EN", "UK", "en"),
            new ClientLanguageProfile(1, 4001, "DE", "DE", "de"),
            new ClientLanguageProfile(2, 4002, "FR", "FR", "fr"),
            new ClientLanguageProfile(3, 4003, "IT", "IT", "it"),
            new ClientLanguageProfile(4, 4004, "PL", "PL", "pl"),
            new ClientLanguageProfile(5, 4005, "ES", "ES", "es"),
            new ClientLanguageProfile(6, 4006, "CZ", "CZ", "cs"),
            new ClientLanguageProfile(7, 4007, "RU", "RU", "ru"),
            new ClientLanguageProfile(8, 4008, "JP", "JP", "ja"),
            new ClientLanguageProfile(9, 4009, "CN", "CN", "zh")
        };

        public const int BaseLoginPort = 4000;

        public static IReadOnlyList<ClientLanguageProfile> All => Profiles;

        public static int RegionCount => Profiles.Length;

        public static int GetLoginPort(byte regionType)
        {
            if (!TryGetProfile(regionType, out ClientLanguageProfile profile))
            {
                throw new ArgumentOutOfRangeException(nameof(regionType));
            }

            return profile.LoginPort;
        }

        public static bool TryGetProfile(byte regionType, out ClientLanguageProfile profile)
        {
            if (regionType >= Profiles.Length)
            {
                profile = null;
                return false;
            }

            profile = Profiles[regionType];
            return true;
        }

        public static bool TryGetCulture(byte regionType, out string culture)
        {
            if (!TryGetProfile(regionType, out ClientLanguageProfile profile))
            {
                culture = null;
                return false;
            }

            culture = profile.ServerCulture;
            return true;
        }

        public static bool TryResolveLoginPort(
            int loginPort,
            out byte regionType,
            out string culture)
        {
            int regionIndex = loginPort - BaseLoginPort;
            if (regionIndex < 0 || regionIndex >= Profiles.Length ||
                Profiles[regionIndex].LoginPort != loginPort)
            {
                regionType = 0;
                culture = null;
                return false;
            }

            ClientLanguageProfile profile = Profiles[regionIndex];
            regionType = profile.RegionType;
            culture = profile.ServerCulture;
            return true;
        }

        public static bool TryStripProtocolPrefix(
            string protocolUsername,
            out string accountName,
            out ClientLanguageProfile profile)
        {
            accountName = null;
            profile = null;
            if (string.IsNullOrWhiteSpace(protocolUsername))
            {
                return false;
            }

            foreach (ClientLanguageProfile candidate in Profiles)
            {
                string prefix = candidate.ProtocolPrefix + "_";
                if (!protocolUsername.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string suffix = protocolUsername.Substring(prefix.Length);
                if (string.IsNullOrWhiteSpace(suffix))
                {
                    return false;
                }

                accountName = suffix;
                profile = candidate;
                return true;
            }

            return false;
        }

        public static bool IsProtocolUsernameForAccount(
            string protocolUsername,
            string accountName,
            byte regionType)
        {
            if (string.IsNullOrEmpty(protocolUsername) || string.IsNullOrEmpty(accountName) ||
                !TryGetProfile(regionType, out ClientLanguageProfile profile))
            {
                return false;
            }

            return string.Equals(
                protocolUsername,
                profile.ProtocolPrefix + "_" + accountName,
                StringComparison.Ordinal);
        }
    }
}