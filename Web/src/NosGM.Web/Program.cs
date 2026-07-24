// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using NosGM.Web;
using NosGM.Web.Contracts;
using NosGM.Web.Localization;
using NosGM.Web.Services;

PortalText.ValidateCatalogs();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 1_048_576;
});

builder.Services.AddProblemDetails();
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-NosGM-CSRF";
    options.Cookie.Name = "__Host-nosgm.csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddOptions<PortalOptions>()
    .BindConfiguration(PortalOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(PortalOptions.IsSafe, "Launcher download URL must be empty or a clean HTTPS URL.")
    .ValidateOnStart();
builder.Services.AddSingleton<IPortalDataSource, SafeDemoPortalDataSource>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("public-api", limiter =>
    {
        limiter.PermitLimit = 60;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers["Cache-Control"] = "public,max-age=604800,immutable";
        context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }
});
app.UseRouting();
app.UseMiddleware<PortalCultureMiddleware>();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapRazorPages();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

var publicApi = app.MapGroup("/api/public").RequireRateLimiting("public-api");
publicApi.MapGet("/metadata", (IOptions<PortalOptions> options) =>
{
    var portal = options.Value;
    return Results.Ok(new PublicPortalMetadata(
        portal.ServerName,
        portal.ClientVersion,
        portal.IsLauncherDownloadAvailable,
        PortalCulture.SupportedLanguages.Select(language => language.Code).ToArray()));
});
publicApi.MapGet("/news", async (
    HttpContext context,
    IPortalDataSource dataSource,
    string? lang,
    int? limit,
    CancellationToken cancellationToken) =>
{
    var language = PortalCulture.Normalize(lang ?? PortalCulture.Current(context));
    return Results.Ok(await dataSource.GetNewsAsync(language, Math.Clamp(limit ?? 5, 1, 20), cancellationToken));
});
publicApi.MapGet("/status", async (IPortalDataSource dataSource, CancellationToken cancellationToken)
    => Results.Ok(await dataSource.GetStatusAsync(cancellationToken)));
publicApi.MapGet("/rankings/{kind}", async (
    string kind,
    int? limit,
    IPortalDataSource dataSource,
    CancellationToken cancellationToken) =>
{
    var ranking = kind.ToLowerInvariant() switch
    {
        "combat" => RankingKind.Combat,
        "reputation" => RankingKind.Reputation,
        "hero" => RankingKind.Hero,
        _ => (RankingKind?)null
    };
    if (ranking is null)
    {
        return Results.NotFound();
    }

    var entries = await dataSource.GetRankingsAsync(
        ranking.Value,
        Math.Clamp(limit ?? 20, 1, 50),
        cancellationToken);
    return Results.Ok(entries);
});

app.Run();

public partial class Program { }
