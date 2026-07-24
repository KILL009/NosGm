// SPDX-License-Identifier: MIT

namespace NosGM.Web.Localization;

public static partial class PortalText
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Catalogs =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["es"] = CreateEs(),
            ["en"] = CreateEn(),
            ["de"] = CreateDe(),
            ["fr"] = CreateFr(),
            ["it"] = CreateIt(),
            ["pl"] = CreatePl(),
            ["cs"] = CreateCs(),
            ["ru"] = CreateRu(),
            ["ja"] = CreateJa(),
            ["zh-CN"] = CreateZhCn()
        };

    public static IReadOnlyCollection<string> Keys => Catalogs[PortalCulture.DefaultLanguage].Keys.ToArray();

    public static IReadOnlyCollection<string> Languages => Catalogs.Keys.ToArray();

    public static string Get(string language, string key)
    {
        var normalized = PortalCulture.Normalize(language);
        if (Catalogs.TryGetValue(normalized, out var catalog) && catalog.TryGetValue(key, out var translated))
        {
            return translated;
        }

        return Catalogs[PortalCulture.DefaultLanguage].TryGetValue(key, out var fallback)
            ? fallback
            : key;
    }

    public static void ValidateCatalogs()
    {
        var expected = new HashSet<string>(Catalogs[PortalCulture.DefaultLanguage].Keys, StringComparer.Ordinal);
        foreach (var (language, catalog) in Catalogs)
        {
            var actual = new HashSet<string>(catalog.Keys, StringComparer.Ordinal);
            if (!expected.SetEquals(actual))
            {
                throw new InvalidOperationException($"Language catalog {language} is incomplete.");
            }
        }

        if (Catalogs.Count != 10)
        {
            throw new InvalidOperationException("NosGM Web must ship exactly ten language catalogs.");
        }
    }
}
