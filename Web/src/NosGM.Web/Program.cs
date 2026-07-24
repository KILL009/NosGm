// SPDX-License-Identifier: MIT

using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using NosGM.Web;
using NosGM.Web.Contracts;
using NosGM.Web.Health;
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
builder.Services.AddHealthChecks()
    .AddCheck<PublicSnapshotHealthCheck>("public-snapshot", tags: ["ready"]);
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
builder.Services.AddOptions<PublicDataOptions>()
    .BindConfiguration(PublicDataOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(PublicDataOptions.IsSafe, "Public snapshot configuration is invalid.")
    .ValidateOnStart();
builder.Services.AddSingleton<SignedSnapshotPortalDataSource>();
builder.Services.AddSingleton<IPortalDataSource>(services =>
    services.GetRequiredService<SignedSnapshotPortalDataSource>());
builder.Services.AddSingleton<IPublicDataHealth>(services =>
    services.GetRequiredService<SignedSnapshotPortalDataSource>());
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
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

var versionedPublicApi = app.MapGroup("/api/v1/public")
    .RequireRateLimiting("public-api");
MapPublicApi(versionedPublicApi);

var legacyPublicApi = app.MapGroup("/api/public")
    .RequireRateLimiting("public-api")
    .AddEndpointFilter(async (context, next) =>
    {
        context.HttpContext.Response.Headers["Deprecation"] = "true";
        context.HttpContext.Response.Headers["Sunset"] = "Thu, 31 Dec 2026 23:59:59 GMT";
        context.HttpContext.Response.Headers["Link"] = "</api/v1/public>; rel=\"successor-version\"";
        return await next(context);
    });
MapPublicApi(legacyPublicApi);

app.Run();

static void MapPublicApi(RouteGroupBuilder publicApi)
{
    publicApi.MapGet("/metadata", (HttpContext context, IOptions<PortalOptions> options) =>
    {
        context.Response.Headers["Cache-Control"] = "public,max-age=300";
        var portal = options.Value;
        return Results.Ok(new PublicPortalMetadata(
            portal.ServerName,
            portal.ClientVersion,
            portal.IsLauncherDownloadAvailable,
            PortalCulture.SupportedLanguages.Select(language => language.Code).ToArray(),
            "v1",
            "signed-snapshot"));
    });

    publicApi.MapGet("/news", async (
        HttpContext context,
        IPortalDataSource dataSource,
        string? lang,
        int? limit,
        CancellationToken cancellationToken) =>
    {
        context.Response.Headers["Cache-Control"] = "public,max-age=60,stale-while-revalidate=120";
        var language = PortalCulture.Normalize(lang ?? PortalCulture.Current(context));
        return Results.Ok(await dataSource.GetNewsAsync(
            language,
            Math.Clamp(limit ?? 5, 1, 20),
            cancellationToken));
    });

    publicApi.MapGet("/status", async (
        HttpContext context,
        IPortalDataSource dataSource,
        CancellationToken cancellationToken) =>
    {
        context.Response.Headers["Cache-Control"] = "public,max-age=15,stale-while-revalidate=30";
        return Results.Ok(await dataSource.GetStatusAsync(cancellationToken));
    });

    publicApi.MapGet("/rankings/{kind}", async (
        HttpContext context,
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

        context.Response.Headers["Cache-Control"] = "public,max-age=60,stale-while-revalidate=120";
        var entries = await dataSource.GetRankingsAsync(
            ranking.Value,
            Math.Clamp(limit ?? 20, 1, 50),
            cancellationToken);
        return Results.Ok(entries);
    });
}

public partial class Program { }
