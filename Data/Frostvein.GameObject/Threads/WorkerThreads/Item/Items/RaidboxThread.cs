using MongoDB.Driver.Core.Configuration;
using Frostvein.Configuration;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.GameObject.ItemThread
{
    public static class RaidboxThread
    {
        private static string connectionString = ServerConfiguration.DatabaseConnection;

        public static string Callback {get; set;}

        private static RaidboxDTO GetRandomRaidboxFromDatabase(ClientSession session, ItemInstance inv)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                int currentOriginalItemVNum = inv.ItemVNum;

                string query = "SELECT TOP 1 * FROM Raidbox WHERE OriginalItemVNum = @CurrentOriginalItemVNum ORDER BY NEWID()";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                        
                    command.Parameters.AddWithValue("@CurrentOriginalItemVNum", currentOriginalItemVNum);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            RaidboxDTO raidbox = new RaidboxDTO
                            {
                                IsRareRandom = reader.GetBoolean(1),
                                ItemGeneratedAmount = reader.GetInt16(2),
                                ItemGeneratedDesign = reader.GetInt16(3),
                                ItemGeneratedVNum = reader.GetInt16(4),
                                MaximumOriginalItemRare = reader.GetByte(5),
                                MinimumOriginalItemRare = reader.GetByte(6),
                                OriginalItemDesign = reader.GetInt16(7),
                                OriginalItemVNum = reader.GetInt16(8),
                                Probability = reader.GetInt16(9),
                            };

                            return raidbox;
                        }
                    }
                }
            }

            return null;
        }

        public static void GenerateReward(ClientSession session, ItemInstance inv)
        {
            RaidboxDTO raidbox = GetRandomRaidboxFromDatabase(session, inv);
            ItemThread.Add(session, raidbox.ItemGeneratedVNum, raidbox.ItemGeneratedAmount, (byte)raidbox.MaximumOriginalItemRare, (byte)raidbox.ItemGeneratedDesign, 0, true, 1, true);
        }
    }
}
