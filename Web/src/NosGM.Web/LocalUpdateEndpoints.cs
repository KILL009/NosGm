// SPDX-License-Identifier: MIT

using System.Net;

namespace NosGM.Web;

internal static class LocalUpdateEndpoints
{
    private const long MaximumManifestBytes = 4 * 1024 * 1024;

    public static void MapLocalUpdateEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        var localRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NosGM",
            "Launcher",
            "local-repair-channel");
        var manifestPath = Path.Combine(localRoot, "release-manifest.json");
        var contentRoot = Path.GetFullPath(Path.Combine(localRoot, "content"));

        app.MapGet("/local-update/release-manifest.json", (HttpContext context) =>
        {
            if (!IsLoopbackRequest(context) || !IsSafeRegularFile(manifestPath, localRoot))
            {
                return Results.NotFound();
            }

            var info = new FileInfo(manifestPath);
            if (info.Length <= 0 || info.Length > MaximumManifestBytes)
            {
                return Results.NotFound();
            }

            SetPrivateNoStoreHeaders(context);
            return Results.File(
                manifestPath,
                "application/json; charset=utf-8",
                enableRangeProcessing: false);
        }).ExcludeFromDescription();

        app.MapGet("/local-update/content/{**relativePath}", (
            HttpContext context,
            string? relativePath) =>
        {
            if (!IsLoopbackRequest(context) ||
                string.IsNullOrWhiteSpace(relativePath) ||
                relativePath.Contains('\\') ||
                relativePath.Contains(':') ||
                Path.IsPathRooted(relativePath))
            {
                return Results.NotFound();
            }

            string candidate;
            try
            {
                candidate = Path.GetFullPath(Path.Combine(
                    contentRoot,
                    relativePath.Replace('/', Path.DirectorySeparatorChar)));
            }
            catch (Exception exception) when (
                exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return Results.NotFound();
            }

            var rootPrefix = contentRoot.EndsWith(
                Path.DirectorySeparatorChar.ToString(),
                StringComparison.Ordinal)
                ? contentRoot
                : contentRoot + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) ||
                !IsSafeRegularFile(candidate, contentRoot))
            {
                return Results.NotFound();
            }

            SetPrivateNoStoreHeaders(context);
            return Results.File(
                candidate,
                "application/octet-stream",
                enableRangeProcessing: false);
        }).ExcludeFromDescription();
    }

    private static bool IsLoopbackRequest(HttpContext context)
    {
        var remoteAddress = context.Connection.RemoteIpAddress;
        var localAddress = context.Connection.LocalIpAddress;
        return remoteAddress is not null &&
               localAddress is not null &&
               IPAddress.IsLoopback(remoteAddress) &&
               IPAddress.IsLoopback(localAddress);
    }

    private static bool IsSafeRegularFile(string path, string trustedRoot)
    {
        try
        {
            var fullRoot = Path.GetFullPath(trustedRoot);
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                return false;
            }

            var rootPrefix = fullRoot.EndsWith(
                Path.DirectorySeparatorChar.ToString(),
                StringComparison.Ordinal)
                ? fullRoot
                : fullRoot + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var current = fullPath;
            while (!string.Equals(current, fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    return false;
                }

                current = Path.GetDirectoryName(current)
                          ?? throw new IOException("Local update path escaped its trusted root.");
            }

            return (File.GetAttributes(fullRoot) & FileAttributes.ReparsePoint) == 0;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or ArgumentException or
                NotSupportedException)
        {
            return false;
        }
    }

    private static void SetPrivateNoStoreHeaders(HttpContext context)
    {
        context.Response.Headers["Cache-Control"] = "no-store,private";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }
}
