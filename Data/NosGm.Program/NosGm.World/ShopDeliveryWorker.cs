using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NosGm.World
{
    /// <summary>
    /// Claims purchases through shop.ProcessNextDelivery. The stored procedure commits the Mail row,
    /// delivery receipt and purchase state in one SQL transaction before the online player is notified.
    /// </summary>
    public sealed class ShopDeliveryWorker : IDisposable
    {
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private readonly string _connectionString;
        private readonly long _systemSenderId;
        private readonly int _pollMilliseconds;
        private Task _task;

        private ShopDeliveryWorker(string connectionString, long systemSenderId, int pollMilliseconds)
        {
            _connectionString = connectionString;
            _systemSenderId = systemSenderId;
            _pollMilliseconds = pollMilliseconds;
        }

        public static ShopDeliveryWorker StartFromEnvironment()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("NOSGM_SHOP_ENABLED"), "true", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!long.TryParse(Environment.GetEnvironmentVariable("NOSGM_SHOP_SYSTEM_SENDER_ID"), out var senderId) || senderId <= 0)
            {
                throw new InvalidOperationException("NOSGM_SHOP_SYSTEM_SENDER_ID must be an existing service character id.");
            }

            if (!int.TryParse(Environment.GetEnvironmentVariable("NOSGM_SHOP_POLL_MS"), out var pollMilliseconds))
            {
                pollMilliseconds = 2000;
            }
            if (pollMilliseconds < 500 || pollMilliseconds > 30000)
            {
                throw new InvalidOperationException("NOSGM_SHOP_POLL_MS must be between 500 and 30000.");
            }

            var connectionString = Environment.GetEnvironmentVariable("NOSGM_SHOP_SQL_CONNECTION_STRING");
            ValidateConnectionString(connectionString);

            var worker = new ShopDeliveryWorker(connectionString, senderId, pollMilliseconds);
            worker._task = Task.Run(() => worker.RunAsync());
            Logger.Info("NosMall delivery worker started.");
            return worker;
        }

        private static void ValidateConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("NOSGM_SHOP_SQL_CONNECTION_STRING is required when NosMall delivery is enabled.");
            }

            SqlConnectionStringBuilder builder;
            try
            {
                builder = new SqlConnectionStringBuilder(connectionString);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("NOSGM_SHOP_SQL_CONNECTION_STRING is not a valid SQL Server connection string.", exception);
            }

            if (string.IsNullOrWhiteSpace(builder.DataSource) || string.IsNullOrWhiteSpace(builder.InitialCatalog))
            {
                throw new InvalidOperationException("NOSGM_SHOP_SQL_CONNECTION_STRING must select an explicit SQL Server and database.");
            }
            if (!builder.IntegratedSecurity && string.Equals(builder.UserID, "sa", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("NosMall delivery must not run with the SQL Server sa account. Use the least-privilege shop application login.");
            }
        }

        private async Task RunAsync()
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    var mail = await ClaimAsync().ConfigureAwait(false);
                    if (mail != null)
                    {
                        NotifyOnlineCharacter(mail);
                        continue;
                    }
                    await Task.Delay(_pollMilliseconds, _stop.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    Logger.Error("NosMall delivery worker error", exception);
                    try { await Task.Delay(Math.Max(_pollMilliseconds, 5000), _stop.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (_stop.IsCancellationRequested) { break; }
                }
            }
        }

        private async Task<MailDTO> ClaimAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("shop.ProcessNextDelivery", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 15;
                command.Parameters.Add("@SystemSenderId", SqlDbType.BigInt).Value = _systemSenderId;
                command.Parameters.Add("@MailboxLimit", SqlDbType.Int).Value = 40;
                await connection.OpenAsync(_stop.Token).ConfigureAwait(false);
                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, _stop.Token).ConfigureAwait(false))
                {
                    if (!await reader.ReadAsync(_stop.Token).ConfigureAwait(false)) return null;
                    return new MailDTO
                    {
                        AttachmentAmount = Convert.ToInt16(reader["AttachmentAmount"]),
                        AttachmentDesign = Convert.ToInt16(reader["AttachmentDesign"]),
                        AttachmentLevel = Convert.ToByte(reader["AttachmentLevel"]),
                        AttachmentRarity = Convert.ToByte(reader["AttachmentRarity"]),
                        AttachmentUpgrade = Convert.ToByte(reader["AttachmentUpgrade"]),
                        AttachmentVNum = reader["AttachmentVNum"] == DBNull.Value ? (short?)null : Convert.ToInt16(reader["AttachmentVNum"]),
                        Date = Convert.ToDateTime(reader["Date"]),
                        DeliverySource = ItemTraceSource.ItemMall,
                        EqPacket = Convert.ToString(reader["EqPacket"]),
                        IsOpened = Convert.ToBoolean(reader["IsOpened"]),
                        IsSenderCopy = Convert.ToBoolean(reader["IsSenderCopy"]),
                        MailId = Convert.ToInt64(reader["MailId"]),
                        Message = Convert.ToString(reader["Message"]),
                        ReceiverId = Convert.ToInt64(reader["ReceiverId"]),
                        SenderClass = (ClassType)Convert.ToByte(reader["SenderClass"]),
                        SenderGender = (GenderType)Convert.ToByte(reader["SenderGender"]),
                        SenderHairColor = (HairColorType)Convert.ToByte(reader["SenderHairColor"]),
                        SenderHairStyle = (HairStyleType)Convert.ToByte(reader["SenderHairStyle"]),
                        SenderId = Convert.ToInt64(reader["SenderId"]),
                        SenderMorphId = Convert.ToInt16(reader["SenderMorphId"]),
                        Title = Convert.ToString(reader["Title"])
                    };
                }
            }
        }

        private static void NotifyOnlineCharacter(MailDTO mail)
        {
            var session = ServerManager.Instance.Sessions
                .FirstOrDefault(candidate => candidate?.Character != null && candidate.Character.CharacterId == mail.ReceiverId);
            if (session == null) return;

            lock (session.Character.MailList)
            {
                if (session.Character.MailList.Any(entry => entry.Value.MailId == mail.MailId)) return;
                var key = session.Character.MailList.Count == 0 ? 1 : session.Character.MailList.Keys.Max() + 1;
                session.Character.MailList.Add(key, mail);
            }

            session.SendPacket(session.Character.GenerateParcel(mail));
        }

        public void Dispose()
        {
            _stop.Cancel();
            try { _task?.Wait(TimeSpan.FromSeconds(5)); } catch (AggregateException) { }
            finally { _stop.Dispose(); }
        }
    }
}
