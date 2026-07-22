/*
 * Execution pipeline adapted from noszanou/BCardGistUpdater at commit
 * 53153c990ae5b65a603d223eeda504df2a67d5fb.
 * Copyright (C) noszanou and BCardGistUpdater contributors.
 * Modifications Copyright (C) 2026 NosGM contributors.
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Text;
using NosGM.DataUpdater.Diff;
using NosGM.DataUpdater.Extraction;
using NosGM.DataUpdater.Publishing;
using NosGM.DataUpdater.Translation;

#if NOSGAME_PACKAGES
using Microsoft.Extensions.DependencyInjection;
using Za.NosGame.Fetcher;
using Za.NosGame.Shared;
#endif

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
            return await RunAsync(options);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static async Task<int> RunAsync(UpdaterOptions options)
    {
        if (options.DownloadClientResources)
        {
#if NOSGAME_PACKAGES
            Console.WriteLine("Downloading and extracting current NosTale resources with the optional NosGame package adapter...");
            await using var services = new ServiceCollection()
                .AddNosGamePackageAdapter(options)
                .BuildServiceProvider();

            await services.GetRequiredService<FileFetcher>().ExecuteAsync();
            var folders = services.GetRequiredService<DatFileFolder>();
            var downloadedOptions = options with
            {
                BCardFile = Path.Combine(folders.DatFolder, "BCard.dat")
            };

            return await ExecutePipelineAsync(
                downloadedOptions,
                services.GetRequiredService<IBCardTranslationProvider>());
#else
            throw new InvalidOperationException(
                "Resource downloading is optional and was not compiled into this build. "
                + "Build with -p:EnableNosGamePackages=true and provide a package-read token, "
                + "or run local mode with BCard.dat and translation maps.");
#endif
        }

        Console.WriteLine($"Using local BCard data: {options.BCardFile}");
        Console.WriteLine($"Using local translation maps: {options.TranslationDirectory}");
        return await ExecutePipelineAsync(
            options,
            new JsonBCardTranslationProvider(options.TranslationDirectory));
    }

    private static async Task<int> ExecutePipelineAsync(
        UpdaterOptions options,
        IBCardTranslationProvider translationProvider)
    {
        var generation = await new BCardCatalogExtractor(options, translationProvider).ExtractAsync();
        var plan = await new CatalogDiffPlanner(options).BuildAsync(generation);

        Console.WriteLine($"Generated {generation.Files.Count} language catalogs from {generation.SourceFile}.");
        Console.WriteLine($"Source SHA-256: {generation.SourceSha256}");
        Console.WriteLine($"Changed files: {plan.ChangedFiles.Count}; unchanged files: {plan.UnchangedFiles}.");

        if (generation.UnsupportedLanguages.Count > 0)
        {
            Console.WriteLine(
                $"Languages without an available translation source were skipped: {string.Join(", ", generation.UnsupportedLanguages)}");
        }

        if (!options.Publish)
        {
            await WritePreviewAsync(options, plan);
        }

        await new GitHubPullRequestPublisher(options).PublishAsync(plan, generation.SourceSha256);
        return 0;
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
