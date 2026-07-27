using NosGm.Configuration;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;

namespace NosGm.Core
{
    /// <summary>
    /// Resolves server messages for a specific player culture.
    /// The neutral resource file is English and satellite resources (for example fr)
    /// override only the keys they translate.
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

            // Accept culture tags, English display names, native names and
            // aliases commonly used by launchers and older NosTale tools.
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
                // The first Chinese catalog is Simplified Chinese. Region-specific
                // aliases deliberately share it until a Traditional catalog exists.
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

    /// <summary>
    /// Maps the official NosTale client region suffix to its Login port and
    /// canonical server culture. The accepted local port is authoritative;
    /// the RegionType byte supplied by the client is compatibility data only.
    /// </summary>
    public static class ClientRegionMap
    {
        private static readonly string[] CulturesByRegion =
        {
            "en", "de", "fr", "it", "pl", "es", "cs", "ru", "ja", "zh"
        };

        public const int BaseLoginPort = 4000;

        public static int RegionCount => CulturesByRegion.Length;

        public static int GetLoginPort(byte regionType)
        {
            if (regionType >= CulturesByRegion.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(regionType));
            }

            return BaseLoginPort + regionType;
        }

        public static bool TryGetCulture(byte regionType, out string culture)
        {
            if (regionType >= CulturesByRegion.Length)
            {
                culture = null;
                return false;
            }

            culture = CulturesByRegion[regionType];
            return true;
        }

        public static bool TryResolveLoginPort(
            int loginPort,
            out byte regionType,
            out string culture)
        {
            int regionIndex = loginPort - BaseLoginPort;
            if (regionIndex < 0 || regionIndex >= CulturesByRegion.Length)
            {
                regionType = 0;
                culture = null;
                return false;
            }

            regionType = (byte)regionIndex;
            culture = CulturesByRegion[regionIndex];
            return true;
        }
    }
}
