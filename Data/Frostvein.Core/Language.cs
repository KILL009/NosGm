using Frostvein.Configuration;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

namespace Frostvein.Core
{
    /// <summary>
    /// Resolves server messages for a specific player culture.
    /// The neutral resource file is English and satellite resources (for example fr)
    /// override only the keys they translate.
    /// </summary>
    public sealed class Language
    {
        private static readonly string[] SupportedCultures = { "en", "fr" };

        private static readonly Lazy<Language> LazyInstance =
            new Lazy<Language>(() => new Language());

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
            return GetMessageFromKey(key, _defaultCultureName);
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

            // Older Frostvein configurations used "uk" to mean UK English.
            if (candidate == "uk" || candidate == "gb")
            {
                candidate = "en";
            }

            if (candidate == "english" || candidate.StartsWith("en-"))
            {
                candidate = "en";
            }
            else if (candidate == "french" || candidate == "français" ||
                     candidate == "francais" || candidate.StartsWith("fr-"))
            {
                candidate = "fr";
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
    }
}
