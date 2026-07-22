/*
 * Derived from the design of noszanou/BCardGistUpdater at commit
 * 53153c990ae5b65a603d223eeda504df2a67d5fb.
 * Copyright (C) noszanou and BCardGistUpdater contributors.
 * Modifications Copyright (C) 2026 NosGM contributors.
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text.Json;

namespace NosGM.DataUpdater.Translation;

public sealed class JsonBCardTranslationProvider : IBCardTranslationProvider
{
    private readonly string _translationDirectory;
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public JsonBCardTranslationProvider(string translationDirectory)
    {
        _translationDirectory = translationDirectory;
    }

    public bool SupportsLanguage(string language) =>
        File.Exists(GetPath(language));

    public string Translate(string language, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || string.Equals(key, "NONE", StringComparison.OrdinalIgnoreCase))
        {
            return "NONE";
        }

        var translations = GetTranslations(language);
        return translations.TryGetValue(key, out var translated) && !string.IsNullOrWhiteSpace(translated)
            ? translated
            : key;
    }

    private IReadOnlyDictionary<string, string> GetTranslations(string language)
    {
        if (_cache.TryGetValue(language, out var cached))
        {
            return cached;
        }

        var path = GetPath(language);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Translation map was not found for {language}.", path);
        }

        var content = File.ReadAllText(path);
        var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(content)
            ?? throw new InvalidDataException($"Translation map is empty or invalid: {path}");

        _cache[language] = translations;
        return translations;
    }

    private string GetPath(string language) =>
        Path.Combine(_translationDirectory, $"BCard_{language.ToUpperInvariant()}.json");
}
