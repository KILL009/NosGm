/*
 * Service registration adapted from noszanou/BCardGistUpdater at commit
 * 53153c990ae5b65a603d223eeda504df2a67d5fb.
 * Copyright (C) noszanou and BCardGistUpdater contributors.
 * Modifications Copyright (C) 2026 NosGM contributors.
 * SPDX-License-Identifier: GPL-3.0-only
 */

#if NOSGAME_PACKAGES
using Microsoft.Extensions.DependencyInjection;
using NosGM.DataUpdater.Translation;
using Za.NosGame.Fetcher;
using Za.NosGame.Fetcher.Downloader;
using Za.NosGame.Fetcher.Extractor;
using Za.NosGame.RessourceLoader._Extension;
using Za.NosGame.Shared;
using Za.NosGame.Shared.Loggers;

namespace NosGM.DataUpdater;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNosGamePackageAdapter(
        this IServiceCollection services,
        UpdaterOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        const bool useDefaultManager = true;
        Directory.CreateDirectory(options.WorkingDirectory);

        services.AddSingleton(options);
        services.AddSingleton<IBaseLogger, SerilogLogger>();
        services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<IBaseLogger>();
            return new DatFileFolder(logger)
            {
                RessourceFolder = Path.Combine(options.WorkingDirectory, "Ressources")
            };
        });

        services.AddSingleton(new ConfigManager(useDefaultManager));
        services.AddSingleton(new NosExtractorConfig(true, true, false, false, true, false));
        services.AddTransient<INosExtractor, NosExtractor>();
        services.AddTransient<IClientDownloader, ClientDownloader>();
        services.AddHttpClient();
        services.AddSingleton<FileFetcher>();
        services.AddTraductionRessourceServices(useDefaultManager);
        services.AddBCardRessourceServices(useDefaultManager);
        services.AddSingleton<IBCardTranslationProvider, NosGameBCardTranslationProvider>();
        return services;
    }
}
#endif
