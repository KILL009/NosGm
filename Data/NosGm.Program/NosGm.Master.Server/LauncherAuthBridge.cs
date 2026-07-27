using NosGm.Configuration;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.Master.Library.Interface;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.Master.Server
{
    /// <summary>
    /// Loopback-first HTTP bridge used by the NosGM launcher to exchange a local
    /// account password for a short-lived, one-use Gameforge authorization code.
    /// Deployments should terminate public TLS in a reverse proxy and forward only
    /// this endpoint to the configured loopback prefix.
    /// </summary>
    internal sealed class LauncherAuthBridge : IDisposable
    {
        private const string TicketPath = "/api/v1/launcher/ticket";
        private const int MaximumRequestBytes = 8192;
        private const int MaximumTrackedWindows = 10000;
        private static readonly string DummyPasswordHash =
            PasswordHashService.HashPassword("NosGM-invalid-account-sentinel");

        private sealed class AttemptWindow
        {
            public readonly object SyncRoot = new object();
            public DateTime StartedAtUtc;
            public int Attempts;
        }

        [DataContract]
        private sealed class TicketRequest
        {
            [DataMember(Name = "accountName")]
            public string AccountName { get; set; }

            [DataMember(Name = "password")]
            public string Password { get; set; }

            [DataMember(Name = "installationId")]
            public string InstallationId { get; set; }

            [DataMember(Name = "countryId")]
            public byte CountryId { get; set; }
        }

        [DataContract]
        private sealed class TicketResponse
        {
            [DataMember(Name = "authorizationCode")]
            public string AuthorizationCode { get; set; }

            [DataMember(Name = "accountName")]
            public string AccountName { get; set; }

            [DataMember(Name = "expiresInSeconds")]
            public int ExpiresInSeconds { get; set; }
        }

        [DataContract]
        private sealed class ErrorResponse
        {
            [DataMember(Name = "error")]
            public string Error { get; set; }
        }

        private readonly ConcurrentDictionary<string, AttemptWindow> _attemptWindows =
            new ConcurrentDictionary<string, AttemptWindow>(StringComparer.Ordinal);
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly HttpListener _listener = new HttpListener();
        private Task _acceptLoop;
        private bool _disposed;

        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LauncherAuthBridge));
            }

            string prefix = NormalizePrefix(ServerConfiguration.LauncherAuthBridgePrefix);
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            _acceptLoop = Task.Run(() => AcceptLoopAsync(_cancellation.Token));
            Logger.Info($"Launcher authentication bridge listening on {prefix}");
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (
                    cancellationToken.IsCancellationRequested ||
                    ex is HttpListenerException ||
                    ex is ObjectDisposedException)
                {
                    break;
                }

                _ = Task.Run(() => HandleAsync(context, cancellationToken), cancellationToken);
            }
        }

        private async Task HandleAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                AddSecurityHeaders(context.Response);
                if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.Ordinal) ||
                    !string.Equals(context.Request.Url?.AbsolutePath, TicketPath, StringComparison.Ordinal))
                {
                    await WriteErrorAsync(context.Response, 404, "not_found").ConfigureAwait(false);
                    return;
                }

                if (context.Request.ContentLength64 <= 0 ||
                    context.Request.ContentLength64 > MaximumRequestBytes ||
                    context.Request.ContentType == null ||
                    !context.Request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteErrorAsync(context.Response, 400, "invalid_request").ConfigureAwait(false);
                    return;
                }

                TicketRequest request = await ReadRequestAsync(context.Request, cancellationToken).ConfigureAwait(false);
                if (!TryValidateRequest(request, out Guid installationId))
                {
                    await WriteErrorAsync(context.Response, 400, "invalid_request").ConfigureAwait(false);
                    return;
                }

                string remoteAddress = context.Request.RemoteEndPoint?.Address.ToString() ?? "unknown";
                string limiterKey = remoteAddress + "|" + request.AccountName.ToUpperInvariant();
                if (!TryConsumeAttempt(limiterKey))
                {
                    context.Response.Headers["Retry-After"] = Math.Max(1, ServerConfiguration.LauncherAuthBridgeAttemptWindowSeconds).ToString();
                    await WriteErrorAsync(context.Response, 429, "try_later").ConfigureAwait(false);
                    return;
                }

                AccountDTO account = DAOFactory.AccountDAO.LoadByName(request.AccountName);
                if (account == null ||
                    !string.Equals(account.Name, request.AccountName, StringComparison.Ordinal))
                {
                    PasswordHashService.VerifyPassword(
                        DummyPasswordHash,
                        request.Password,
                        true,
                        out _);
                    await WriteErrorAsync(context.Response, 401, "invalid_credentials").ConfigureAwait(false);
                    return;
                }

                if (!PasswordHashService.VerifyPassword(
                        account.Password,
                        request.Password,
                        true,
                        out bool passwordNeedsUpgrade))
                {
                    await WriteErrorAsync(context.Response, 401, "invalid_credentials").ConfigureAwait(false);
                    return;
                }

                if (ServerConfiguration.MaintenanceMode && account.Authority < AuthorityType.GM)
                {
                    await WriteErrorAsync(context.Response, 503, "maintenance").ConfigureAwait(false);
                    return;
                }

                if (account.Authority == AuthorityType.Banned)
                {
                    await WriteErrorAsync(context.Response, 401, "invalid_credentials").ConfigureAwait(false);
                    return;
                }

                PenaltyLogDTO activeBan = DAOFactory.PenaltyLogDAO.LoadByAccount(account.AccountId)
                    .FirstOrDefault(entry => entry.DateEnd > DateTime.Now && entry.Penalty == PenaltyType.Banned);
                if (activeBan != null)
                {
                    await WriteErrorAsync(context.Response, 401, "invalid_credentials").ConfigureAwait(false);
                    return;
                }

                if (passwordNeedsUpgrade &&
                    PasswordHashService.TryHashPassword(request.Password, out string upgradedPassword))
                {
                    DAOFactory.AccountDAO.TryUpgradePassword(account.AccountId, account.Password, upgradedPassword);
                }

                int ttlSeconds = Math.Max(15, Math.Min(600, ServerConfiguration.GameforgeAuthTicketTtlSeconds));
                string authorizationCode = Guid.NewGuid().ToString("D");
                if (!GameforgeAuthTicketStore.Instance.TryIssue(
                        account.Name,
                        authorizationCode,
                        installationId,
                        request.CountryId,
                        TimeSpan.FromSeconds(ttlSeconds)))
                {
                    await WriteErrorAsync(context.Response, 503, "ticket_unavailable").ConfigureAwait(false);
                    return;
                }

                _attemptWindows.TryRemove(limiterKey, out _);
                await WriteJsonAsync(
                        context.Response,
                        200,
                        new TicketResponse
                        {
                            AuthorizationCode = authorizationCode,
                            AccountName = account.Name,
                            ExpiresInSeconds = ttlSeconds
                        })
                    .ConfigureAwait(false);
            }
            catch (SerializationException)
            {
                await TryWriteErrorAsync(context.Response, 400, "invalid_request").ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Normal during Master shutdown or a cancelled request.
            }
            catch (Exception ex)
            {
                Logger.Error("Launcher authentication bridge request failed", ex);
                await TryWriteErrorAsync(context.Response, 500, "server_error").ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    context.Response.Close();
                }
                catch
                {
                    // The client may disconnect before the response is complete.
                }
            }
        }

        private bool TryConsumeAttempt(string key)
        {
            int windowSeconds = Math.Max(10, Math.Min(600, ServerConfiguration.LauncherAuthBridgeAttemptWindowSeconds));
            int maximumAttempts = Math.Max(1, Math.Min(100, ServerConfiguration.LauncherAuthBridgeMaxAttemptsPerWindow));
            DateTime nowUtc = DateTime.UtcNow;

            if (_attemptWindows.Count > MaximumTrackedWindows)
            {
                foreach (var pair in _attemptWindows)
                {
                    if (pair.Value.StartedAtUtc.AddSeconds(windowSeconds * 2) <= nowUtc)
                    {
                        _attemptWindows.TryRemove(pair.Key, out _);
                    }
                }
            }

            AttemptWindow window = _attemptWindows.GetOrAdd(
                key,
                _ => new AttemptWindow { StartedAtUtc = nowUtc });
            lock (window.SyncRoot)
            {
                if (window.StartedAtUtc.AddSeconds(windowSeconds) <= nowUtc)
                {
                    window.StartedAtUtc = nowUtc;
                    window.Attempts = 0;
                }

                if (window.Attempts >= maximumAttempts)
                {
                    return false;
                }

                window.Attempts++;
                return true;
            }
        }

        private static bool TryValidateRequest(TicketRequest request, out Guid installationId)
        {
            installationId = Guid.Empty;
            return request != null &&
                   !string.IsNullOrWhiteSpace(request.AccountName) &&
                   request.AccountName.Length <= 255 &&
                   request.AccountName.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '\v', '\0' }) < 0 &&
                   request.Password != null &&
                   request.Password.Length <= PasswordHashService.MaximumCredentialLength &&
                   Guid.TryParse(request.InstallationId, out installationId) &&
                   installationId != Guid.Empty &&
                   GameforgeLoginPacketParser.TryGetCulture(request.CountryId, out _);
        }

        private static async Task<TicketRequest> ReadRequestAsync(
            HttpListenerRequest request,
            CancellationToken cancellationToken)
        {
            using (var memory = new MemoryStream())
            {
                var buffer = new byte[2048];
                while (memory.Length <= MaximumRequestBytes)
                {
                    int read = await request.InputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                        .ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }

                    memory.Write(buffer, 0, read);
                }

                if (memory.Length == 0 || memory.Length > MaximumRequestBytes)
                {
                    throw new SerializationException("Launcher authentication request size is invalid.");
                }

                memory.Position = 0;
                var serializer = new DataContractJsonSerializer(typeof(TicketRequest));
                return serializer.ReadObject(memory) as TicketRequest;
            }
        }

        private static void AddSecurityHeaders(HttpListenerResponse response)
        {
            response.Headers["Cache-Control"] = "no-store";
            response.Headers["Pragma"] = "no-cache";
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["Referrer-Policy"] = "no-referrer";
        }

        private static Task WriteErrorAsync(HttpListenerResponse response, int statusCode, string error)
        {
            return WriteJsonAsync(response, statusCode, new ErrorResponse { Error = error });
        }

        private static async Task TryWriteErrorAsync(
            HttpListenerResponse response,
            int statusCode,
            string error)
        {
            try
            {
                await WriteErrorAsync(response, statusCode, error).ConfigureAwait(false);
            }
            catch
            {
                // The requester may have disconnected before the error could be returned.
            }
        }

        private static async Task WriteJsonAsync<T>(HttpListenerResponse response, int statusCode, T value)
        {
            byte[] body;
            using (var memory = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(memory, value);
                body = memory.ToArray();
            }

            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = body.Length;
            await response.OutputStream.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
        }

        private static string NormalizePrefix(string configuredPrefix)
        {
            if (string.IsNullOrWhiteSpace(configuredPrefix) ||
                !Uri.TryCreate(configuredPrefix.Trim(), UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("LauncherAuthBridgePrefix must be an absolute HTTP or HTTPS listener prefix.");
            }

            if (uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback)
            {
                throw new InvalidOperationException("Plain HTTP launcher authentication may bind only to a loopback address.");
            }

            string prefix = uri.AbsoluteUri;
            return prefix.EndsWith("/", StringComparison.Ordinal) ? prefix : prefix + "/";
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cancellation.Cancel();
            try
            {
                _listener.Stop();
                _listener.Close();
                _acceptLoop?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Shutdown must not block the Master process.
            }
            finally
            {
                _cancellation.Dispose();
            }
        }
    }
}
