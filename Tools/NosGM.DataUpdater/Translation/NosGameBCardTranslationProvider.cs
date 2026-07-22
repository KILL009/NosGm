/*
 * Resource translation integration adapted from noszanou/BCardGistUpdater at commit
 * 53153c990ae5b65a603d223eeda504df2a67d5fb.
 * Copyright (C) noszanou and BCardGistUpdater contributors.
 * Modifications Copyright (C) 2026 NosGM contributors.
 * SPDX-License-Identifier: GPL-3.0-only
 */

#if NOSGAME_PACKAGES
using Za.NosGame.RessourceLoader.Traduction;
using Za.NosGame.Shared;
using Za.NosGame.Shared.DatEntitys.Enums;

namespace NosGM.DataUpdater.Translation;

public sealed class NosGameBCardTranslationProvider : IBCardTranslationProvider
{
    private readonly II18NManager _i18nManager;

    public NosGameBCardTranslationProvider(II18NManager i18nManager)
    {
        _i18nManager = i18nManager;
    }

    public bool SupportsLanguage(string language) =>
        Enum.TryParse<RegionLanguageType>(language, true, out _);

    public string Translate(string language, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || string.Equals(key, "NONE", StringComparison.OrdinalIgnoreCase))
        {
            return "NONE";
        }

        if (!Enum.TryParse<RegionLanguageType>(language, true, out var region))
        {
            return key;
        }

        var translated = _i18nManager.GetDataTranslations(GameDataType.BCard, region, key);
        return string.IsNullOrWhiteSpace(translated) ? key : translated;
    }
}
#endif
