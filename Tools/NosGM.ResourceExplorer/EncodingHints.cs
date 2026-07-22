// SPDX-License-Identifier: BSL-1.0

namespace NosGM.ResourceExplorer;

internal static class EncodingHints
{
    public static string ForFileName(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("_code_ru_")) return "windows-1251";
        if (lower.Contains("_code_tr_")) return "windows-1254";
        if (lower.Contains("_code_hk_") || lower.Contains("_code_tw_")) return "big5";
        if (lower.Contains("_code_jp_") || lower.Contains("_code_ja_")) return "shift_jis-or-client-specific";
        if (lower.Contains("_code_cn_") || lower.Contains("_code_zh_")) return "gb18030-or-client-specific";
        if (lower.Contains("_code_de_") || lower.Contains("_code_pl_") || lower.Contains("_code_it_") || lower.Contains("_code_cz_")) return "windows-1250";
        if (lower.Contains("_code_uk_") || lower.Contains("_code_en_") || lower.Contains("_code_fr_") || lower.Contains("_code_es_")) return "windows-1252";
        return "unknown-preserve-bytes";
    }
}
