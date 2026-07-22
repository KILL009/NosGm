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
using NosGM.DataUpdater.Translation;

namespace NosGM.DataUpdater.Extraction;

public sealed class BCardCatalogExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly UpdaterOptions _options;
    private readonly IBCardTranslationProvider _translationProvider;

    public BCardCatalogExtractor(
        UpdaterOptions options,
        IBCardTranslationProvider translationProvider)
    {
        _options = options;
        _translationProvider = translationProvider;
    }

    public async Task<CatalogGenerationResult> ExtractAsync(CancellationToken cancellationToken = default)
    {
        var inputFile = _options.BCardFile;
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException("BCard.dat was not found.", inputFile);
        }

        var sourceBytes = await File.ReadAllBytesAsync(inputFile, cancellationToken);
        var sourceSha256 = Convert.ToHexString(SHA256.HashData(sourceBytes)).ToLowerInvariant();
        var lines = await File.ReadAllLinesAsync(inputFile, Encoding.GetEncoding(1252), cancellationToken);

        var generatedFiles = new List<GeneratedCatalogFile>();
        var unsupportedLanguages = new List<string>();

        foreach (var configuredLanguage in _options.Languages)
        {
            var language = configuredLanguage.ToUpperInvariant();
            if (!_translationProvider.SupportsLanguage(language))
            {
                unsupportedLanguages.Add(language);
                continue;
            }

            var types = Parse(lines, language);
            var document = new BCardCatalogDocument(1, language, sourceSha256, types);
            var content = JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
            var path = $"{_options.OutputRoot}/BCard_{language}.json";
            generatedFiles.Add(new GeneratedCatalogFile(path, content, document));
        }

        if (generatedFiles.Count == 0)
        {
            throw new InvalidOperationException(
                "None of the configured languages have an available translation provider. "
                + "In local mode place BCard_<LANG>.json maps in the configured translation directory.");
        }

        return new CatalogGenerationResult(inputFile, sourceSha256, generatedFiles, unsupportedLanguages);
    }

    private IReadOnlyList<BCardTypeEntry> Parse(string[] lines, string language)
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
                current.Name = Translate(language, value);
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
                    Translate(language, value),
                    Translate(language, descriptionId ?? "NONE")));
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

    private string Translate(string language, string key) =>
        _translationProvider.Translate(language, key);

    private sealed class MutableBCardType(long type)
    {
        public long Type { get; } = type;

        public string Name { get; set; } = "NONE";

        public List<BCardSubtypeEntry> Subtypes { get; } = [];
    }
}
