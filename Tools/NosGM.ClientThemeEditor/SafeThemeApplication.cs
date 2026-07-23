// SPDX-License-Identifier: MIT

namespace NosGM.ClientThemeEditor;

internal static class SafeThemeApplication
{
    public static PatchManifest ApplyCopy(
        string inputPath,
        string outputPath,
        ThemeProfile profile,
        ThemeDocument theme)
    {
        var output = Path.GetFullPath(outputPath);
        var manifest = output + ".nosgm-theme-manifest.json";
        if (File.Exists(output) || File.Exists(manifest))
        {
            throw new IOException(
                "Copy mode never overwrites an existing executable or manifest. Choose a new output path.");
        }

        try
        {
            return ThemeEngine.ApplyToOutput(inputPath, output, profile, theme, overwrite: false);
        }
        catch (Exception applicationException)
        {
            var cleanupErrors = new List<Exception>();
            TryDelete(output, cleanupErrors);
            TryDelete(manifest, cleanupErrors);
            if (cleanupErrors.Count > 0)
            {
                throw new IOException(
                    "Theme application failed and incomplete copy artifacts could not be removed.",
                    new AggregateException(new[] { applicationException }.Concat(cleanupErrors)));
            }

            throw;
        }
    }

    private static void TryDelete(string path, ICollection<Exception> errors)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            errors.Add(exception);
        }
    }
}
