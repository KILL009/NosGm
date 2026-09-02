using NosGm.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace NosGm.World
{
    /// <summary>
    /// Executes only structured commands queued by the NosGM Portal stored procedures.
    /// Arbitrary SQL, packet text, shell commands and reflection are never evaluated.
    /// </summary>
    public sealed class PortalBridgeWorker : IDisposable
    {
        private const int MaximumCommandsPerCycle = 10;
        private const int MaximumParametersJsonLength = 4096;
        private const int MaximumResultLength = 500;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly string _connectionString;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = MaximumParametersJsonLength };
        private readonly string _workerId;
        private Task _task;

        private PortalBridgeWorker(string connectionString)
        {
            _connectionString = connectionString;
            _workerId = BuildWorkerId();
        }

        public static PortalBridgeWorker StartFromEnvironment()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("NOSGM_PORTAL_BRIDGE_ENABLED"), "true", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var connectionString = Environment.GetEnvironmentVariable("NOSGM_PORTAL_SQL_CONNECTION_STRING");
            ValidateConnectionString(connectionString);
            var worker = new PortalBridgeWorker(connectionString);
            worker.Start();
            Logger.Info("NosGM Portal bridge started as " + worker._workerId + ".");
            return worker;
        }

        private static void ValidateConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("NOSGM_PORTAL_SQL_CONNECTION_STRING is required when the Portal bridge is enabled.");
            }

            SqlConnectionStringBuilder builder;
            try
            {
                builder = new SqlConnectionStringBuilder(connectionString);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("NOSGM_PORTAL_SQL_CONNECTION_STRING is not a valid SQL Server connection string.", exception);
            }

            if (string.IsNullOrWhiteSpace(builder.DataSource) || string.IsNullOrWhiteSpace(builder.InitialCatalog))
            {
                throw new InvalidOperationException("NOSGM_PORTAL_SQL_CONNECTION_STRING must select an explicit SQL Server and database.");
            }
            if (!builder.IntegratedSecurity && string.Equals(builder.UserID, "sa", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The Portal bridge must not run with the SQL Server sa account. Use the least-privilege portal bridge login.");
            }
        }

        private static string BuildWorkerId()
        {
            var value = string.Format(CultureInfo.InvariantCulture, "{0}-world-{1}-{2}", Environment.MachineName,
                ServerManager.Instance.ChannelId, System.Diagnostics.Process.GetCurrentProcess().Id);
            return value.Length <= 80 ? value : value.Substring(0, 80);
        }

        public void Start()
        {
            if (_task != null) throw new InvalidOperationException("Portal bridge already started.");
            _task = Task.Run(() => RunAsync(_cancellation.Token));
        }

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await PublishLivePositionsAsync(token).ConfigureAwait(false);
                    for (var i = 0; i < MaximumCommandsPerCycle && !token.IsCancellationRequested; i++)
                    {
                        var command = await ClaimAsync(token).ConfigureAwait(false);
                        if (command == null) break;
                        await ExecuteAsync(command, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    Logger.Error("Portal bridge cycle failed", exception);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task PublishLivePositionsAsync(CancellationToken token)
        {
            var snapshot = ServerManager.Instance.Sessions
                .Where(session => session?.Character != null && session.Character.MapInstance != null)
                .Select(session => new
                {
                    session.Character.CharacterId,
                    session.Character.MapId,
                    MapX = session.Character.MapX,
                    MapY = session.Character.MapY
                }).ToList();

            var positions = new DataTable();
            positions.Columns.Add("CharacterId", typeof(long));
            positions.Columns.Add("MapId", typeof(short));
            positions.Columns.Add("MapX", typeof(short));
            positions.Columns.Add("MapY", typeof(short));
            foreach (var player in snapshot) positions.Rows.Add(player.CharacterId, player.MapId, player.MapX, player.MapY);

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("portal.UpsertLiveCharacterStateBatch", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 15;
                var batch = command.Parameters.AddWithValue("@Positions", positions);
                batch.SqlDbType = SqlDbType.Structured;
                batch.TypeName = "portal.LivePositionBatch";
                command.Parameters.Add("@ChannelId", SqlDbType.Int).Value = ServerManager.Instance.ChannelId;
                command.Parameters.Add("@WorkerId", SqlDbType.VarChar, 80).Value = _workerId;
                command.Parameters.Add("@SeenAt", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                await connection.OpenAsync(token).ConfigureAwait(false);
                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        private async Task<PortalCommand> ClaimAsync(CancellationToken token)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("portal.ClaimNextGmCommand", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 15;
                command.Parameters.Add("@WorkerId", SqlDbType.VarChar, 80).Value = _workerId;
                command.Parameters.Add("@ChannelId", SqlDbType.Int).Value = ServerManager.Instance.ChannelId;
                await connection.OpenAsync(token).ConfigureAwait(false);
                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token).ConfigureAwait(false))
                {
                    if (!await reader.ReadAsync(token).ConfigureAwait(false)) return null;
                    var parametersOrdinal = reader.GetOrdinal("ParametersJson");
                    var parameters = reader.IsDBNull(parametersOrdinal) ? "{}" : reader.GetString(parametersOrdinal);
                    if (parameters.Length > MaximumParametersJsonLength) throw new InvalidOperationException("portal_parameters_too_large");
                    return new PortalCommand
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("CommandId")),
                        Action = reader.GetString(reader.GetOrdinal("Action")),
                        CharacterId = reader.IsDBNull(reader.GetOrdinal("TargetCharacterId")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("TargetCharacterId")),
                        Parameters = parameters
                    };
                }
            }
        }

        private async Task ExecuteAsync(PortalCommand command, CancellationToken token)
        {
            var succeeded = false;
            var result = "unsupported_command";
            try
            {
                var values = _json.Deserialize<Dictionary<string, object>>(command.Parameters ?? "{}") ?? new Dictionary<string, object>();
                switch (command.Action)
                {
                    case "announce":
                        var message = RequiredText(values, "message", 200);
                        foreach (var session in ServerManager.Instance.Sessions.Where(session => session?.Character != null))
                            session.SendPacket(UserInterfaceHelper.GenerateMsg("[NosGM] " + message, 0));
                        result = "announcement_sent";
                        succeeded = true;
                        break;
                    case "kick":
                        var target = FindOnline(command.CharacterId);
                        if (target == null) throw new InvalidOperationException("player_not_online_on_channel");
                        ServerManager.Instance.Kick(target.Character.Name);
                        result = "player_disconnected";
                        succeeded = true;
                        break;
                    case "teleport":
                        var teleportSession = FindOnline(command.CharacterId);
                        if (teleportSession == null) throw new InvalidOperationException("player_not_online_on_channel");
                        var mapId = RequiredInt16(values, "mapId");
                        var x = RequiredInt16(values, "x");
                        var y = RequiredInt16(values, "y");
                        if (ServerManager.GetBaseMapInstanceIdByMapId(mapId) == Guid.Empty) throw new InvalidOperationException("map_not_loaded");
                        ServerManager.Instance.ChangeMap(teleportSession.Character.CharacterId, mapId, x, y);
                        result = "player_teleported";
                        succeeded = true;
                        break;
                }
            }
            catch (Exception exception)
            {
                result = OneLine(exception.Message, MaximumResultLength);
                Logger.Error("Portal command " + command.Id + " failed", exception);
            }
            await CompleteAsync(command.Id, succeeded, result, token).ConfigureAwait(false);
        }

        private static string RequiredText(IDictionary<string, object> values, string key, int maximumLength)
        {
            if (!values.TryGetValue(key, out var raw)) throw new InvalidOperationException("missing_" + key);
            var value = Convert.ToString(raw, CultureInfo.InvariantCulture)?.Trim();
            if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength || value.IndexOfAny(new[] { '\r', '\n' }) >= 0)
                throw new InvalidOperationException("invalid_" + key);
            return value;
        }

        private static short RequiredInt16(IDictionary<string, object> values, string key)
        {
            if (!values.TryGetValue(key, out var raw)) throw new InvalidOperationException("missing_" + key);
            try { return Convert.ToInt16(raw, CultureInfo.InvariantCulture); }
            catch (Exception exception) when (exception is FormatException || exception is InvalidCastException || exception is OverflowException)
            {
                throw new InvalidOperationException("invalid_" + key, exception);
            }
        }

        private static ClientSession FindOnline(long? characterId)
        {
            return characterId.HasValue
                ? ServerManager.Instance.Sessions.FirstOrDefault(session => session?.Character != null && session.Character.CharacterId == characterId.Value)
                : null;
        }

        private async Task CompleteAsync(long commandId, bool succeeded, string result, CancellationToken token)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("portal.CompleteGmCommand", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 15;
                command.Parameters.Add("@CommandId", SqlDbType.BigInt).Value = commandId;
                command.Parameters.Add("@WorkerId", SqlDbType.VarChar, 80).Value = _workerId;
                command.Parameters.Add("@Succeeded", SqlDbType.Bit).Value = succeeded;
                command.Parameters.Add("@ResultMessage", SqlDbType.NVarChar, MaximumResultLength).Value = OneLine(result, MaximumResultLength);
                await connection.OpenAsync(token).ConfigureAwait(false);
                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
        }

        private static string OneLine(string value, int maximumLength)
        {
            var sanitized = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return sanitized.Length <= maximumLength ? sanitized : sanitized.Substring(0, maximumLength);
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            try { _task?.Wait(TimeSpan.FromSeconds(5)); } catch (AggregateException) { }
            finally { _cancellation.Dispose(); }
        }

        private sealed class PortalCommand
        {
            public long Id { get; set; }
            public string Action { get; set; }
            public long? CharacterId { get; set; }
            public string Parameters { get; set; }
        }
    }
}
