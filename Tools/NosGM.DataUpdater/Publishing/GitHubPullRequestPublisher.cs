/*
 * GitHub publishing flow adapted from noszanou/BCardGistUpdater at commit
 * 53153c990ae5b65a603d223eeda504df2a67d5fb.
 * Copyright (C) noszanou and BCardGistUpdater contributors.
 * Modifications Copyright (C) 2026 NosGM contributors.
 * SPDX-License-Identifier: GPL-3.0-only
 */

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NosGM.DataUpdater.Models;

namespace NosGM.DataUpdater.Publishing;

public sealed class GitHubPullRequestPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly UpdaterOptions _options;

    public GitHubPullRequestPublisher(UpdaterOptions options)
    {
        _options = options;
    }

    public async Task<string?> PublishAsync(
        RepositoryUpdatePlan plan,
        string sourceSha256,
        CancellationToken cancellationToken = default)
    {
        if (!plan.HasChanges)
        {
            Console.WriteLine("No BCard data changes were detected. No branch or pull request was created.");
            return null;
        }

        if (!_options.Publish)
        {
            Console.WriteLine($"Dry run: {plan.ChangedFiles.Count} repository files would change.");
            foreach (var path in plan.ChangedFiles.Keys.Order(StringComparer.Ordinal))
            {
                Console.WriteLine($"  {path}");
            }

            return null;
        }

        using var client = CreateClient(_options.GitHubToken!);
        var baseSha = await GetBaseCommitShaAsync(client, cancellationToken);
        var branchName = BuildBranchName(sourceSha256);

        await CreateBranchAsync(client, branchName, baseSha, cancellationToken);

        foreach (var changedFile in plan.ChangedFiles.OrderBy(static file => file.Key, StringComparer.Ordinal))
        {
            var existingSha = await GetFileShaAsync(client, changedFile.Key, _options.BaseBranch, cancellationToken);
            await PutFileAsync(client, branchName, changedFile.Key, changedFile.Value, existingSha, cancellationToken);
        }

        var pullRequestUrl = await CreatePullRequestAsync(
            client,
            branchName,
            sourceSha256,
            plan.PullRequestSummary,
            cancellationToken);

        Console.WriteLine($"Created BCard data pull request: {pullRequestUrl}");
        return pullRequestUrl;
    }

    private HttpClient CreateClient(string token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NosGM-DataUpdater/1.0");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private async Task<string> GetBaseCommitShaAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var url = $"repos/{Escape(_options.RepositoryOwner)}/{Escape(_options.RepositoryName)}/git/ref/heads/{Escape(_options.BaseBranch)}";
        using var response = await client.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.GetProperty("object").GetProperty("sha").GetString()
            ?? throw new InvalidOperationException("GitHub did not return the base commit SHA.");
    }

    private async Task CreateBranchAsync(
        HttpClient client,
        string branchName,
        string baseSha,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            @ref = $"refs/heads/{branchName}",
            sha = baseSha
        };

        using var response = await client.PostAsync(
            $"repos/{Escape(_options.RepositoryOwner)}/{Escape(_options.RepositoryName)}/git/refs",
            JsonContent(payload),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<string?> GetFileShaAsync(
        HttpClient client,
        string path,
        string branch,
        CancellationToken cancellationToken)
    {
        var url = $"repos/{Escape(_options.RepositoryOwner)}/{Escape(_options.RepositoryName)}/contents/{EscapePath(path)}?ref={Uri.EscapeDataString(branch)}";
        using var response = await client.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.GetProperty("sha").GetString();
    }

    private async Task PutFileAsync(
        HttpClient client,
        string branchName,
        string path,
        string content,
        string? existingSha,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["message"] = $"data: update {Path.GetFileName(path)}",
            ["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
            ["branch"] = branchName
        };

        if (!string.IsNullOrWhiteSpace(existingSha))
        {
            payload["sha"] = existingSha;
        }

        using var response = await client.PutAsync(
            $"repos/{Escape(_options.RepositoryOwner)}/{Escape(_options.RepositoryName)}/contents/{EscapePath(path)}",
            JsonContent(payload),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<string> CreatePullRequestAsync(
        HttpClient client,
        string branchName,
        string sourceSha256,
        string summary,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            title = $"data: update BCard catalogs ({sourceSha256[..12]})",
            head = branchName,
            @base = _options.BaseBranch,
            body = summary,
            draft = false
        };

        using var response = await client.PostAsync(
            $"repos/{Escape(_options.RepositoryOwner)}/{Escape(_options.RepositoryName)}/pulls",
            JsonContent(payload),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.GetProperty("html_url").GetString()
            ?? throw new InvalidOperationException("GitHub created the pull request but returned no URL.");
    }

    private static StringContent JsonContent<T>(T payload) =>
        new(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"GitHub API request failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}",
            null,
            response.StatusCode);
    }

    private static string BuildBranchName(string sourceSha256)
    {
        var runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
        var suffix = string.IsNullOrWhiteSpace(runId)
            ? DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss")
            : runId;
        return $"automation/bcard-{sourceSha256[..12]}-{suffix}";
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);

    private static string EscapePath(string path) =>
        string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
}
