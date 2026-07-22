using NosGm.Core;
using NosGm.DAL;
using NosGm.DAL.DAO;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace NosGm.World
{
    /// <summary>
    /// Local, allowlisted Discord-to-World bridge. It never accepts raw console packets.
    /// Requests require a timestamp, one-use nonce and HMAC-SHA256 signature.
    /// </summary>
    public sealed class DiscordGmBridge : IDisposable
    {
        private const int MaxBodyBytes = 32 * 1024;
        private readonly HttpListener _listener = new HttpListener();
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, long> _nonces = new ConcurrentDictionary<string, long>();
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = MaxBodyBytes };
        private readonly string _secret;
        private readonly string _auditPath;
        private readonly object _auditLock = new object();

        private DiscordGmBridge(string prefix, string secret)
        {
            _secret = secret;
            _listener.Prefixes.Add(prefix.EndsWith("/", StringComparison.Ordinal) ? prefix : prefix + "/");
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            _auditPath = Path.Combine(logDirectory, "discord-gm-audit.jsonl");
        }

        public static DiscordGmBridge StartFromEnvironment()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("NOSGM_GM_BRIDGE_ENABLED"), "true", StringComparison.OrdinalIgnoreCase))
                return null;

            var secret = Environment.GetEnvironmentVariable("NOSGM_GM_BRIDGE_SECRET");
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
                throw new InvalidOperationException("NOSGM_GM_BRIDGE_SECRET must contain at least 32 characters.");

            var prefix = Environment.GetEnvironmentVariable("NOSGM_GM_BRIDGE_PREFIX") ?? "http://127.0.0.1:8787/";
            if (!prefix.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase) &&
                !prefix.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The GM bridge must bind to 127.0.0.1/localhost. Use a private reverse proxy for remote hosts.");

            var bridge = new DiscordGmBridge(prefix, secret);
            bridge._listener.Start();
            Task.Run(() => bridge.ListenLoopAsync());
            Logger.Info("Discord GM bridge listening on " + prefix);
            return bridge;
        }

        private async Task ListenLoopAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync().ConfigureAwait(false);
                    Task.Run(() => HandleAsync(context));
                }
                catch (HttpListenerException) when (_stop.IsCancellationRequested) { }
                catch (ObjectDisposedException) when (_stop.IsCancellationRequested) { }
                catch (Exception ex) { Logger.Error("Discord GM bridge listener error", ex); }
            }
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            CommandRequest request = null;
            try
            {
                if (context.Request.HttpMethod != "POST" || context.Request.Url.AbsolutePath != "/v1/commands")
                    throw new BridgeException(404, "Route not found.");
                if (context.Request.ContentLength64 < 0 || context.Request.ContentLength64 > MaxBodyBytes)
                    throw new BridgeException(413, "Invalid request size.");

                string body;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8))
                    body = await reader.ReadToEndAsync().ConfigureAwait(false);
                if (Encoding.UTF8.GetByteCount(body) > MaxBodyBytes) throw new BridgeException(413, "Request too large.");

                Authenticate(context.Request, body);
                request = _json.Deserialize<CommandRequest>(body);
                ValidateEnvelope(request);
                var result = Execute(request);
                Audit(request, true, result.message);
                await WriteAsync(context.Response, 200, result).ConfigureAwait(false);
            }
            catch (BridgeException ex)
            {
                Audit(request, false, ex.Message);
                await WriteAsync(context.Response, ex.Status, Response.Fail(request == null ? null : request.requestId, ex.Message)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error("Discord GM bridge command failed", ex);
                Audit(request, false, ex.GetType().Name + ": " + ex.Message);
                await WriteAsync(context.Response, 500, Response.Fail(request == null ? null : request.requestId, "Internal World Server error.")).ConfigureAwait(false);
            }
        }

        private void Authenticate(HttpListenerRequest request, string body)
        {
            var timestampText = request.Headers["X-NosGM-Timestamp"];
            var nonce = request.Headers["X-NosGM-Nonce"];
            var supplied = request.Headers["X-NosGM-Signature"];
            long timestamp;
            if (!long.TryParse(timestampText, NumberStyles.None, CultureInfo.InvariantCulture, out timestamp) ||
                string.IsNullOrWhiteSpace(nonce) || nonce.Length > 100 || string.IsNullOrWhiteSpace(supplied))
                throw new BridgeException(401, "Missing authentication headers.");

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(now - timestamp) > 60) throw new BridgeException(401, "Expired request.");
            string expected;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret)))
                expected = ToHex(hmac.ComputeHash(Encoding.UTF8.GetBytes(timestampText + "\n" + nonce + "\n" + body)));
            if (!FixedTimeEquals(expected, supplied.ToLowerInvariant())) throw new BridgeException(401, "Invalid signature.");

            // Only authenticated requests may consume nonce storage. This prevents
            // unauthenticated callers from filling the replay cache.
            CleanupNonces(now);
            if (!_nonces.TryAdd(nonce, now)) throw new BridgeException(409, "Replayed request.");
        }

        private Response Execute(CommandRequest request)
        {
            var args = request.arguments ?? new Dictionary<string, object>();
            switch (request.command)
            {
                case "status":
                    return Response.Ok(request.requestId, "World Server online.", new Dictionary<string, object> { { "onlinePlayers", ServerManager.Instance.Sessions.Count() }, { "channelId", ServerManager.Instance.ChannelId } });
                case "players":
                    {
                        var sessions = ServerManager.Instance.Sessions.Where(s => s.Character != null).ToList();
                        var names = sessions.Select(s => s.Character.Name).OrderBy(n => n).Take(50).ToList();
                        var message = sessions.Count == 0
                            ? "No characters are currently online."
                            : "Online (" + sessions.Count + "): " + string.Join(", ", names) + (sessions.Count > names.Count ? " …" : string.Empty);
                        return Response.Ok(request.requestId, message, new Dictionary<string, object> { { "onlinePlayers", sessions.Count }, { "players", names } });
                    }
                case "player":
                    {
                        var character = CharacterByName(Text(args, "target", 1, 32));
                        var online = ServerManager.Instance.GetSessionByCharacterName(character.Name) != null;
                        var message = character.Name + " — Level " + character.Level + ", Hero " + character.HeroLevel + ", Job " + character.JobLevel + ", class " + character.Class + ", " + (online ? "online" : "offline") + ".";
                        return Response.Ok(request.requestId, message, new Dictionary<string, object>
                    {
                        { "characterId", character.CharacterId }, { "name", character.Name }, { "level", character.Level },
                        { "heroLevel", character.HeroLevel }, { "jobLevel", character.JobLevel }, { "class", character.Class.ToString() }, { "online", online }
                    });
                    }
                case "position":
                    {
                        var character = CharacterByName(Text(args, "target", 1, 32));
                        var online = ServerManager.Instance.GetSessionByCharacterName(character.Name) != null;
                        var message = character.Name + " — map " + character.MapId + " at (" + character.MapX + ", " + character.MapY + ") [" + (online ? "online" : "last saved") + "].";
                        return Response.Ok(request.requestId, message, new Dictionary<string, object>
                    {
                        { "name", character.Name }, { "mapId", character.MapId }, { "x", character.MapX }, { "y", character.MapY }, { "online", online }
                    });
                    }
                case "server":
                    {
                        using (var process = Process.GetCurrentProcess())
                        {
                            var uptime = DateTime.Now - process.StartTime;
                            var memoryMb = Math.Round(process.WorkingSet64 / 1024d / 1024d, 1);
                            var online = ServerManager.Instance.Sessions.Count(s => s.Character != null);
                            var message = "World online — channel " + ServerManager.Instance.ChannelId + ", " + online + " players, uptime " +
                                          ((int)uptime.TotalDays) + "d " + uptime.Hours + "h " + uptime.Minutes + "m, memory " + memoryMb.ToString("0.0", CultureInfo.InvariantCulture) + " MB.";
                            return Response.Ok(request.requestId, message, new Dictionary<string, object>
                        {
                            { "channelId", ServerManager.Instance.ChannelId }, { "onlinePlayers", online },
                            { "uptimeSeconds", (long)uptime.TotalSeconds }, { "workingSetMb", memoryMb }
                        });
                        }
                    }
                case "inventory":
                    {
                        var character = CharacterByName(Text(args, "target", 1, 32));
                        var items = DAOFactory.ItemInstanceDAO.LoadByCharacterId(character.CharacterId)
                            .Where(i => i.Amount > 0).OrderBy(i => i.Type).ThenBy(i => i.Slot).Take(25).ToList();
                        if (items.Count == 0) return Response.Ok(request.requestId, character.Name + " has no stored inventory items.", new Dictionary<string, object> { { "items", new List<object>() } });
                        var lines = items.Select(i => "• " + i.Type + " slot " + i.Slot + ": VNum " + i.ItemVNum + " ×" + i.Amount +
                                                         (i.Rare != 0 ? " R" + i.Rare : string.Empty) + (i.Upgrade != 0 ? " +" + i.Upgrade : string.Empty)).ToList();
                        var message = character.Name + " inventory (first " + items.Count + "):\n" + string.Join("\n", lines);
                        var dataItems = items.Select(i => (object)new Dictionary<string, object>
                    {
                        { "vnum", i.ItemVNum }, { "amount", i.Amount }, { "type", i.Type.ToString() }, { "slot", i.Slot },
                        { "rare", i.Rare }, { "upgrade", i.Upgrade }, { "design", i.Design }
                    }).ToList();
                        return Response.Ok(request.requestId, message, new Dictionary<string, object> { { "character", character.Name }, { "items", dataItems } });
                    }
                case "whisper":
                    {
                        var target = Text(args, "target", 1, 32);
                        var message = Text(args, "message", 1, 300);
                        var session = ServerManager.Instance.GetSessionByCharacterName(target);
                        if (session == null) throw new BridgeException(404, "Character is not online.");
                        session.SendPacket(UserInterfaceHelper.GenerateMsg("[GM Discord] " + message, 0));
                        return Response.Ok(request.requestId, "Private GM message sent to " + target + ".");
                    }
                case "unstuck":
                    {
                        var target = Text(args, "target", 1, 32);
                        var mapId = (short)Number(args, "mapId", 0, short.MaxValue);
                        var x = (short)Number(args, "x", 0, short.MaxValue);
                        var y = (short)Number(args, "y", 0, short.MaxValue);
                        Text(args, "reason", 1, 300);
                        var session = ServerManager.Instance.GetSessionByCharacterName(target);
                        if (session == null) throw new BridgeException(404, "Character is not online.");
                        if (ServerManager.GetBaseMapInstanceIdByMapId(mapId) == Guid.Empty) throw new BridgeException(404, "Map does not exist.");
                        ServerManager.Instance.ChangeMap(session.Character.CharacterId, mapId, x, y);
                        return Response.Ok(request.requestId, target + " was rescued to map " + mapId + " (" + x + ", " + y + ").");
                    }
                case "history":
                    {
                        var character = CharacterByName(Text(args, "target", 1, 32));
                        var logs = ServerManager.Instance.PenaltyLogs.Where(p => p.AccountId == character.AccountId)
                            .OrderByDescending(p => p.DateStart).Take(10).ToList();
                        if (logs.Count == 0) return Response.Ok(request.requestId, "No penalties found for " + character.Name + ".", new Dictionary<string, object> { { "penalties", new List<object>() } });
                        var lines = logs.Select(p => "• " + p.Penalty + " | " + p.DateStart.ToString("yyyy-MM-dd HH:mm") + " → " + p.DateEnd.ToString("yyyy-MM-dd HH:mm") +
                                                         " | " + OneLine(p.Reason, 100) + " | " + OneLine(p.AdminName, 60)).ToList();
                        var dataPenalties = logs.Select(p => (object)new Dictionary<string, object>
                    {
                        { "penalty", p.Penalty.ToString() }, { "start", p.DateStart.ToString("yyyy-MM-dd HH:mm") },
                        { "end", p.DateEnd.ToString("yyyy-MM-dd HH:mm") }, { "reason", OneLine(p.Reason, 100) },
                        { "admin", OneLine(p.AdminName, 60) }
                    }).ToList();
                        return Response.Ok(request.requestId, character.Name + " penalties (latest " + logs.Count + "):\n" + string.Join("\n", lines),
                            new Dictionary<string, object> { { "count", logs.Count }, { "penalties", dataPenalties } });
                    }
                case "announce":
                    {
                        var message = Text(args, "message", 1, 500);
                        foreach (var session in ServerManager.Instance.Sessions.Where(s => s.Character != null))
                            session.SendPacket(UserInterfaceHelper.GenerateMsg("[Discord] " + message, 0));
                        return Response.Ok(request.requestId, "Global announcement sent.");
                    }
                case "kick":
                    {
                        var target = Text(args, "target", 1, 32);
                        if (ServerManager.Instance.GetSessionByCharacterName(target) == null) throw new BridgeException(404, "Character is not online.");
                        ServerManager.Instance.Kick(target);
                        return Response.Ok(request.requestId, target + " was disconnected.");
                    }
                case "teleport":
                    {
                        var target = Text(args, "target", 1, 32);
                        var mapId = (short)Number(args, "mapId", 0, short.MaxValue);
                        var x = (short)Number(args, "x", 0, short.MaxValue);
                        var y = (short)Number(args, "y", 0, short.MaxValue);
                        var session = ServerManager.Instance.GetSessionByCharacterName(target);
                        if (session == null) throw new BridgeException(404, "Character is not online.");
                        if (ServerManager.GetBaseMapInstanceIdByMapId(mapId) == Guid.Empty) throw new BridgeException(404, "Map does not exist.");
                        ServerManager.Instance.ChangeMap(session.Character.CharacterId, mapId, x, y);
                        return Response.Ok(request.requestId, target + " was teleported.");
                    }
                case "mute":
                    {
                        var target = Text(args, "target", 1, 32);
                        var minutes = Number(args, "minutes", 1, 43200);
                        var reason = Text(args, "reason", 1, 300);
                        var character = CharacterByName(target);
                        Character.InsertOrUpdatePenalty(new PenaltyLogDTO { AccountId = character.AccountId, Reason = reason, Penalty = PenaltyType.Muted, DateStart = DateTime.Now, DateEnd = DateTime.Now.AddMinutes(minutes), AdminName = Admin(request) });
                        return Response.Ok(request.requestId, target + " was muted for " + minutes + " minutes.");
                    }
                case "unmute":
                    EndPenalty(CharacterByName(Text(args, "target", 1, 32)), PenaltyType.Muted, "Character is not muted.");
                    return Response.Ok(request.requestId, "Active mute removed.");
                case "ban":
                    {
                        var target = Text(args, "target", 1, 32);
                        var days = Number(args, "days", 0, 3650);
                        var reason = Text(args, "reason", 1, 300);
                        var character = CharacterByName(target);
                        ServerManager.Instance.Kick(target);
                        Character.InsertOrUpdatePenalty(new PenaltyLogDTO { AccountId = character.AccountId, Reason = reason, Penalty = PenaltyType.Banned, DateStart = DateTime.Now, DateEnd = days == 0 ? DateTime.Now.AddYears(15) : DateTime.Now.AddDays(days), AdminName = Admin(request) });
                        if (!ServerManager.Instance.BannedCharacters.Contains(character.CharacterId)) ServerManager.Instance.BannedCharacters.Add(character.CharacterId);
                        return Response.Ok(request.requestId, target + " was banned.");
                    }
                case "unban":
                    {
                        var character = CharacterByName(Text(args, "target", 1, 32));
                        EndPenalty(character, PenaltyType.Banned, "Character is not banned.");
                        ServerManager.Instance.BannedCharacters.Remove(character.CharacterId);
                        return Response.Ok(request.requestId, "Active ban removed.");
                    }
                case "link-challenge":
                    {
                        var target = Text(args, "target", 1, 32);
                        var code = Text(args, "code", 8, 8).ToUpperInvariant();
                        if (code.Any(c => !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9')))
                            throw new BridgeException(400, "Invalid link code.");
                        var session = ServerManager.Instance.GetSessionByCharacterName(target);
                        if (session == null || session.Character == null) throw new BridgeException(404, "Character is not online.");
                        session.SendPacket(UserInterfaceHelper.GenerateMsg("[NosGM Link] Discord verification code: " + code, 0));
                        return Response.Ok(request.requestId, "Verification code sent privately in-game.", new Dictionary<string, object>
                    {
                        { "characterId", session.Character.CharacterId }, { "name", session.Character.Name },
                        { "level", session.Character.Level }, { "heroLevel", session.Character.HeroLevel },
                        { "jobLevel", session.Character.JobLevel }, { "class", session.Character.Class.ToString() }, { "online", true }
                    });
                    }
                case "give-item":
                    throw new BridgeException(503, "Item delivery is disabled until persistent idempotency is configured.");
                case "shutdown":
                    throw new BridgeException(503, "Remote shutdown is disabled until the World Server shutdown lifecycle is validated.");
                default:
                    throw new BridgeException(400, "Command is not allowlisted.");
            }
        }

        private CharacterDTO CharacterByName(string userName)
        {
            var character = DAOFactory.CharacterDAO.LoadByName(userName);
            if (character == null) throw new BridgeException(404, "Character not found.");
            return character;
        }

        private void EndPenalty(CharacterDTO character, PenaltyType type, string error)
        {
            var log = ServerManager.Instance.PenaltyLogs.LastOrDefault(p => p.AccountId == character.AccountId && p.Penalty == type && p.DateEnd > DateTime.Now);
            if (log == null) throw new BridgeException(409, error);
            log.DateEnd = DateTime.Now.AddSeconds(-1);
            Character.InsertOrUpdatePenalty(log);
        }

        private static string Admin(CommandRequest request) { return "Discord:" + (request.actor == null ? "unknown" : request.actor.discordTag ?? request.actor.discordUserId ?? "unknown"); }
        private static string Text(IDictionary<string, object> args, string key, int min, int max)
        {
            object value; var text = args.TryGetValue(key, out value) ? Convert.ToString(value, CultureInfo.InvariantCulture).Trim() : string.Empty;
            if (text.Length < min || text.Length > max) throw new BridgeException(400, "Invalid " + key + ".");
            return text;
        }
        private static int Number(IDictionary<string, object> args, string key, int min, int max)
        {
            object value; int number;
            if (!args.TryGetValue(key, out value) || !int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out number) || number < min || number > max)
                throw new BridgeException(400, "Invalid " + key + ".");
            return number;
        }
        private static string OneLine(string value, int max)
        {
            var text = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            return text.Length <= max ? text : text.Substring(0, max) + "…";
        }
        private static void ValidateEnvelope(CommandRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.requestId) || request.requestId.Length > 80 || request.actor == null || string.IsNullOrWhiteSpace(request.actor.discordUserId) || string.IsNullOrWhiteSpace(request.command))
                throw new BridgeException(400, "Invalid command envelope.");
        }
        private void CleanupNonces(long now) { foreach (var pair in _nonces.Where(p => now - p.Value > 120)) { long ignored; _nonces.TryRemove(pair.Key, out ignored); } }
        private static string ToHex(byte[] bytes) { var b = new StringBuilder(bytes.Length * 2); foreach (var value in bytes) b.Append(value.ToString("x2")); return b.ToString(); }
        private static bool FixedTimeEquals(string left, string right) { if (left.Length != right.Length) return false; var diff = 0; for (var i = 0; i < left.Length; i++) diff |= left[i] ^ right[i]; return diff == 0; }

        private void Audit(CommandRequest request, bool ok, string message)
        {
            var entry = new Dictionary<string, object> { { "at", DateTime.UtcNow.ToString("o") }, { "ok", ok }, { "requestId", request == null ? null : request.requestId }, { "discordUserId", request == null || request.actor == null ? null : request.actor.discordUserId }, { "discordTag", request == null || request.actor == null ? null : request.actor.discordTag }, { "command", request == null ? null : request.command }, { "arguments", request == null ? null : request.arguments }, { "message", message } };
            lock (_auditLock) File.AppendAllText(_auditPath, _json.Serialize(entry) + Environment.NewLine, Encoding.UTF8);
        }
        private async Task WriteAsync(HttpListenerResponse response, int status, Response payload)
        {
            var bytes = Encoding.UTF8.GetBytes(_json.Serialize(payload));
            response.StatusCode = status; response.ContentType = "application/json; charset=utf-8"; response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false); response.Close();
        }
        public void Dispose() { _stop.Cancel(); if (_listener.IsListening) _listener.Stop(); _listener.Close(); _stop.Dispose(); }

        private sealed class BridgeException : Exception { public int Status { get; private set; } public BridgeException(int status, string message) : base(message) { Status = status; } }
        public sealed class CommandRequest { public string requestId { get; set; } public Actor actor { get; set; } public string command { get; set; } public Dictionary<string, object> arguments { get; set; } }
        public sealed class Actor { public string discordUserId { get; set; } public string discordTag { get; set; } }
        public sealed class Response
        {
            public bool ok { get; set; }
            public string requestId { get; set; }
            public string message { get; set; }
            public Dictionary<string, object> data { get; set; }
            public static Response Ok(string id, string message, Dictionary<string, object> data = null) { return new Response { ok = true, requestId = id, message = message, data = data }; }
            public static Response Fail(string id, string message) { return new Response { ok = false, requestId = id, message = message }; }
        }
    }
}
