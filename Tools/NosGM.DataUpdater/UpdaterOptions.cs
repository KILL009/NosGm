/*
 * Derived from the design of noszanou/BCardGistUpdater at commit
 * 53153c990ae5b65a603d223eeda504df2a67d5fb.
 * Copyright (C) noszanou and BCardGistUpdater contributors.
 * Modifications Copyright (C) 2026 NosGM contributors.
 * SPDX-License-Identifier: GPL-3.0-only
 */

namespace NosGM.DataUpdater;

public sealed record UpdaterOptions(
    string RepositoryOwner,
    string RepositoryName,
    string BaseBranch,
    string RepositoryRoot,
    string WorkingDirectory,
    string BCardFile,
    string TranslationDirectory,
    string OutputRoot,
    IReadOnlyList<string> Languages,
    bool DownloadClientResources,
    bool Publish,
    string? GitHubToken)
{
    private static readonly string[] DefaultLanguages =
    [
        "ES", "EN", "DE", "FR", "IT", "PL", "CZ", "RU", "JP", "CN"
    ];

    public static UpdaterOptions FromEnvironment(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var arguments = args.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var repositoryRoot = GetEnvironment("NOSGM_UPDATER_REPOSITORY_ROOT")
            ?? Environment.GetEnvironmentVariable("GITHUB_WORKSPACE")
            ?? Directory.GetCurrentDirectory();

        var workingDirectory = Path.GetFullPath(
            GetEnvironment("NOSGM_UPDATER_WORK_DIRECTORY")
            ?? Path.Combine(Path.GetTempPath(), "NosGM.DataUpdater"));

        var configuredLanguages = GetEnvironment("NOSGM_UPDATER_LANGUAGES")
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => value.ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new UpdaterOptions(
            RepositoryOwner: GetEnvironment("NOSGM_UPDATER_OWNER") ?? "KILL009",
            RepositoryName: GetEnvironment("NOSGM_UPDATER_REPO") ?? "NosGm",
            BaseBranch: GetEnvironment("NOSGM_UPDATER_BASE_BRANCH") ?? "main",
            RepositoryRoot: Path.GetFullPath(repositoryRoot),
            WorkingDirectory: workingDirectory,
            BCardFile: Path.GetFullPath(
                GetEnvironment("NOSGM_UPDATER_BCARD_FILE")
                ?? Path.Combine(workingDirectory, "input", "BCard.dat")),
            TranslationDirectory: Path.GetFullPath(
                GetEnvironment("NOSGM_UPDATER_TRANSLATION_DIRECTORY")
                ?? Path.Combine(workingDirectory, "input", "translations")),
            OutputRoot: NormalizeRepositoryPath(GetEnvironment("NOSGM_UPDATER_OUTPUT_ROOT") ?? "Data/Generated/BCards"),
            Languages: configuredLanguages is { Length: > 0 } ? configuredLanguages : DefaultLanguages,
            DownloadClientResources: arguments.Contains("--download-resources")
                || IsTrue(GetEnvironment("NOSGM_UPDATER_DOWNLOAD_RESOURCES")),
            Publish: arguments.Contains("--publish") || IsTrue(GetEnvironment("NOSGM_UPDATER_PUBLISH")),
            GitHubToken: GetEnvironment("GITHUB_TOKEN") ?? GetEnvironment("NOSGM_DATA_UPDATER_TOKEN"));
    }

    public void Validate()
    {
        if (!Directory.Exists(RepositoryRoot))
        {
            throw new DirectoryNotFoundException($"Repository root was not found: {RepositoryRoot}");
        }

        if (Languages.Count == 0)
        {
            throw new InvalidOperationException("At least one language must be configured.");
        }

        if (!DownloadClientResources && !File.Exists(BCardFile))
        {
            throw new FileNotFoundException(
                "Local mode requires BCard.dat. Set NOSGM_UPDATER_BCARD_FILE or use --download-resources with the optional package adapter.",
                BCardFile);
        }

        if (Publish && string.IsNullOrWhiteSpace(GitHubToken))
        {
            throw new InvalidOperationException("Publishing requires GITHUB_TOKEN or NOSGM_DATA_UPDATER_TOKEN.");
        }
    }

    private static string? GetEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool IsTrue(string? value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeRepositoryPath(string value) =>
        value.Replace('\\', '/').Trim('/');
}
