/*
 * Derived from the design of noszanou/BCardGistUpdater at commit
 * 53153c990ae5b65a603d223eeda504df2a67d5fb.
 * Copyright (C) noszanou and BCardGistUpdater contributors.
 * Modifications Copyright (C) 2026 NosGM contributors.
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace NosGM.DataUpdater.Translation;

public interface IBCardTranslationProvider
{
    bool SupportsLanguage(string language);

    string Translate(string language, string key);
}
