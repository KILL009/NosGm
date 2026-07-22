/*
 * BCard parsing logic adapted from noszanou/BCardGistUpdater at commit
 * 53153c990ae5b65a603d223eeda504df2a67d5fb.
 * Copyright (C) noszanou and BCardGistUpdater contributors.
 * Modifications Copyright (C) 2026 NosGM contributors.
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NosGM.DataUpdater.Models;
using Za.NosGame.RessourceLoader.Traduction;
using Za.NosGame.Shared;
using Za.NosGame.Shared.DatEntitys.Enums;

namespace NosGM.DataUpdater.Extraction;

public sealed class BCardCatalogExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly II18NManager _i18nManager;
    private readonly DatFileFolder _datFileFolder;
    private readonly UpdaterOptions _options;

    public BCardCatalogExtractor(
        II18NManager i18nManager,
        DatFileFolder datFileFolder,
        UpdaterOptions options)
    {
        _i18nManager = i18nManager;
        _datFileFolder = datFileFolder;
        _options = options;
    }

    public async Task<CatalogGenerationResult> ExtractAsync(CancellationToken cancellationToken = default)
    {
        var inputFile = Path.Combine(_datFileFolder.DatFolder, "BCard.dat");
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException("BCard.dat was not found after resource extraction.", inputFile);
        }

        var sourceBytes = await File.ReadAllBytesAsync(inputFile, cancellationToken);
        var sourceSha256 = Convert.ToHexString(SHA256.HashData(sourceBytes)).ToLowerInvariant();
        var lines = await File.ReadAllLinesAsync(inputFile, Encoding.GetEncoding(1252), cancellationToken);

        var generatedFiles = new List<GeneratedCatalogFile>();
        var unsupportedLanguages = new List<string>();

        foreach (var configuredLanguage in _options.Languages)
        {
            if (!Enum.TryParse<RegionLanguageType>(configuredLanguage, true, out var region))
            {
                unsupportedLanguages.Add(configuredLanguage);
                continue;
            }

            var language = configuredLanguage.ToUpperInvariant();
            var types = Parse(lines, region);
            var document = new BCardCatalogDocument(1, language, sourceSha256, types);
            var content = JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
            var path = $"{_options.OutputRoot}/BCard_{language}.json";
            generatedFiles.Add(new GeneratedCatalogFile(path, content, document));
        }

        if (generatedFiles.Count == 0)
        {
            var available = string.Join(", ", Enum.GetNames<RegionLanguageType>());
            throw new InvalidOperationException(
                $"None of the configured languages are supported. Available enum values: {available}");
        }

        return new CatalogGenerationResult(inputFile, sourceSha256, generatedFiles, unsupportedLanguages);
    }

    private IReadOnlyList<BCardTypeEntry> Parse(string[] lines, RegionLanguageType region)
    {
        var descriptions = new Dictionary<(long Type, string SubjectKey), string>();
        var result = new List<BCardTypeEntry>();
        MutableBCardType? current = null;

        foreach (var line in lines.Where(static value => !string.IsNullOrWhiteSpace(value)))
        {
            var parts = line.Split('\t');
            if (parts.Length < 2)
            {
                continue;
            }

            var key = parts[1];
            var value = parts.Length > 2 ? parts[2] : string.Empty;

            if (string.Equals(key, "VNUM", StringComparison.Ordinal))
            {
                if (!long.TryParse(value, out var type))
                {
                    current = null;
                    continue;
                }

                current = new MutableBCardType(type);
                continue;
            }

            if (current is null)
            {
                continue;
            }

            if (string.Equals(key, "NAME", StringComparison.Ordinal))
            {
                current.Name = Translate(region, value);
            }
            else if (key.StartsWith("SUBJ", StringComparison.Ordinal))
            {
                descriptions[(current.Type, key)] = value;
            }
            else if (key.StartsWith("LIST", StringComparison.Ordinal))
            {
                var encodedSubtype = key[4..].Replace("-", string.Empty, StringComparison.Ordinal);
                if (!long.TryParse(encodedSubtype, out var subtype))
                {
                    continue;
                }

                var subjectKey = $"SUBJ{subtype / 10}";
                descriptions.TryGetValue((current.Type, subjectKey), out var descriptionId);
                current.Subtypes.Add(new BCardSubtypeEntry(
                    subtype,
                    Translate(region, value),
                    Translate(region, descriptionId ?? "NONE")));
            }
            else if (string.Equals(key, "END", StringComparison.Ordinal))
            {
                result.Add(new BCardTypeEntry(
                    current.Type,
                    string.IsNullOrWhiteSpace(current.Name) ? "NONE" : current.Name,
                    current.Subtypes.OrderBy(static entry => entry.Subtype).ToArray()));
                current = null;
            }
        }

        return result.OrderBy(static entry => entry.Type).ToArray();
    }

    private string Translate(RegionLanguageType region, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || string.Equals(key, "NONE", StringComparison.OrdinalIgnoreCase))
        {
            return "NONE";
        }

        var translated = _i18nManager.GetDataTranslations(GameDataType.BCard, region, key);
        return string.IsNullOrWhiteSpace(translated) ? key : translated;
    }

    private sealed class MutableBCardType(long type)
    {
        public long Type { get; } = type;

        public string Name { get; set; } = "NONE";

        public List<BCardSubtypeEntry> Subtypes { get; } = [];
    }
}
