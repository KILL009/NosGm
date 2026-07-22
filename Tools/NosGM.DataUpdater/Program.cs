/*
 * Execution pipeline adapted from noszanou/BCardGistUpdater at commit
 * 53153c990ae5b65a603d223eeda504df2a67d5fb.
 * Copyright (C) noszanou and BCardGistUpdater contributors.
 * Modifications Copyright (C) 2026 NosGM contributors.
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NosGM.DataUpdater.Diff;
using NosGM.DataUpdater.Extraction;
using NosGM.DataUpdater.Publishing;
using Za.NosGame.Fetcher;

namespace NosGM.DataUpdater;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        try
        {
            var options = UpdaterOptions.FromEnvironment(args);
            options.Validate();

            await using var services = new ServiceCollection()
                .AddNosGMDataUpdater(options)
                .BuildServiceProvider();

            if (options.DownloadClientResources)
            {
                Console.WriteLine("Downloading and extracting current NosTale resources...");
                await services.GetRequiredService<FileFetcher>().ExecuteAsync();
            }
            else
            {
                Console.WriteLine("Using local resources from the configured working directory.");
            }

            var generation = await services.GetRequiredService<BCardCatalogExtractor>().ExtractAsync();
            var plan = await services.GetRequiredService<CatalogDiffPlanner>().BuildAsync(generation);

            Console.WriteLine($"Generated {generation.Files.Count} language catalogs from {generation.SourceFile}.");
            Console.WriteLine($"Source SHA-256: {generation.SourceSha256}");
            Console.WriteLine($"Changed files: {plan.ChangedFiles.Count}; unchanged files: {plan.UnchangedFiles}.");

            if (generation.UnsupportedLanguages.Count > 0)
            {
                Console.WriteLine(
                    $"Unsupported configured language codes were skipped: {string.Join(", ", generation.UnsupportedLanguages)}");
            }

            if (!options.Publish)
            {
                await WritePreviewAsync(options, plan);
            }

            await services.GetRequiredService<GitHubPullRequestPublisher>()
                .PublishAsync(plan, generation.SourceSha256);

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static async Task WritePreviewAsync(
        UpdaterOptions options,
        Models.RepositoryUpdatePlan plan)
    {
        if (!plan.HasChanges)
        {
            return;
        }

        var previewRoot = Path.Combine(options.WorkingDirectory, "preview");
        foreach (var file in plan.ChangedFiles)
        {
            var path = Path.Combine(previewRoot, file.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException($"Could not determine preview directory for {path}."));
            await File.WriteAllTextAsync(path, file.Value, new UTF8Encoding(false));
        }

        Console.WriteLine($"Dry-run preview written to: {previewRoot}");
    }
}
