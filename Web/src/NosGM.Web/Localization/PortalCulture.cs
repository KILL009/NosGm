// SPDX-License-Identifier: MIT

using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;

namespace NosGM.Web.Localization;

public sealed record PortalLanguage(string Code, string DisplayName);

public static class PortalCulture
{
    public const string CookieName = "__Host-nosgm.lang";
    public const string DefaultLanguage = "en";

    private static readonly IReadOnlyDictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["es"] = "es",
            ["en"] = "en",
            ["de"] = "de",
            ["fr"] = "fr",
            ["it"] = "it",
            ["pl"] = "pl",
            ["cs"] = "cs",
            ["cz"] = "cs",
            ["ru"] = "ru",
            ["ja"] = "ja",
            ["jp"] = "ja",
            ["zh"] = "zh-CN",
            ["zh-cn"] = "zh-CN",
            ["cn"] = "zh-CN"
        };

    public static IReadOnlyList<PortalLanguage> SupportedLanguages { get; } =
        Array.AsReadOnly<PortalLanguage>(
        [
            new("es", "Español"),
            new("en", "English"),
            new("de", "Deutsch"),
            new("fr", "Français"),
            new("it", "Italiano"),
            new("pl", "Polski"),
            new("cs", "Čeština"),
            new("ru", "Русский"),
            new("ja", "日本語"),
            new("zh-CN", "简体中文")
        ]);

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultLanguage;
        }

        var candidate = value.Trim();
        if (Aliases.TryGetValue(candidate, out var exact))
        {
            return exact;
        }

        var separator = candidate.IndexOfAny('-', '_');
        if (separator > 0 && Aliases.TryGetValue(candidate[..separator], out var neutral))
        {
            return neutral;
        }

        return DefaultLanguage;
    }

    public static string Current(HttpContext context)
        => context.Items.TryGetValue(typeof(PortalCulture), out var value) && value is string language
            ? language
            : DefaultLanguage;

    public static string BuildLanguageUrl(HttpContext context, string language)
    {
        var query = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in context.Request.Query)
        {
            if (!string.Equals(pair.Key, "lang", StringComparison.OrdinalIgnoreCase))
            {
                query[pair.Key] = pair.Value.ToString();
            }
        }

        query["lang"] = Normalize(language);
        var path = (context.Request.PathBase + context.Request.Path).Value ?? "/";
        return QueryHelpers.AddQueryString(path, query);
    }

    internal static string Resolve(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("lang", out var requested) && requested.Count == 1)
        {
            return Normalize(requested[0]);
        }

        if (context.Request.Cookies.TryGetValue(CookieName, out var cookie))
        {
            return Normalize(cookie);
        }

        var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
        var first = acceptLanguage.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return Normalize(first?.Split(';', 2)[0]);
    }
}

public sealed class PortalCultureMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var language = PortalCulture.Resolve(context);
        context.Items[typeof(PortalCulture)] = language;

        var culture = CultureInfo.GetCultureInfo(language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        if (context.Request.Query.ContainsKey("lang"))
        {
            context.Response.Cookies.Append(
                PortalCulture.CookieName,
                language,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    MaxAge = TimeSpan.FromDays(365),
                    Path = "/"
                });
        }

        context.Response.Headers["Content-Language"] = language;
        await next(context);
    }
}
