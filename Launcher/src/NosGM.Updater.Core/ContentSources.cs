// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Http.Headers;

namespace NosGM.Updater.Core;

public sealed class HttpContentSource : IContentSource
{
    private readonly Uri _baseUri;
    private readonly HttpClient _client;

    public HttpContentSource(Uri baseUri, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        if (!baseUri.IsAbsoluteUri ||
            !string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(baseUri.UserInfo) ||
            !string.IsNullOrEmpty(baseUri.Query) ||
            !string.IsNullOrEmpty(baseUri.Fragment))
        {
            throw new ArgumentException("Content base URI must be an absolute clean HTTPS URI.", nameof(baseUri));
        }

        var normalized = baseUri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? baseUri
            : new Uri(baseUri.AbsoluteUri + '/', UriKind.Absolute);
        _baseUri = normalized;

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            CheckCertificateRevocationList = true
        };

        _client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout ?? TimeSpan.FromMinutes(10)
        };
        _client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("NosGM-Launcher", "1.0"));
    }

    public async Task DownloadVerifiedAsync(
        ReleaseFile file,
        string destinationPath,
        IProgress<UpdateProgress>? progress,
        long alreadyCompletedBytes,
        long totalBytes,
        int completedFiles,
        int totalFiles,
        CancellationToken cancellationToken)
    {
        var relativeUrl = SafePaths.NormalizeRelativePath(file.Url);
        var escaped = string.Join('/', relativeUrl.Split('/').Select(Uri.EscapeDataString));
        var requestUri = new Uri(_baseUri, escaped);
        EnsureSameOriginAndBasePath(requestUri);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var response = await _client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is long contentLength && contentLength != file.Size)
        {
            throw new InvalidDataException(
                $"Remote content length for '{file.Path}' does not match the signed manifest.");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        long currentFileBytes = 0;
        var result = await Hashing.CopyBoundedAndHashAsync(
            source,
            destinationPath,
            file.Size,
            bytes =>
            {
                currentFileBytes += bytes;
                progress?.Report(new UpdateProgress(
                    "download",
                    file.Path,
                    alreadyCompletedBytes + currentFileBytes,
                    totalBytes,
                    completedFiles,
                    totalFiles));
            },
            cancellationToken);

        if (!string.Equals(result.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"SHA-256 verification failed for '{file.Path}'.");
        }
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    private void EnsureSameOriginAndBasePath(Uri uri)
    {
        if (!string.Equals(uri.Scheme, _baseUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, _baseUri.Host, StringComparison.OrdinalIgnoreCase) ||
            uri.Port != _baseUri.Port ||
            !uri.AbsolutePath.StartsWith(_baseUri.AbsolutePath, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Release file URL escaped the trusted content origin or base path.");
        }
    }
}

public sealed class DirectoryContentSource : IContentSource
{
    private readonly string _root;

    public DirectoryContentSource(string rootPath)
    {
        _root = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task DownloadVerifiedAsync(
        ReleaseFile file,
        string destinationPath,
        IProgress<UpdateProgress>? progress,
        long alreadyCompletedBytes,
        long totalBytes,
        int completedFiles,
        int totalFiles,
        CancellationToken cancellationToken)
    {
        var sourcePath = SafePaths.ResolveManagedPath(_root, file.Url);
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 128 * 1024,
            useAsync: true);

        long currentFileBytes = 0;
        var result = await Hashing.CopyBoundedAndHashAsync(
            source,
            destinationPath,
            file.Size,
            bytes =>
            {
                currentFileBytes += bytes;
                progress?.Report(new UpdateProgress(
                    "download",
                    file.Path,
                    alreadyCompletedBytes + currentFileBytes,
                    totalBytes,
                    completedFiles,
                    totalFiles));
            },
            cancellationToken);

        if (!string.Equals(result.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"SHA-256 verification failed for '{file.Path}'.");
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
